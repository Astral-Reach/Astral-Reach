using System.Numerics;

namespace Content.Shared.Sandbox;

public static class SandboxMovement
{
    public const float Speed = 4f;

    public static Vector2 Direction(MoveButtons buttons)
    {
        var direction = new Vector2(
            (buttons.HasFlag(MoveButtons.Right) ? 1 : 0) - (buttons.HasFlag(MoveButtons.Left) ? 1 : 0),
            (buttons.HasFlag(MoveButtons.Up) ? 1 : 0) - (buttons.HasFlag(MoveButtons.Down) ? 1 : 0));
        return direction == Vector2.Zero ? direction : Vector2.Normalize(direction);
    }
}
