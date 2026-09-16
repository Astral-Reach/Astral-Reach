using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Sandbox;

[Prototype("tile")]
public sealed partial class SandboxTileDefinition : IPrototype, ITileDefinition
{
    [IdDataField] public string ID { get; private set; } = "";
    [DataField] public string Name { get; private set; } = "";
    [DataField] public ResPath? Sprite { get; private set; }
    public ushort TileId { get; private set; }
    public Dictionary<Direction, ResPath> EdgeSprites { get; } = new();
    public int EdgeSpritePriority => 0;
    public float Friction => 0;
    public byte Variants => 1;
    public void AssignTileId(ushort id) => TileId = id;
}

public sealed partial class SandboxTileSystem : EntitySystem
{
    private static readonly ProtoId<SandboxTileDefinition> Empty = "Empty";
    private static readonly ProtoId<SandboxTileDefinition> Floor = "SandboxFloor";
    [Dependency] private ITileDefinitionManager _tiles = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        base.Initialize();
        if (_tiles.Count > 0)
            return;
        // Zero is the engine's empty tile. Registration order must match on both sides.
        _tiles.Register(_prototypes.Index(Empty));
        _tiles.Register(_prototypes.Index(Floor));
        _tiles.Initialize();
    }
}
