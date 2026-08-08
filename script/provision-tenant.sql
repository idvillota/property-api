-- =============================================================================
-- Property — Provision new tenant (PostgreSQL)
-- =============================================================================
-- Creates a tenant with Administrator/Manager roles, all platform features,
-- TenantSettings, and two starter users (password Demo123!).
--
-- Prerequisites:
--   1. EF migrations applied (dotnet ef database update)
--   2. Features seeded by API PermissionBootstrap (start the API once first)
--   3. Edit CONFIGURATION below, then run against the target database
--
-- Prefer POST /api/auth/register-tenant for normal onboarding.
-- =============================================================================

DO $provision$
DECLARE
    p_tenant_name          text := 'Property Demo';
    p_tenant_slug          text := 'property-demo';
    p_timezone_id          text := 'America/Bogota';
    p_currency_code        text := 'COP';

    p_trade_name           text := 'Property Demo';
    p_legal_name           text := 'Property Demo S.A.S.';
    p_tax_id               text := '900123456-1';
    p_tax_regime           text := 'Simplified Regime';
    p_legal_representative text := 'Admin User';
    p_address_line         text := 'Calle 10 # 20-30';
    p_city                 text := 'Bogota';
    p_country              text := 'Colombia';
    p_postal_code          text := '110111';
    p_phone                text := '+57 300 000 0000';

    -- BCrypt for Demo123!
    p_password_hash        text := '$2y$11$ARhnNBSjxwmZPw37dVi69Ootnsb1PekP/yjF181SaFqJ2JCHcE7cO';

    p_admin_email          text := 'idvillota@gmail.com';
    p_manager_email        text := 'manager@example.com';

    v_now                  timestamptz := now() AT TIME ZONE 'UTC';
    v_tenant_id            uuid := gen_random_uuid();
    v_role_admin_id        uuid := gen_random_uuid();
    v_role_manager_id      uuid := gen_random_uuid();
    v_user_admin_id        uuid;
    v_user_manager_id      uuid;
    v_tu_admin_id          uuid := gen_random_uuid();
    v_tu_manager_id        uuid := gen_random_uuid();
    v_feature              record;
BEGIN
    INSERT INTO "Tenants" ("Id", "Name", "Slug", "TimeZoneId", "CurrencyCode", "IsActive", "CreatedAtUtc", "UpdatedAtUtc")
    VALUES (v_tenant_id, p_tenant_name, p_tenant_slug, p_timezone_id, p_currency_code, true, v_now, v_now);

    INSERT INTO "TenantSettings" (
        "TenantId", "TradeName", "LegalName", "TaxRegime", "TaxId", "LegalRepresentative",
        "AddressLine", "City", "Country", "PostalCode", "Phone", "DashboardLayoutJson"
    ) VALUES (
        v_tenant_id, p_trade_name, p_legal_name, p_tax_regime, p_tax_id, p_legal_representative,
        p_address_line, p_city, p_country, p_postal_code, p_phone, NULL
    );

    INSERT INTO "Roles" ("Id", "TenantId", "Name", "NormalizedName", "CreatedAtUtc", "UpdatedAtUtc")
    VALUES
        (v_role_admin_id, v_tenant_id, 'Administrator', 'ADMINISTRATOR', v_now, v_now),
        (v_role_manager_id, v_tenant_id, 'Manager', 'MANAGER', v_now, v_now);

    FOR v_feature IN SELECT "Id" FROM "Features"
    LOOP
        INSERT INTO "RoleFeatures" ("Id", "TenantId", "RoleId", "FeatureId", "CreatedAtUtc", "UpdatedAtUtc")
        VALUES (gen_random_uuid(), v_tenant_id, v_role_admin_id, v_feature."Id", v_now, v_now);

        INSERT INTO "RoleFeatures" ("Id", "TenantId", "RoleId", "FeatureId", "CreatedAtUtc", "UpdatedAtUtc")
        VALUES (gen_random_uuid(), v_tenant_id, v_role_manager_id, v_feature."Id", v_now, v_now);
    END LOOP;

    SELECT "Id" INTO v_user_admin_id FROM "Users" WHERE "NormalizedEmail" = upper(p_admin_email);
    IF v_user_admin_id IS NULL THEN
        v_user_admin_id := gen_random_uuid();
        INSERT INTO "Users" ("Id", "Email", "NormalizedEmail", "PasswordHash", "DisplayName", "CreatedAtUtc", "UpdatedAtUtc")
        VALUES (v_user_admin_id, p_admin_email, upper(p_admin_email), p_password_hash, 'Administrator', v_now, v_now);
    END IF;

    SELECT "Id" INTO v_user_manager_id FROM "Users" WHERE "NormalizedEmail" = upper(p_manager_email);
    IF v_user_manager_id IS NULL THEN
        v_user_manager_id := gen_random_uuid();
        INSERT INTO "Users" ("Id", "Email", "NormalizedEmail", "PasswordHash", "DisplayName", "CreatedAtUtc", "UpdatedAtUtc")
        VALUES (v_user_manager_id, p_manager_email, upper(p_manager_email), p_password_hash, 'Manager', v_now, v_now);
    END IF;

    INSERT INTO "TenantUsers" ("Id", "TenantId", "UserId", "IsActive", "BrandTheme", "ColorScheme", "CreatedAtUtc", "UpdatedAtUtc")
    VALUES
        (v_tu_admin_id, v_tenant_id, v_user_admin_id, true, 'operations', 'auto', v_now, v_now),
        (v_tu_manager_id, v_tenant_id, v_user_manager_id, true, 'operations', 'auto', v_now, v_now);

    INSERT INTO "TenantUserRoles" ("Id", "TenantId", "TenantUserId", "RoleId", "CreatedAtUtc", "UpdatedAtUtc")
    VALUES
        (gen_random_uuid(), v_tenant_id, v_tu_admin_id, v_role_admin_id, v_now, v_now),
        (gen_random_uuid(), v_tenant_id, v_tu_manager_id, v_role_manager_id, v_now, v_now);

    RAISE NOTICE 'Tenant provisioned: % (slug=%)', p_tenant_name, p_tenant_slug;
END
$provision$;
