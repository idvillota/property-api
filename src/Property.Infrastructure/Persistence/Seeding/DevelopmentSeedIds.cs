namespace Property.Infrastructure.Persistence.Seeding;

/// <summary>Stable ids for local demo seed (idempotent re-runs).</summary>
public static class DevelopmentSeedIds
{
    public static readonly Guid TenantId = Guid.Parse("a1000001-0001-4001-8001-000000000001");
    public static readonly Guid AdminUserId = Guid.Parse("a1000002-0002-4002-8002-000000000002");
    public static readonly Guid ManagerUserId = Guid.Parse("a1000003-0003-4003-8003-000000000003");
    public static readonly Guid AdminRoleId = Guid.Parse("a1000004-0004-4004-8004-000000000004");
    public static readonly Guid ManagerRoleId = Guid.Parse("a1000005-0005-4005-8005-000000000005");
    public static readonly Guid AdminTenantUserId = Guid.Parse("a1000006-0006-4006-8006-000000000006");
    public static readonly Guid ManagerTenantUserId = Guid.Parse("a1000007-0007-4007-8007-000000000007");
    public static readonly Guid PersonalAdminUserId = Guid.Parse("a1000008-0008-4008-8008-000000000008");
    public static readonly Guid PersonalAdminTenantUserId = Guid.Parse("a1000009-0009-4009-8009-000000000009");

    public static readonly Guid NeighborhoodPandiacoId = Guid.Parse("a1000010-0010-4010-8010-000000000010");
    public static readonly Guid NeighborhoodMaridiazId = Guid.Parse("a1000011-0011-4011-8011-000000000011");
    public static readonly Guid PropertyId = Guid.Parse("a1000012-0012-4012-8012-000000000012");
    public static readonly Guid BuildingId = Guid.Parse("a1000013-0013-4013-8013-000000000013");

    public static readonly Guid ReceptionEmployeeId = Guid.Parse("a1000014-0014-4014-8014-000000000014");
    public static readonly Guid AdminEmployeeId = Guid.Parse("a1000015-0015-4015-8015-000000000015");

    public static readonly Guid SocialRoomId = Guid.Parse("a1000020-0020-4020-8020-000000000020");
    public static readonly Guid BbqId = Guid.Parse("a1000021-0021-4021-8021-000000000021");
    public static readonly Guid CourtId = Guid.Parse("a1000022-0022-4022-8022-000000000022");

    public const string TenantSlug = "demo-admin";
    public const string AdminEmail = "admin@property.demo";
    public const string ManagerEmail = "manager@property.demo";
    public const string PersonalAdminEmail = "idvillota@gmail.com";
    public const string DefaultPassword = "Demo123!";
}
