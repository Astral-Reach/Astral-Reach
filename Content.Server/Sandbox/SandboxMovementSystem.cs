using Content.Shared.Sandbox;

namespace Content.Server.Sandbox;

public sealed partial class SandboxMovementSystem : SharedSandboxMovementSystem
{
    public override void UpdateBeforeSolve(bool prediction, float frameTime)
    {
        var query = EntityQueryEnumerator<SandboxPawnComponent>();
        while (query.MoveNext(out var uid, out var pawn))
            Move(uid, pawn);
    }
}
