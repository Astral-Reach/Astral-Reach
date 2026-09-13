using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Sandbox;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SandboxPawnComponent : Component
{
    [DataField, AutoNetworkedField] public MoveButtons Buttons;
    [DataField, AutoNetworkedField] public bool InteractHeld;
}

[Flags, Serializable, NetSerializable]
public enum MoveButtons : byte
{
    None = 0, Up = 1, Down = 2, Left = 4, Right = 8
}
