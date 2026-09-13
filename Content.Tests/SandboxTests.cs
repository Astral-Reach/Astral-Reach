using System.Numerics;
using Content.Client.Connection;
using Content.Shared.Sandbox;
using NUnit.Framework;

namespace Content.Tests;

[TestFixture]
public sealed class SandboxTests
{
    [Test]
    public void AllInputCombinationsHaveBoundedNormalizedSpeed()
    {
        for (var value = 0; value < 16; value++)
        {
            var buttons = (MoveButtons) value;
            var direction = SandboxMovement.Direction(buttons);
            Assert.That(direction.Length(), Is.EqualTo(0).Within(0.00001).Or.EqualTo(1).Within(0.00001));
            Assert.That(float.IsFinite(direction.X) && float.IsFinite(direction.Y));
        }
    }

    [TestCase(MoveButtons.Up | MoveButtons.Down)]
    [TestCase(MoveButtons.Left | MoveButtons.Right)]
    [TestCase(MoveButtons.Up | MoveButtons.Down | MoveButtons.Left | MoveButtons.Right)]
    [TestCase(MoveButtons.None)]
    public void OppositeOrReleasedInputsStop(MoveButtons buttons)
        => Assert.That(SandboxMovement.Direction(buttons), Is.EqualTo(Vector2.Zero));

    [Test]
    public void DiagonalAndCardinalSpeedsMatch()
    {
        var diagonal = SandboxMovement.Direction(MoveButtons.Up | MoveButtons.Right);
        Assert.Multiple(() =>
        {
            Assert.That(diagonal.X, Is.GreaterThan(0));
            Assert.That(diagonal.Y, Is.GreaterThan(0));
            Assert.That(diagonal.Length(), Is.EqualTo(SandboxMovement.Direction(MoveButtons.Right).Length()).Within(0.00001));
        });
    }

    [TestCase("localhost", "localhost", 1212)]
    [TestCase(" example.test:4000 ", "example.test", 4000)]
    [TestCase("127.0.0.1", "127.0.0.1", 1212)]
    [TestCase("127.0.0.1:65535", "127.0.0.1", 65535)]
    [TestCase("[::1]", "::1", 1212)]
    [TestCase("[2001:db8::1]:1234", "2001:db8::1", 1234)]
    public void ParsesSupportedEndpoints(string text, string host, int port)
    {
        Assert.That(ServerAddress.TryParse(text, 1212, out var endpoint, out var error), Is.True, error);
        Assert.Multiple(() =>
        {
            Assert.That(endpoint!.Host, Is.EqualTo(host));
            Assert.That(endpoint.Port, Is.EqualTo(port));
        });
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase(":1212")]
    [TestCase("localhost:")]
    [TestCase("localhost:0")]
    [TestCase("localhost:65536")]
    [TestCase("localhost:-1")]
    [TestCase("localhost:1x")]
    [TestCase("localhost: 12")]
    [TestCase("local host")]
    [TestCase("http://localhost")]
    [TestCase("::1")]
    [TestCase("[::1")]
    [TestCase("[::1]garbage")]
    [TestCase("[::1]:")]
    [TestCase("[localhost]")]
    [TestCase("999.1.1.1")]
    [TestCase("-host")]
    [TestCase("host-")]
    [TestCase("host..test")]
    public void RejectsMalformedEndpoints(string text)
    {
        Assert.That(ServerAddress.TryParse(text, 1212, out var endpoint, out var error), Is.False);
        Assert.That(endpoint, Is.Null);
        Assert.That(error, Is.Not.Empty);
    }

    [TestCase("server --platform win-x64 --hybrid-acz", true)]
    [TestCase("server --platform linux-x64 --configuration Release", true)]
    [TestCase("client", true)]
    [TestCase("server --platform win-typo", false)]
    [TestCase("server --platfrom win-x64", false)]
    [TestCase("server --configuration Tools", false)]
    [TestCase("client --hybrid-acz", false)]
    [TestCase("nonsense", false)]
    public void PackagingArgumentsCannotSilentlySkipRequestedWork(string arguments, bool valid)
        => Assert.That(Content.Packaging.CommandLineArgs.TryParse(arguments.Split(' '), out _), Is.EqualTo(valid));
}
