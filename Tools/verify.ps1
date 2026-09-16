param([string]$PackageRid = '', [switch]$SkipPackaging)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskRoot
New-Item -ItemType Directory -Force artifacts/verification | Out-Null
if ((git -C RobustToolbox rev-parse HEAD) -ne 'edf061e7450a4074f173e3000bf1552b6f54082f') {
    throw 'Unexpected Robust Toolbox revision'
}

function Invoke-CheckedDotnet([string]$name, [string[]]$taskArgs) {
    & dotnet @taskArgs *> "artifacts/verification/$name.log"
    if ($LASTEXITCODE -ne 0) {
        Get-Content "artifacts/verification/$name.log" -Tail 30
        throw "dotnet failed: $name"
    }
    $taskWarnings = Get-Content "artifacts/verification/$name.log" | Where-Object {
        $_ -match 'warning [A-Z]+[0-9]+:' -and $_ -notmatch '[/\\]RobustToolbox[/\\]'
    }
    if ($taskWarnings) { $taskWarnings; throw "Content warnings: $name" }
    Write-Output "Passed: $name"
}

foreach ($taskConfiguration in @('Debug','DebugOpt','Release')) {
    # Remove only generated output in this verified checkout, never untracked files broadly.
    $taskOutput = [IO.Path]::GetFullPath((Join-Path $taskRoot 'bin'))
    if ($taskOutput -ne [IO.Path]::Combine($taskRoot, 'bin') -or (git ls-files -- bin)) {
        throw 'Unexpected generated output path'
    }
    if (Test-Path -LiteralPath $taskOutput) {
        if ((Get-Item -LiteralPath $taskOutput).Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw 'Generated output is a link'
        }
        Remove-Item -LiteralPath $taskOutput -Recurse -Force
    }
    Invoke-CheckedDotnet "restore-$taskConfiguration" @('restore','SpaceStation14.slnx',"-p:Configuration=$taskConfiguration")
    Invoke-CheckedDotnet "build-$taskConfiguration" @('build','SpaceStation14.slnx','-c',$taskConfiguration,'--no-restore','--nologo',"-bl:artifacts/verification/build-$taskConfiguration.binlog")
    foreach ($taskProject in @('Content.Tests','Content.IntegrationTests')) {
        Invoke-CheckedDotnet "test-$taskProject-$taskConfiguration" @('test',"$taskProject/$taskProject.csproj",'-c',$taskConfiguration,'--no-build','--logger',"trx;LogFileName=$taskProject-$taskConfiguration.trx",'--results-directory','artifacts/verification')
    }
}

if (!$SkipPackaging) {
    if (!$PackageRid) { $PackageRid = if ($IsWindows) { 'win-x64' } else { 'linux-x64' } }
    Invoke-CheckedDotnet "package-$PackageRid" @('run','--project','Content.Packaging','-c','DebugOpt','--','server','--hybrid-acz','--platform',$PackageRid,'--log-build')
    $taskExtract = Join-Path ([IO.Path]::GetTempPath()) ('astral-reach-verify-' + [guid]::NewGuid().ToString('N'))
    & python Tools/verify_packages.py --package "release/SS14.Server_$PackageRid.zip" --extract $taskExtract --serve --check-update --evidence "artifacts/verification/package-$PackageRid"
    if ($LASTEXITCODE -ne 0) { throw 'Package/download smoke test failed' }
}
