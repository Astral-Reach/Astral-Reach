using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Controllers;
using Robust.Shared.Player;

namespace Content.Shared.Sandbox;

public abstract partial class SharedSandboxMovementSystem : VirtualController
{
    public override void Initialize()
    {
        base.Initialize();
        CommandBinds.Builder
            .Bind(EngineKeyFunctions.MoveUp, new MovementHandler(this, MoveButtons.Up))
            .Bind(EngineKeyFunctions.MoveDown, new MovementHandler(this, MoveButtons.Down))
            .Bind(EngineKeyFunctions.MoveLeft, new MovementHandler(this, MoveButtons.Left))
            .Bind(EngineKeyFunctions.MoveRight, new MovementHandler(this, MoveButtons.Right))
            .Register<SharedSandboxMovementSystem>();
    }

    public override void Shutdown()
    {
        CommandBinds.Unregister<SharedSandboxMovementSystem>();
        base.Shutdown();
    }

    protected void Move(EntityUid uid, SandboxPawnComponent pawn)
    {
        if (TryComp<PhysicsComponent>(uid, out var body))
            PhysicsSystem.SetLinearVelocity(uid, SandboxMovement.Direction(pawn.Buttons) * SandboxMovement.Speed, body: body);
    }

    public void SetInput(ICommonSession? session, MoveButtons button, BoundKeyState state)
    {
        if (session?.AttachedEntity is not { } uid ||
            !TryComp<SandboxPawnComponent>(uid, out var pawn) ||
            state is not (BoundKeyState.Down or BoundKeyState.Up))
            return;

        var buttons = state == BoundKeyState.Down ? pawn.Buttons | button : pawn.Buttons & ~button;
        if (buttons == pawn.Buttons)
            return;
        pawn.Buttons = buttons;
        Dirty(uid, pawn);
    }

    private sealed class MovementHandler(SharedSandboxMovementSystem system, MoveButtons button) : InputCmdHandler
    {
        public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
        {
            system.SetInput(session, button, message.State);
            return false;
        }
    }
}
