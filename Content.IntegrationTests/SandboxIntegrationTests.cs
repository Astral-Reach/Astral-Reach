using System.Linq;
using System.Numerics;
using Content.Server.Sandbox;
using Content.Shared.Sandbox;
using Robust.Client;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.UnitTesting;
using ClientPlayers = Robust.Client.Player.IPlayerManager;
using ServerPlayers = Robust.Server.Player.IPlayerManager;

namespace Content.IntegrationTests;

[TestFixture, NonParallelizable]
public sealed class SandboxIntegrationTests : RobustIntegrationTest
{
    private static ServerIntegrationOptions ServerOptions() => new()
    {
        Pool = false,
        ContentStart = true,
        LoadTestAssembly = false,
        ContentAssemblies = [typeof(Content.Server.Entry.EntryPoint).Assembly, typeof(Content.Shared.Entry.EntryPoint).Assembly],
        Options = new() { LoadConfigAndUserData = false },
        FailureLogLevel = LogLevel.Warning
    };

    private static ClientIntegrationOptions ClientOptions() => new()
    {
        Pool = false,
        ContentStart = true,
        LoadTestAssembly = false,
        ContentAssemblies = [typeof(Content.Client.Entry.EntryPoint).Assembly, typeof(Content.Shared.Entry.EntryPoint).Assembly],
        Options = new() { LoadConfigAndUserData = false },
        FailureLogLevel = LogLevel.Warning
    };

    private static async Task Tick(ServerIntegrationInstance server, ClientIntegrationInstance first,
        ClientIntegrationInstance second, int count = 30)
    {
        for (var i = 0; i < count; i++)
        {
            await server.WaitRunTicks(1);
            await first.WaitRunTicks(1);
            await second.WaitRunTicks(1);
        }
    }

    private static Task Input(ClientIntegrationInstance client, BoundKeyFunction function, BoundKeyState state)
        => client.WaitPost(() =>
        {
            var manager = client.ResolveDependency<IInputManager>();
            manager.ViewportKeyEvent(null, new BoundKeyEventArgs(function, state, default, false));
        });

    [Test, CancelAfter(120000)]
    public async Task TwoClientsMoveCollideToggleAndReconnect()
    {
        using var server = StartServer(ServerOptions());
        using var first = StartClient(ClientOptions());
        using var second = StartClient(ClientOptions());
        await first.WaitIdleAsync();
        await first.WaitAssertion(() =>
        {
            var input = first.ResolveDependency<IInputManager>();
            foreach (var function in new[] { EngineKeyFunctions.UIClick, EngineKeyFunctions.TextBackspace,
                         EngineKeyFunctions.TextSubmit, EngineKeyFunctions.TextPaste, EngineKeyFunctions.TextSelectAll,
                         EngineKeyFunctions.MoveUp, EngineKeyFunctions.MoveDown, EngineKeyFunctions.MoveLeft,
                         EngineKeyFunctions.MoveRight, SandboxInput.Interact })
                Assert.That(input.TryGetKeyBinding(function, out _), Is.True, $"Missing default binding: {function}");
        });
        await ConnectClient(server, first, "Alice");
        await ConnectClient(server, second, "Bobby");
        await Tick(server, first, second, 60);
        var players = server.ResolveDependency<ServerPlayers>();
        var world = server.System<SandboxWorldSystem>();
        var transform = server.System<SharedTransformSystem>();
        var alice = players.Sessions.Single(p => p.Name == "Alice");
        var bobby = players.Sessions.Single(p => p.Name == "Bobby");
        var pawn = alice.AttachedEntity!.Value;
        var other = bobby.AttachedEntity!.Value;
        Assert.That(pawn, Is.Not.EqualTo(other));
        Assert.That(alice.Status, Is.EqualTo(SessionStatus.InGame));
        var original = transform.GetMapCoordinates(pawn).Position;
        var otherOriginal = transform.GetMapCoordinates(other).Position;

        await Input(first, EngineKeyFunctions.MoveRight, BoundKeyState.Down);
        await Tick(server, first, second, 30);
        await Input(first, EngineKeyFunctions.MoveRight, BoundKeyState.Up);
        await Tick(server, first, second, 15);
        var stopped = transform.GetMapCoordinates(pawn).Position;
        Assert.That(stopped.X, Is.GreaterThan(original.X + 1));
        Assert.That(transform.GetMapCoordinates(other).Position, Is.EqualTo(otherOriginal));
        await Tick(server, first, second, 15);
        Assert.That(Vector2.Distance(transform.GetMapCoordinates(pawn).Position, stopped), Is.LessThan(0.01f));
        var netPawn = server.EntMan.GetNetEntity(pawn);
        var clientPawn = first.EntMan.GetEntity(netPawn);
        Assert.That(Vector2.Distance(first.System<SharedTransformSystem>().GetMapCoordinates(clientPawn).Position, stopped), Is.LessThan(0.15f));

        // Opposing inputs cancel through the real input/network stack.
        await Input(first, EngineKeyFunctions.MoveLeft, BoundKeyState.Down);
        await Input(first, EngineKeyFunctions.MoveRight, BoundKeyState.Down);
        await Tick(server, first, second, 15);
        var opposed = transform.GetMapCoordinates(pawn).Position;
        await Tick(server, first, second, 15);
        Assert.That(Vector2.Distance(transform.GetMapCoordinates(pawn).Position, opposed), Is.LessThan(0.01f));
        await Input(first, EngineKeyFunctions.MoveLeft, BoundKeyState.Up);
        await Input(first, EngineKeyFunctions.MoveRight, BoundKeyState.Up);

        // Measure velocity during diagonal movement, away from the walls.
        await server.WaitPost(() => transform.SetCoordinates(pawn, new EntityCoordinates(world.Grid, new Vector2(3.5f, 3.5f))));
        await Input(first, EngineKeyFunctions.MoveUp, BoundKeyState.Down);
        await Input(first, EngineKeyFunctions.MoveRight, BoundKeyState.Down);
        await Tick(server, first, second, 15);
        var velocity = server.EntMan.GetComponent<PhysicsComponent>(pawn).LinearVelocity;
        Assert.That(velocity.Length(), Is.EqualTo(4f).Within(0.01f));
        Assert.That(velocity.X, Is.EqualTo(velocity.Y).Within(0.01f));
        await Input(first, EngineKeyFunctions.MoveUp, BoundKeyState.Up);
        await Input(first, EngineKeyFunctions.MoveRight, BoundKeyState.Up);
        await Tick(server, first, second, 15);

        await Input(first, EngineKeyFunctions.MoveLeft, BoundKeyState.Down);
        await Tick(server, first, second, 120);
        await Input(first, EngineKeyFunctions.MoveLeft, BoundKeyState.Up);
        await Tick(server, first, second, 15);
        Assert.That(transform.GetMapCoordinates(pawn).Position.X, Is.GreaterThanOrEqualTo(1.25f), "Pawn crossed the enclosing wall.");

        await server.WaitPost(() => transform.SetCoordinates(pawn, new EntityCoordinates(world.Grid, new Vector2(6.5f, 6.5f))));
        await Tick(server, first, second);
        await Input(first, SandboxInput.Interact, BoundKeyState.Down);
        await Tick(server, first, second);
        Assert.That(server.EntMan.GetComponent<SandboxSwitchComponent>(world.Switch).Enabled, Is.True);

        await server.WaitPost(() =>
        {
            // A session attached to someone else's pawn is not allowed to interact.
            players.SetAttachedEntity(alice, other);
            transform.SetCoordinates(other, new EntityCoordinates(world.Grid, new Vector2(6.5f, 6.5f)));
            world.HandleInteraction(alice, BoundKeyState.Down);
            players.SetAttachedEntity(alice, pawn);
            players.SetAttachedEntity(bobby, other);
        });
        Assert.That(server.EntMan.GetComponent<SandboxSwitchComponent>(world.Switch).Enabled, Is.True);
        var netSwitch = server.EntMan.GetNetEntity(world.Switch);
        foreach (var client in new[] { first, second })
            Assert.That(client.EntMan.GetComponent<SandboxSwitchComponent>(client.EntMan.GetEntity(netSwitch)).Enabled, Is.True);
        await server.WaitPost(() => world.HandleInteraction(alice, BoundKeyState.Down));
        Assert.That(server.EntMan.GetComponent<SandboxSwitchComponent>(world.Switch).Enabled, Is.True, "Held input toggled twice.");
        await Input(first, SandboxInput.Interact, BoundKeyState.Up);
        await Tick(server, first, second);

        // Out-of-range and invalid sessions cannot change state.
        await server.WaitPost(() =>
        {
            transform.SetCoordinates(pawn, new EntityCoordinates(world.Grid, new Vector2(3.5f, 3.5f)));
            world.HandleInteraction(alice, BoundKeyState.Down);
            world.HandleInteraction(null, BoundKeyState.Down);
            world.HandleInteraction(alice, BoundKeyState.Up);
        });
        Assert.That(server.EntMan.GetComponent<SandboxSwitchComponent>(world.Switch).Enabled, Is.True);

        // Within two units, but across the interior wall.
        await server.WaitPost(() =>
        {
            transform.SetCoordinates(world.Switch, new EntityCoordinates(world.Grid, new Vector2(7.75f, 7.5f)));
            transform.SetCoordinates(pawn, new EntityCoordinates(world.Grid, new Vector2(9.35f, 7.5f)));
        });
        await Tick(server, first, second);
        await server.WaitPost(() => { world.HandleInteraction(alice, BoundKeyState.Down); world.HandleInteraction(alice, BoundKeyState.Up); });
        Assert.That(server.EntMan.GetComponent<SandboxSwitchComponent>(world.Switch).Enabled, Is.True, "Interaction passed through a wall.");

        for (var i = 0; i < 3; i++)
        {
            var old = alice.AttachedEntity!.Value;
            await DisconnectClient(server, first, "Test reconnect");
            await Tick(server, first, second);
            Assert.That(server.EntMan.EntityExists(old), Is.False);
            await server.WaitPost(() => world.HandleInteraction(alice, BoundKeyState.Down));
            await ConnectClient(server, first, "Alice");
            await Tick(server, first, second, 60);
            alice = players.Sessions.Single(p => p.Name == "Alice");
            Assert.That(alice.AttachedEntity, Is.Not.Null.And.Not.EqualTo(old));
            Assert.That(server.EntMan.GetComponent<SandboxPawnComponent>(alice.AttachedEntity!.Value).Buttons, Is.EqualTo(MoveButtons.None));
            Assert.That(server.EntMan.GetComponent<SandboxSwitchComponent>(world.Switch).Enabled, Is.True);
        }

        await server.WaitPost(() =>
        {
            server.EntMan.DeleteEntity(world.Switch);
            world.HandleInteraction(alice, BoundKeyState.Down);
        });
        await Tick(server, first, second);
    }

    [Test]
    public async Task ClientAssembliesPassEngineSandbox()
    {
        using var client = StartClient(ClientOptions());
        await client.WaitIdleAsync();
        await client.CheckSandboxed(typeof(Content.Client.Entry.EntryPoint).Assembly);
        await client.CheckSandboxed(typeof(Content.Shared.Entry.EntryPoint).Assembly);
    }
}
