using Lidgren.Network;
using NUnit.Framework;
using Robust.Shared.Network.Messages;
using Robust.Shared.Network.Messages.Handshake;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.PixelFormats;

namespace Robust.Shared.Tests.Networking;

[TestFixture]
public sealed class SecurityBackportTest
{
    [Test]
    public void ImageLoadingRejectsTiffButAcceptsPng()
    {
        using var image = new Image<Rgba32>(1, 1);
        using var tiff = new MemoryStream();
        image.Save(tiff, new TiffEncoder());
        tiff.Position = 0;
        Assert.Throws<UnknownImageFormatException>(() =>
        {
            using var loaded = Image.Load(tiff);
        });

        using var png = new MemoryStream();
        image.Save(png, new PngEncoder());
        png.Position = 0;
        using var decoded = Image.Load(png);
        Assert.That(decoded.Size, Is.EqualTo(image.Size));
    }

    // Lidgren does not expose a public constructor for received messages.
    private static NetIncomingMessage Incoming()
    {
        return (NetIncomingMessage) Activator.CreateInstance(typeof(NetIncomingMessage), nonPublic: true)!;
    }

    [TestCase(-1)]
    [TestCase(1024)]
    [TestCase(int.MaxValue)]
    public void StringTableRejectsInvalidPayloadLength(int length)
    {
        var buffer = Incoming();
        buffer.WriteVariableInt32(length);
        buffer.Position = 0;

        Assert.Throws<InvalidOperationException>(() => new MsgMapStrStrings().ReadFromBuffer(buffer, null!));
    }

    [TestCase(-1)]
    [TestCase(1025)]
    [TestCase(int.MaxValue)]
    public void HandshakeRejectsInvalidFieldLength(int length)
    {
        var buffer = Incoming();
        buffer.Write(Guid.Empty.ToByteArray());
        buffer.WriteVariableInt32(length);
        buffer.Position = 0;

        Assert.Throws<InvalidOperationException>(() => new MsgEncryptionResponse().ReadFromBuffer(buffer, null!));
    }

    [Test]
    public void HandshakeRoundTrip()
    {
        var original = new MsgEncryptionResponse
        {
            UserId = Guid.NewGuid(),
            SealedData = [1, 2, 3],
            LegacyHwid = [4, 5]
        };
        var peer = new NetClient(new NetPeerConfiguration("security-backport-test"));
        var outgoing = peer.CreateMessage();
        original.WriteToBuffer(outgoing, null!);
        var incoming = Incoming();
        incoming.Data = outgoing.Data;
        incoming.LengthBits = outgoing.LengthBits;
        var decoded = new MsgEncryptionResponse();
        decoded.ReadFromBuffer(incoming, null!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.UserId, Is.EqualTo(original.UserId));
            Assert.That(decoded.SealedData, Is.EqualTo(original.SealedData));
            Assert.That(decoded.LegacyHwid, Is.EqualTo(original.LegacyHwid));
        });
    }
}
