namespace Property.Application.Common.Interfaces;

public interface IQrCodePngRenderer
{
    byte[] Render(string payload);
}
