using System.Globalization;
using Content.Server.Sandbox;
using Content.Server.Acz;
using Robust.Server.ServerStatus;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;

namespace Content.Server.Entry;

public sealed class EntryPoint : GameServer
{
    public override void Init()
    {
        base.Init();
        Dependencies.BuildGraph();
        var factory = Dependencies.Resolve<IComponentFactory>();
        factory.DoAutoRegistrations();
        factory.GenerateNetIds();
        Dependencies.Resolve<ILocalizationManager>().LoadCulture(new CultureInfo("en-US"));
        var cfg = Dependencies.Resolve<IConfigurationManager>();
        cfg.OverrideDefault(CVars.BuildForkId, "astral-reach");
        cfg.OverrideDefault(CVars.BuildEngineVersion, "289.0.0");
        Dependencies.Resolve<IStatusHost>().SetMagicAczProvider(new ContentMagicAczProvider(Dependencies));
    }

    public override void PostInit()
    {
        base.PostInit();
        Dependencies.Resolve<IEntitySystemManager>().GetEntitySystem<SandboxWorldSystem>().CreateArena();
    }
}
