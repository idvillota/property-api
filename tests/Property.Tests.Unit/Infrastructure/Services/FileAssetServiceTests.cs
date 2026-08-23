using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Property.Application.Common.Options;
using Property.Infrastructure.Services;
using Property.Infrastructure.Storage;
using Property.Tests.Unit.Support;

namespace Property.Tests.Unit.Infrastructure.Services;

public sealed class FileAssetServiceTests
{
    private static (FileAssetService Sut, string Root) CreateSut(TenantDbFixture fx, long maxBytes = 1024)
    {
        var root = Path.Combine(Path.GetTempPath(), "property-filestore-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var storage = new LocalDiskFileStorage(root);
        var options = Options.Create(new FileStorageOptions { MaxBytes = maxBytes, Provider = "Local" });
        var sut = new FileAssetService(fx.Db, storage, fx.TenantContext, fx.UnitOfWork, options);
        return (sut, root);
    }

    [Fact]
    public async Task FileAsset_ScopedByTenant()
    {
        using var fx = new TenantDbFixture();
        var (sut, root) = CreateSut(fx);
        try
        {
            await using var upload = new MemoryStream(Encoding.UTF8.GetBytes("hola tenant A"));
            var asset = await sut.UploadAsync(upload, "nota.txt", "text/plain", null, "pqrs");

            var downloadA = await sut.DownloadAsync(asset.Id);
            Assert.NotNull(downloadA);
            await using (downloadA!.Content)
            {
                using var reader = new StreamReader(downloadA.Content);
                Assert.Equal("hola tenant A", await reader.ReadToEndAsync());
            }

            var otherTenant = Guid.NewGuid();
            fx.TenantContext.TenantId = otherTenant;

            Assert.Null(await sut.GetByIdAsync(asset.Id));
            Assert.Null(await sut.DownloadAsync(asset.Id));

            // Row still exists globally (cross-tenant isolation is filter-based).
            Assert.Equal(1, await fx.Db.FileAssets.IgnoreQueryFilters().CountAsync(f => f.Id == asset.Id));
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task FileUpload_RejectsOversize()
    {
        using var fx = new TenantDbFixture();
        var (sut, root) = CreateSut(fx, maxBytes: 8);
        try
        {
            await using var upload = new MemoryStream(Encoding.UTF8.GetBytes("0123456789"));
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.UploadAsync(upload, "big.txt", "text/plain", 10, null));

            Assert.Contains("maximum allowed size", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, await fx.Db.FileAssets.CountAsync());
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
