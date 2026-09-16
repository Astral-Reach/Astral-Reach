using Content.Packaging;
using Robust.Packaging;

IPackageLogger logger = new PackageLoggerConsole();

if (!CommandLineArgs.TryParse(args, out var parsed))
{
    logger.Error("Unable to parse args, aborting.");
    return 1;
}

// Packaging paths are relative to the content checkout, never an arbitrary working directory.
if (!File.Exists("SpaceStation14.slnx") || !File.Exists("Resources/ConfigPresets/server_config.toml") ||
    !Directory.Exists("RobustToolbox/Robust.Server"))
{
    logger.Error("Run packaging from the Astral Reach repository root.");
    return 1;
}

if (parsed.WipeRelease)
    WipeRelease();
else
{
    // Ensure the release directory exists. Otherwise, the packaging will fail.
    Directory.CreateDirectory("release");
}

if (!parsed.SkipBuild)
{
    CleanDirectory("bin/Content.Client");
    CleanDirectory("bin/Content.Server");
}

if (parsed.Client)
{
    await ClientPackaging.PackageClient(parsed.SkipBuild, parsed.LogBuild, parsed.Configuration, logger);
}
else
{
    await ServerPackaging.PackageServer(parsed.SkipBuild, parsed.HybridAcz, parsed.LogBuild, logger, parsed.Configuration, parsed.Platforms);
}

return 0;

void WipeRelease()
{
    if (Directory.Exists("release"))
    {
        logger.Info("Cleaning old release packages (release/)...");
        CleanDirectory("release");
    }

    Directory.CreateDirectory("release");
}

void CleanDirectory(string relative)
{
    var root = Path.GetFullPath(Environment.CurrentDirectory) + Path.DirectorySeparatorChar;
    var path = Path.GetFullPath(relative);
    if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException($"Output path escapes repository: {path}");
    // Refuse redirected output paths, including an ancestor such as bin -> another checkout.
    for (var directory = new DirectoryInfo(path); directory != null && directory.FullName.Length >= root.Length; directory = directory.Parent)
        if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException($"Output path is a link: {directory.FullName}");
    if (Directory.Exists(path))
        Directory.Delete(path, recursive: true);
}
