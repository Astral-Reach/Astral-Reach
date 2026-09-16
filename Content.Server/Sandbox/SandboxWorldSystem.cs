using System.Linq;
using System.Numerics;
using Content.Shared.Sandbox;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;

namespace Content.Server.Sandbox;

/// <summary>A replaceable development scene, deliberately independent of rounds and game modes.</summary>
public sealed partial class SandboxWorldSystem : EntitySystem
{
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private ITileDefinitionManager _tiles = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private MetaDataSystem _metadata = default!;

    private readonly Dictionary<NetUserId, EntityUid> _pawns = new();
    private readonly HashSet<ICommonSession> _joining = new();
    public EntityUid Arena { get; private set; }
    public Entity<MapGridComponent> Grid { get; private set; }
    public EntityUid Switch { get; private set; }
    public const float InteractionRange = 2f;

    public override void Initialize()
    {
        base.Initialize();
        _players.PlayerStatusChanged += OnPlayerStatus;
        CommandBinds.Builder.Bind(SandboxInput.Interact, new InteractHandler(this)).Register<SandboxWorldSystem>();
    }

    public override void Shutdown()
    {
        _players.PlayerStatusChanged -= OnPlayerStatus;
        CommandBinds.Unregister<SandboxWorldSystem>();
        _joining.Clear();
        _pawns.Clear();
        base.Shutdown();
    }

    public void CreateArena()
    {
        if (Exists(Arena))
            return;
        Arena = _maps.CreateMap(out var mapId);
        _maps.SetAmbientLight(mapId, Color.White);
        Grid = _maps.CreateGridEntity(mapId);
        var tile = new Tile(_tiles["SandboxFloor"].TileId);
        for (var x = 0; x < 16; x++)
        for (var y = 0; y < 16; y++)
        {
            _maps.SetTile(Grid.Owner, Grid.Comp, new Vector2i(x, y), tile);
            if (x is 0 or 15 || y is 0 or 15 || x == 8 && y is >= 6 and <= 10)
                Spawn("SandboxWall", new EntityCoordinates(Grid, new Vector2(x + 0.5f, y + 0.5f)));
        }
        Switch = Spawn("SandboxSwitch", new EntityCoordinates(Grid, new Vector2(6.5f, 7.5f)));
    }

    private void OnPlayerStatus(object? sender, SessionStatusEventArgs args)
    {
        var session = args.Session;
        switch (args.NewStatus)
        {
            case SessionStatus.Connected:
                // Join on the next tick, after the engine has completed its connection callback.
                _joining.Add(session);
                break;
            case SessionStatus.InGame:
                if (_pawns.TryGetValue(session.UserId, out var existing) && Exists(existing))
                    return;
                var pawn = Spawn("SandboxPawn", new EntityCoordinates(Grid, new Vector2(3.5f, 3.5f + _pawns.Count % 4)));
                _metadata.SetEntityName(pawn, session.Name);
                _pawns[session.UserId] = pawn;
                _players.SetAttachedEntity(session, pawn);
                break;
            case SessionStatus.Disconnected:
                _joining.Remove(session);
                if (_pawns.Remove(session.UserId, out var oldPawn))
                    QueueDel(oldPawn);
                break;
        }
    }

    public override void Update(float frameTime)
    {
        foreach (var session in _joining.ToArray())
        {
            _joining.Remove(session);
            if (session.Status == SessionStatus.Connected)
                _players.JoinGame(session);
        }
    }

    public void HandleInteraction(ICommonSession? session, BoundKeyState state)
    {
        if (session is null || session.Status != SessionStatus.InGame ||
            session.AttachedEntity is not { } pawn ||
            !_pawns.TryGetValue(session.UserId, out var owned) || owned != pawn ||
            !TryComp<SandboxPawnComponent>(pawn, out var component) ||
            state is not (BoundKeyState.Up or BoundKeyState.Down))
            return;

        var pressed = state == BoundKeyState.Down;
        if (component.InteractHeld == pressed)
            return;
        component.InteractHeld = pressed;
        Dirty(pawn, component);
        if (!pressed)
            return;

        var origin = _transform.GetMapCoordinates(pawn);
        EntityUid? nearest = null;
        var nearestDistance = InteractionRange * InteractionRange;
        var query = EntityQueryEnumerator<SandboxSwitchComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (TerminatingOrDeleted(uid))
                continue;
            var target = _transform.GetMapCoordinates(uid, xform);
            if (origin.MapId != target.MapId)
                continue;
            var offset = target.Position - origin.Position;
            var distance = offset.LengthSquared();
            if (distance > nearestDistance)
                continue;
            if (distance > 0.0001f && _physics.IntersectRay(origin.MapId,
                    new CollisionRay(origin.Position, Vector2.Normalize(offset), 1), MathF.Sqrt(distance), pawn).Any())
                continue;
            nearest = uid;
            nearestDistance = distance;
        }

        if (nearest is not { } selected)
            return;
        var toggle = Comp<SandboxSwitchComponent>(selected);
        toggle.Enabled = !toggle.Enabled;
        Dirty(selected, toggle);
    }

    private sealed class InteractHandler(SandboxWorldSystem world) : InputCmdHandler
    {
        public override bool HandleCmdMessage(IEntityManager entManager, ICommonSession? session, IFullInputCmdMessage message)
        {
            world.HandleInteraction(session, message.State);
            return false;
        }
    }
}
