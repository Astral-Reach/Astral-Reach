using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Robust.Client;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.UnitTesting;

namespace Content.IntegrationTests;

/// <summary>Real UDP failure/cancellation through the content menu, without external servers or authentication.</summary>
[TestFixture, NonParallelizable]
public sealed class ConnectionFlowTests : RobustIntegrationTest
{
    private static IEnumerable<Control> Descendants(Control control)
    {
        yield return control;
        foreach (var child in control.Children)
        foreach (var descendant in Descendants(child))
            yield return descendant;
    }

    private static async Task Until(ClientIntegrationInstance client, Func<bool> predicate, int seconds = 15)
    {
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed.TotalSeconds < seconds)
        {
            await client.WaitRunTicks(1);
            if (predicate())
                return;
            await Task.Delay(10);
        }
        Assert.Fail("Timed out waiting for the connection state.");
    }

    [Test, CancelAfter(60000)]
    public async Task MenuValidatesCancelsAndRetriesAnUnavailableServer()
    {
        // Own a UDP port that deliberately never replies. No dependence on DNS or outside services.
        using var unavailable = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint) unavailable.Client.LocalEndPoint!).Port;
        var options = new ClientIntegrationOptions
        {
            Pool = false, ContentStart = true, LoadTestAssembly = false,
            ContentAssemblies = [typeof(Content.Client.Entry.EntryPoint).Assembly, typeof(Content.Shared.Entry.EntryPoint).Assembly],
            Options = new() { LoadConfigAndUserData = false },
            FailureLogLevel = LogLevel.Warning,
            InitIoC = () =>
            {
                IoCManager.Register<INetManager, NetManager>(true);
                IoCManager.Register<IClientNetManager, NetManager>(true);
            },
            BeforeStart = () => IoCManager.Resolve<IBaseClient>().PlayerNameOverride = "CommandLineName"
        };
        options.CVarOverrides.Add("net.connection_timeout", "1");
        options.CVarOverrides.Add("net.handshake_attempts", "1");
        using var client = StartClient(options);
        await client.WaitRunTicks(2);
        var controls = Descendants(client.ResolveDependency<IUserInterfaceManager>().StateRoot).ToArray();
        var username = controls.OfType<LineEdit>().Single(c => c.Name == "Username");
        var address = controls.OfType<LineEdit>().Single(c => c.Name == "ServerAddress");
        var status = controls.OfType<Label>().Single(c => c.Name == "ConnectionStatus");
        var cancel = controls.OfType<Button>().Single(c => c.Text == "Cancel");
        var connect = controls.OfType<Button>().Single(c => c.Text == "Connect");
        var engine = client.ResolveDependency<IBaseClient>();
        Assert.That(username.Text, Is.EqualTo("CommandLineName"));
        Assert.That(address.Text, Is.EqualTo("localhost"));
        await client.WaitPost(() => { username.Text = "a!"; address.ForceSubmitText(); });
        Assert.That(status.Text, Does.Contain("valid username"));
        Assert.That(engine.RunLevel, Is.EqualTo(ClientRunLevel.Initialize));
        await client.WaitPost(() => { username.Text = "MenuName"; address.Text = "localhost:0"; address.ForceSubmitText(); });
        Assert.That(status.Text, Does.Contain("Port"));
        Assert.That(engine.RunLevel, Is.EqualTo(ClientRunLevel.Initialize));

        for (var attempt = 0; attempt < 3; attempt++)
        {
            await client.WaitPost(() => { address.Text = $"127.0.0.1:{port}"; address.ForceSubmitText(); });
            Assert.That(engine.PlayerNameOverride, Is.Null, "The menu did not replace the command-line username.");
            Assert.That(connect.Disabled, Is.True);
            Assert.That(cancel.Visible, Is.True);
            await client.WaitRunTicks(2);
            var pointer = new ScreenCoordinates(cancel.GlobalPixelPosition + cancel.PixelSize / 2, default);
            foreach (var state in new[] { BoundKeyState.Down, BoundKeyState.Up })
                await client.DoGuiEvent(cancel, new GUIBoundKeyEventArgs(EngineKeyFunctions.UIClick, state, pointer, true, default, default));
            Assert.That(status.Text, Is.EqualTo("Connection cancelled."));
            Assert.That(connect.Disabled, Is.False);
            Assert.That(cancel.Visible, Is.False);
        }

        await client.WaitPost(address.ForceSubmitText);
        await Until(client, () => engine.RunLevel == ClientRunLevel.Initialize && !connect.Disabled);
        Assert.That(status.Text, Is.Not.Empty.And.Not.EqualTo("Connection cancelled."));
        Assert.That(connect.Text, Is.EqualTo("Retry"));
        await client.WaitPost(address.ForceSubmitText);
        Assert.That(connect.Disabled, Is.True, "Retry did not start a fresh connection.");
        await Until(client, () => engine.RunLevel == ClientRunLevel.Initialize && !connect.Disabled);
    }
}
