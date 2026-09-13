using Content.Shared.Sandbox;
using Robust.Client.Physics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Sandbox;

public sealed partial class SandboxMovementSystem : SharedSandboxMovementSystem
{
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IInputManager _input = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SandboxPawnComponent, UpdateIsPredictedEvent>(OnPrediction);
        SubscribeLocalEvent<SandboxPawnComponent, LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<SandboxPawnComponent, LocalPlayerDetachedEvent>(OnDetached);
    }

    private void OnPrediction(Entity<SandboxPawnComponent> ent, ref UpdateIsPredictedEvent args)
    {
        if (ent.Owner == _players.LocalEntity)
            args.IsPredicted = true;
    }

    private void OnAttached(Entity<SandboxPawnComponent> ent, ref LocalPlayerAttachedEvent args)
        => PhysicsSystem.UpdateIsPredicted(ent.Owner);

    private void OnDetached(Entity<SandboxPawnComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _input.ReleaseAllKeys();
        ent.Comp.Buttons = MoveButtons.None;
        PhysicsSystem.UpdateIsPredicted(ent.Owner);
    }

    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        if (_players.LocalEntity is { } uid && TryComp<SandboxPawnComponent>(uid, out var pawn))
            Move(uid, pawn);
    }
}
