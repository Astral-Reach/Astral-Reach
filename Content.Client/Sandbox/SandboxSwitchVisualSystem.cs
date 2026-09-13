using Content.Shared.Sandbox;
using Robust.Client.GameObjects;

namespace Content.Client.Sandbox;

public sealed partial class SandboxSwitchVisualSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprites = default!;

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<SandboxSwitchComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var toggle, out var sprite))
            _sprites.SetColor((uid, sprite), toggle.Enabled ? Color.Lime : Color.Orange);
    }
}
