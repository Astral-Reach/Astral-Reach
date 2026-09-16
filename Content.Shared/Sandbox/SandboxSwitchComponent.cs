using Robust.Shared.GameStates;

namespace Content.Shared.Sandbox;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SandboxSwitchComponent : Component
{
    [DataField, AutoNetworkedField] public bool Enabled;
}
