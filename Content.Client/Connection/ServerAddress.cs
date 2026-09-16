using System.Net;
using System.Text.RegularExpressions;

namespace Content.Client.Connection;

/// <summary>Direct-connect endpoint parsing, without interpreting URLs or silently truncating addresses.</summary>
public static class ServerAddress
{
    private static readonly Regex NumericAddress = new(@"^[0-9.]+$");
    private static readonly Regex Hostname = new(@"^(?=.{1,253}$)(?:[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?\.)*[A-Za-z0-9](?:[A-Za-z0-9-]{0,61}[A-Za-z0-9])?\.?$");

    public static bool TryParse(string input, ushort defaultPort, out ServerEndpoint? endpoint, out string error)
    {
        endpoint = null;
        error = "Enter a hostname, IPv4 address, or [IPv6 address], optionally followed by :port.";
        var text = input.Trim();
        if (text.Length == 0)
            return false;
        string host;
        string? portText = null;
        if (text.StartsWith('['))
        {
            var close = text.IndexOf(']');
            if (close < 2)
                return false;
            host = text[1..close];
            if (!IPAddress.TryParse(host, out var ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
                return false;
            if (close < text.Length - 1)
            {
                if (text[close + 1] != ':')
                    return false;
                portText = text[(close + 2)..];
            }
        }
        else
        {
            var split = text.Split(':');
            if (split.Length > 2)
                return false;
            host = split[0];
            if (split.Length == 2)
                portText = split[1];
            if (!Hostname.IsMatch(host))
                return false;
            if (NumericAddress.IsMatch(host) &&
                (!IPAddress.TryParse(host, out var ipv4) || ipv4.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork ||
                 host.Split('.').Length != 4))
                return false;
        }
        var port = defaultPort;
        if (portText != null && (!ushort.TryParse(portText, System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out port) || port == 0))
        {
            error = "Port must be between 1 and 65535.";
            return false;
        }
        endpoint = new ServerEndpoint(host, port);
        error = "";
        return true;
    }
}

public sealed record ServerEndpoint(string Host, ushort Port);
