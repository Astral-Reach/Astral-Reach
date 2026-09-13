using Robust.Client;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared;
using Robust.Shared.AuthLib;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Client.Connection;

/// <summary>One persistent connection screen: subscriptions are installed once, independent of reconnects.</summary>
public sealed class SandboxScreen : IDisposable
{
    private readonly IBaseClient _client;
    private readonly IClientNetManager _network;
    private readonly IGameController _controller;
    private readonly IConfigurationManager _config;
    private readonly LineEdit _username = new() { Name = "Username" };
    private readonly LineEdit _address = new() { Name = "ServerAddress", Text = "localhost" };
    private readonly Label _status = new() { Name = "ConnectionStatus", Text = "Start the local server, then connect." };
    private readonly Button _connect = new() { Text = "Connect" };
    private readonly Button _cancel = new() { Text = "Cancel", Visible = false };
    private readonly Button _redial = new() { Text = "Reconnect through launcher", Visible = false };
    private readonly BoxContainer _menu;
    private readonly BoxContainer _hud;
    private readonly bool _fromLauncher;
    private bool _cancelled;
    private bool _disposed;

    public SandboxScreen(IDependencyCollection dependencies)
    {
        _client = dependencies.Resolve<IBaseClient>();
        _network = dependencies.Resolve<IClientNetManager>();
        _controller = dependencies.Resolve<IGameController>();
        _fromLauncher = _controller.LaunchState.FromLauncher;
        _config = dependencies.Resolve<IConfigurationManager>();
        var ui = dependencies.Resolve<IUserInterfaceManager>();
        _username.Text = _client.PlayerNameOverride ?? _config.GetCVar(CVars.PlayerName);
        _menu = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalAlignment = Control.HAlignment.Center,
            VerticalAlignment = Control.VAlignment.Center,
            MinWidth = 440,
            SeparationOverride = 12
        };
        _menu.AddChild(new Label { Text = "ASTRAL REACH", HorizontalAlignment = Control.HAlignment.Center });
        _menu.AddChild(new Label { Text = "Development sandbox" });
        _menu.AddChild(new Label { Text = "Username" });
        _menu.AddChild(_username);
        _menu.AddChild(new Label { Text = "Server address" });
        _menu.AddChild(_address);
        _menu.AddChild(_connect);
        _menu.AddChild(_cancel);
        _menu.AddChild(_redial);
        _menu.AddChild(_status);
        var quit = new Button { Text = "Quit" };
        quit.OnPressed += _ => _controller.Shutdown();
        _menu.AddChild(quit);
        ui.StateRoot.AddChild(_menu);

        _hud = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalAlignment = Control.HAlignment.Left,
            VerticalAlignment = Control.VAlignment.Top,
            Margin = new Thickness(16),
            Visible = false
        };
        _hud.AddChild(new Label { Text = "Astral Reach  |  WASD: move  |  E: toggle nearby object" });
        var disconnect = new Button { Text = "Disconnect" };
        disconnect.OnPressed += _ => _client.DisconnectFromServer("Disconnected by player.");
        _hud.AddChild(disconnect);
        ui.StateRoot.AddChild(_hud);

        _connect.OnPressed += _ => Connect();
        _address.OnTextEntered += _ => Connect();
        _cancel.OnPressed += _ =>
        {
            _cancelled = true;
            _client.DisconnectFromServer("Connection cancelled.");
            SetBusy(false);
            _status.Text = "Connection cancelled.";
        };
        _redial.OnPressed += _ => Redial();
        _client.RunLevelChanged += OnRunLevelChanged;
        _network.ConnectFailed += OnConnectFailed;
        _network.Disconnect += OnDisconnected;
        _network.ClientConnectStateChanged += OnConnectStateChanged;
        if (_controller.LaunchState.ConnectEndpoint is { } endpoint)
        {
            _address.Text = _controller.LaunchState.Ss14Address ?? _controller.LaunchState.ConnectAddress ?? "localhost";
            _username.Editable = !_fromLauncher;
            _address.Editable = !_fromLauncher;
            _redial.Visible = _controller.LaunchState.Ss14Address != null;
            // The engine starts the initial launcher connection after content PostInit.
        }
    }

    private void Connect()
    {
        if (_client.RunLevel != ClientRunLevel.Initialize &&
            !(_cancelled && _client.RunLevel == ClientRunLevel.Connecting &&
              _network.ClientConnectState == ClientConnectionState.NotConnecting))
            return;
        if (_fromLauncher && _controller.LaunchState.ConnectEndpoint is { } launcherEndpoint)
        {
            _cancelled = false;
            SetBusy(true);
            _client.ConnectToServer(launcherEndpoint);
            return;
        }
        if (!UsernameHelpers.IsNameValid(_username.Text.Trim(), out _))
        {
            _status.Text = "Use a valid username: 3–32 letters, numbers, or underscores.";
            return;
        }
        if (!ServerAddress.TryParse(_address.Text, _client.DefaultPort, out var endpoint, out var error))
        {
            _status.Text = error;
            return;
        }
        _config.SetCVar(CVars.PlayerName, _username.Text.Trim());
        _client.PlayerNameOverride = null;
        _cancelled = false;
        SetBusy(true);
        try
        {
            _client.ConnectToServer(endpoint!.Host, endpoint.Port);
        }
        catch (ArgumentException exception)
        {
            _status.Text = exception.Message;
            SetBusy(false);
        }
    }

    private void OnRunLevelChanged(object? sender, RunLevelChangedEventArgs args)
    {
        var inGame = args.NewLevel == ClientRunLevel.InGame;
        _menu.Visible = !inGame;
        _hud.Visible = inGame;
        SetBusy(args.NewLevel is ClientRunLevel.Connecting or ClientRunLevel.Connected);
        if (args.NewLevel == ClientRunLevel.Initialize)
        {
            _connect.Text = "Reconnect";
            if (_client.LastDisconnectReason is { Length: > 0 } reason)
                _status.Text = reason;
        }
    }

    private void OnConnectFailed(object? sender, NetConnectFailArgs args)
    {
        if (_cancelled)
            return;
        _status.Text = args.Reason;
        _connect.Text = "Retry";
        SetBusy(false);
        if (args.RedialFlag)
            Redial();
    }

    private void OnConnectStateChanged(ClientConnectionState state)
    {
        if (!_cancelled && state != ClientConnectionState.NotConnecting &&
            _client.RunLevel == ClientRunLevel.Connecting)
            _status.Text = $"Connecting: {state}";
    }

    private void OnDisconnected(object? sender, NetDisconnectedArgs args)
    {
        if (!_cancelled && args.RedialFlag)
            Redial();
    }

    private void Redial()
    {
        if (_controller.LaunchState.Ss14Address is not { } address)
            return;
        try
        {
            _controller.Redial(address);
        }
        catch (Exception exception)
        {
            _status.Text = $"Launcher reconnect failed: {exception.Message}";
        }
    }

    private void SetBusy(bool busy)
    {
        _connect.Disabled = busy;
        _username.Editable = !busy && !_fromLauncher;
        _address.Editable = !busy && !_fromLauncher;
        _cancel.Visible = busy;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _client.RunLevelChanged -= OnRunLevelChanged;
        _network.ConnectFailed -= OnConnectFailed;
        _network.Disconnect -= OnDisconnected;
        _network.ClientConnectStateChanged -= OnConnectStateChanged;
        _menu.Orphan();
        _hud.Orphan();
    }

}
