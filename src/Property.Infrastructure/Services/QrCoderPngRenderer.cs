using Property.Application.Common.Interfaces;
using QRCoder;

namespace Property.Infrastructure.Services;

public sealed class QrCoderPngRenderer : IQrCodePngRenderer
{
    public byte[] Render(string payload)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(8);
    }
}
