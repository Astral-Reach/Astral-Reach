using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization;

namespace Content.Shared.Sandbox;

[Flags, FlagsFor(typeof(CollisionLayer)), FlagsFor(typeof(CollisionMask))]
public enum SandboxCollision
{
    None = 0,
    Wall = 1,
    Pawn = 2
}

[ConstantsFor(typeof(DrawDepth))]
public enum SandboxDrawDepth
{
    Floor = 0,
    Wall = 1,
    Pawn = 2
}
