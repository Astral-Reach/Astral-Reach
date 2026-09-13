using System.Globalization;
using Content.Client.Connection;
using Content.Shared.Sandbox;
using Robust.Client.Input;
using Robust.Shared.ContentPack;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Content.Client.Entry;

public sealed class EntryPoint : GameClient
{
    private SandboxScreen? _screen;

    public override void Init()
    {
        base.Init();
        Dependencies.BuildGraph();
        var factory = Dependencies.Resolve<IComponentFactory>();
        factory.DoAutoRegistrations();
        factory.GenerateNetIds();
        Dependencies.Resolve<ILocalizationManager>().LoadCulture(new CultureInfo("en-US"));
        Dependencies.Resolve<IConfigurationManager>().OverrideDefault(CVars.DiscordEnabled, false);
    }

    public override void PostInit()
    {
        base.PostInit();
        var context = Dependencies.Resolve<IInputManager>().Contexts.GetContext("human");
        context.AddFunction(EngineKeyFunctions.MoveUp);
        context.AddFunction(EngineKeyFunctions.MoveDown);
        context.AddFunction(EngineKeyFunctions.MoveLeft);
        context.AddFunction(EngineKeyFunctions.MoveRight);
        context.AddFunction(SandboxInput.Interact);
        _screen = new SandboxScreen(Dependencies);
    }

    public override void Update(ModUpdateLevel level, FrameEventArgs frameEventArgs)
    {
        _screen?.CompleteStartup();
        base.Update(level, frameEventArgs);
    }

    public override void Shutdown()
    {
        _screen?.Dispose();
        base.Shutdown();
    }
}
