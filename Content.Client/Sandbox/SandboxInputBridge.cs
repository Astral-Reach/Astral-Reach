using Content.Shared.Sandbox;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client.Sandbox;

/// <summary>Forwards viewport key events into Robust's predicted, networked input pipeline.</summary>
public sealed partial class SandboxInputBridge : EntitySystem
{
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private InputSystem _simulation = default!;

    public override void Initialize()
    {
        base.Initialize();
        _input.KeyBindStateChanged += OnKey;
    }

    public override void Shutdown()
    {
        _input.ReleaseAllKeys();
        _input.KeyBindStateChanged -= OnKey;
        base.Shutdown();
    }

    private void OnKey(ViewportBoundKeyEventArgs args)
    {
        if (_players.LocalEntity is not { } pawn || !HasComp<SandboxPawnComponent>(pawn))
            return;
        var key = args.KeyEventArgs;
        if (key.Function != EngineKeyFunctions.MoveUp && key.Function != EngineKeyFunctions.MoveDown &&
            key.Function != EngineKeyFunctions.MoveLeft && key.Function != EngineKeyFunctions.MoveRight &&
            key.Function != SandboxInput.Interact)
            return;
        var message = new ClientFullInputCmdMessage(_timing.CurTick, _timing.TickFraction,
            _input.NetworkBindMap.KeyFunctionID(key.Function))
        {
            State = key.State,
            Coordinates = EntityCoordinates.Invalid,
            ScreenCoordinates = key.PointerLocation
        };
        if (_simulation.HandleInputCommand(_players.LocalSession, key.Function, message))
            key.Handle();
    }
}
