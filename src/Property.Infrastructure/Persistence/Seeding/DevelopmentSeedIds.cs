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

    /// <summary>Second PH for holding / multi-conjunto demos (DIFF-002).</summary>
    public static readonly Guid PropertyBId = Guid.Parse("a1000130-0130-4130-8130-000000000130");
    public static readonly Guid BuildingBId = Guid.Parse("a1000131-0131-4131-8131-000000000131");
    public static readonly Guid FloorBId = Guid.Parse("a1000132-0132-4132-8132-000000000132");
    public static readonly Guid UnitB1Id = Guid.Parse("a1000133-0133-4133-8133-000000000133");

    public static readonly Guid ReceptionistUserId = Guid.Parse("a1000040-0040-4040-8040-000000000040");
    public static readonly Guid ReceptionistTenantUserId = Guid.Parse("a1000041-0041-4041-8041-000000000041");
    public static readonly Guid ReceptionistRoleId = Guid.Parse("a1000042-0042-4042-8042-000000000042");
    public static readonly Guid AccountantUserId = Guid.Parse("a1000043-0043-4043-8043-000000000043");
    public static readonly Guid AccountantTenantUserId = Guid.Parse("a1000044-0044-4044-8044-000000000044");
    public static readonly Guid AccountantRoleId = Guid.Parse("a1000045-0045-4045-8045-000000000045");
    public static readonly Guid OwnerUserId = Guid.Parse("a1000046-0046-4046-8046-000000000046");
    public static readonly Guid OwnerTenantUserId = Guid.Parse("a1000047-0047-4047-8047-000000000047");
    public static readonly Guid OwnerRoleId = Guid.Parse("a1000048-0048-4048-8048-000000000048");
    public static readonly Guid OwnerPersonId = Guid.Parse("c1000000-0000-4000-8000-000000000001");
    public static readonly Guid OwnerUserPersonLinkId = Guid.Parse("a1000049-0049-4049-8049-000000000049");

    public static readonly Guid DunningRuleId = Guid.Parse("a1000050-0050-4050-8050-000000000050");
    public static readonly Guid VisitorParkingSettingsId = Guid.Parse("a1000051-0051-4051-8051-000000000051");
    public static readonly Guid PollId = Guid.Parse("a1000052-0052-4052-8052-000000000052");
    public static readonly Guid AssemblyId = Guid.Parse("a1000053-0053-4053-8053-000000000053");
    public static readonly Guid AssemblyMotionId = Guid.Parse("a1000054-0054-4054-8054-000000000054");
    public static readonly Guid LibraryFileAssetId = Guid.Parse("a1000055-0055-4055-8055-000000000055");
    public static readonly Guid LibraryDocumentId = Guid.Parse("a1000056-0056-4056-8056-000000000056");
    public static readonly Guid WebhookSubscriptionId = Guid.Parse("a1000057-0057-4057-8057-000000000057");
    public static readonly Guid MuralPostId = Guid.Parse("a1000058-0058-4058-8058-000000000058");

    public const string TenantSlug = "demo-admin";
    public const string AdminEmail = "admin@property.demo";
    public const string ManagerEmail = "manager@property.demo";
    public const string ReceptionistEmail = "porteria@property.demo";
    public const string AccountantEmail = "contador@property.demo";
    public const string OwnerEmail = "propietario@property.demo";
    public const string PersonalAdminEmail = "idvillota@gmail.com";
    public const string DefaultPassword = "Demo123!";
}
