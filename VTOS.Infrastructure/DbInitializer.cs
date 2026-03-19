using Microsoft.EntityFrameworkCore;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;
using VTOS.Infrastructure.Persistence;

namespace VTOS.Infrastructure;

/// <summary>
/// Seeds initial test data into the database at application startup.
/// Runs only when the Role table is empty — safe to call on every startup.
///
/// All seed data is scoped to Đà Nẵng city with realistic information.
/// Business flow: Contract → Campaign → Order → Payment → Production → Delivery
///
/// Test accounts (password: Test@1234):
///   admin@vtos.com      — Admin role
///   school1@vtos.com    — School role, linked to THPT Phan Châu Trinh
///   school2@vtos.com    — School role, linked to THPT Trần Phú
///   school3@vtos.com    — School role, linked to THCS Nguyễn Huệ
///   parent0@vtos.com    — Parent role (Trần Thị Hương)
///   parent1@vtos.com    — Parent role (Lê Văn Đức)
///   parent2@vtos.com    — Parent role (Phạm Thị Mai)
///   parent3@vtos.com    — Parent role (Ngô Quang Hải)
///   provider1@vtos.com  — Provider role, linked to May Mặc Hoàng Gia
///   provider2@vtos.com  — Provider role, linked to Đồng Phục Sơn Trà
/// </summary>
public static class DbInitializer
{
    // ── Fixed GUIDs ────────────────────────────────────────────────────────────
    private static readonly Guid ROLE_ADMIN    = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ROLE_PARENT   = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ROLE_SCHOOL   = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ROLE_PROVIDER = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static readonly Guid SCH1 = Guid.Parse("6D3CCB42-97FF-44D4-AC8B-68FC56B4DDD9");
    private static readonly Guid SCH2 = Guid.Parse("D25F24A9-29F5-4FD9-B7A7-CD224EA512C5");
    private static readonly Guid SCH3 = Guid.Parse("DC280EA8-E2D7-442B-9C0B-2F71A4FFE663");

    private static readonly Guid USR_SCH1 = Guid.Parse("A1000001-0000-0000-0000-000000000001");
    private static readonly Guid USR_SCH2 = Guid.Parse("A1000002-0000-0000-0000-000000000002");
    private static readonly Guid USR_SCH3 = Guid.Parse("A1000003-0000-0000-0000-000000000003");

    private static readonly Guid USR_P0 = Guid.Parse("4720A43C-B0C3-4EAD-B937-62FDE2A8F4D6");
    private static readonly Guid USR_P1 = Guid.Parse("2F5FE30D-C8F7-4AA0-B31D-3D3859D60F5A");
    private static readonly Guid USR_P2 = Guid.Parse("CF748A1E-6707-46D1-9702-2820248D7436");
    private static readonly Guid USR_P3 = Guid.Parse("86114525-C907-4EB8-BF49-C9AF2AB29185");

    private static readonly Guid USR_PRV1 = Guid.Parse("B1000001-0000-0000-0000-000000000001");
    private static readonly Guid USR_PRV2 = Guid.Parse("B1000002-0000-0000-0000-000000000002");

    private static readonly Guid PRV1 = Guid.Parse("A8FDE7B9-9A70-45B4-AA99-95194DD71AEE");
    private static readonly Guid PRV2 = Guid.Parse("D674D4CE-4DED-4A7B-8CA4-00A66F4A966F");
    private static readonly Guid PRV3 = Guid.Parse("A2A1D062-EA19-4601-9A26-CDE715E7FADA");

    private static readonly Guid CAM1 = Guid.Parse("BA850881-4ADF-4E66-997B-9FDE3BC7A502");
    private static readonly Guid CAM2 = Guid.Parse("8B212CD8-7BFA-4485-8F82-49D1D3F93B86");
    private static readonly Guid CAM3 = Guid.Parse("4BEBF209-FA07-432D-918B-163C20ECF58D");
    private static readonly Guid CAM4 = Guid.Parse("CC400001-0000-0000-0000-000000000004");

    private static readonly Guid OFT1 = Guid.Parse("05762684-7FC7-4643-B3DF-CC0ED2FAF8B9");
    private static readonly Guid OFT2 = Guid.Parse("EA4E1CDB-5D31-44DE-A50F-60EFD3F06ECF");
    private static readonly Guid OFT3 = Guid.Parse("416F76AA-D38E-46AB-BFBD-08827C771281");
    private static readonly Guid OFT4 = Guid.Parse("516F76AA-D38E-46AB-BFBD-08827C771282");
    private static readonly Guid OFT5 = Guid.Parse("616F76AA-D38E-46AB-BFBD-08827C771283");

    private static readonly Guid SC1 = Guid.Parse("B810392C-E8B6-4FB2-8D61-A9D55ED2F101");
    private static readonly Guid SC2 = Guid.Parse("4C9F7AE2-8E8E-4951-8F2C-E584A3C92E8A");
    private static readonly Guid SC3 = Guid.Parse("C524C3A4-8645-459D-AD1D-03886DE08CC7");

    private static readonly Guid BATCH1 = Guid.Parse("AAA00001-0000-0000-0000-000000000001");
    private static readonly Guid BATCH2 = Guid.Parse("AAA00002-0000-0000-0000-000000000002");
    private static readonly Guid BATCH3 = Guid.Parse("AAA00003-0000-0000-0000-000000000003");

    // Product variants
    private static readonly Guid PV1_S = Guid.Parse("DD100001-0000-0000-0000-000000000001");
    private static readonly Guid PV1_M = Guid.Parse("DD100002-0000-0000-0000-000000000002");
    private static readonly Guid PV1_L = Guid.Parse("DD100003-0000-0000-0000-000000000003");
    private static readonly Guid PV2_S = Guid.Parse("DD200001-0000-0000-0000-000000000001");
    private static readonly Guid PV2_M = Guid.Parse("DD200002-0000-0000-0000-000000000002");
    private static readonly Guid PV3_M = Guid.Parse("DD300001-0000-0000-0000-000000000001");

    // Orders — cover ALL OrderStatus values
    private static readonly Guid ORD1 = Guid.Parse("EE100001-0000-0000-0000-000000000001");
    private static readonly Guid ORD2 = Guid.Parse("EE100002-0000-0000-0000-000000000002");
    private static readonly Guid ORD3 = Guid.Parse("EE100003-0000-0000-0000-000000000003");
    private static readonly Guid ORD4 = Guid.Parse("EE100004-0000-0000-0000-000000000004");
    private static readonly Guid ORD5 = Guid.Parse("EE100005-0000-0000-0000-000000000005");
    private static readonly Guid ORD6 = Guid.Parse("EE100006-0000-0000-0000-000000000006");
    private static readonly Guid ORD7 = Guid.Parse("EE100007-0000-0000-0000-000000000007");
    private static readonly Guid ORD8 = Guid.Parse("EE100008-0000-0000-0000-000000000008");

    // Children
    private static readonly Guid CHILD0 = Guid.Parse("A319CD79-2B45-4507-89FD-318E26A5A26A");
    private static readonly Guid CHILD1 = Guid.Parse("4FCC8D2B-1488-47D8-ABD9-C1B2A36B6BA4");
    private static readonly Guid CHILD2 = Guid.Parse("67FF78A4-78A9-4BAF-9BE2-DF0331E6A7DE");
    private static readonly Guid CHILD3 = Guid.Parse("7C1FC73C-78E3-4F67-957D-9BB9FF0DFF99");

    // Contracts
    private static readonly Guid CTR1 = Guid.Parse("FF100001-0000-0000-0000-000000000001");
    private static readonly Guid CTR2 = Guid.Parse("FF100002-0000-0000-0000-000000000002");
    private static readonly Guid CTR3 = Guid.Parse("FF100003-0000-0000-0000-000000000003");
    private static readonly Guid CTR4 = Guid.Parse("FF100004-0000-0000-0000-000000000004");

    // Wallets (need fixed IDs for PaymentTransaction.WalletID)
    private static readonly Guid WALLET1 = Guid.Parse("FFA00001-0000-0000-0000-000000000001");
    private static readonly Guid WALLET2 = Guid.Parse("FFA00002-0000-0000-0000-000000000002");
    private static readonly Guid WALLET3 = Guid.Parse("FFA00003-0000-0000-0000-000000000003");
    private static readonly Guid WALLET_PRV1 = Guid.Parse("FFA00004-0000-0000-0000-000000000004");
    private static readonly Guid WALLET_PRV2 = Guid.Parse("FFA00005-0000-0000-0000-000000000005");

    // PaymentTransactions (need fixed IDs for Refund.PaymentID)
    private static readonly Guid TXN1 = Guid.Parse("FFB00001-0000-0000-0000-000000000001");
    private static readonly Guid TXN2 = Guid.Parse("FFB00002-0000-0000-0000-000000000002");
    private static readonly Guid TXN3 = Guid.Parse("FFB00003-0000-0000-0000-000000000003");
    private static readonly Guid TXN4 = Guid.Parse("FFB00004-0000-0000-0000-000000000004");
    private static readonly Guid TXN5 = Guid.Parse("FFB00005-0000-0000-0000-000000000005");
    private static readonly Guid TXN6 = Guid.Parse("FFB00006-0000-0000-0000-000000000006");
    private static readonly Guid TXN7 = Guid.Parse("FFB00007-0000-0000-0000-000000000007");
    private static readonly Guid TXN8 = Guid.Parse("FFB00008-0000-0000-0000-000000000008");
    private static readonly Guid TXN9 = Guid.Parse("FFB00009-0000-0000-0000-000000000009");
    private static readonly Guid TXN10 = Guid.Parse("FFB00010-0000-0000-0000-000000000010");
    private static readonly Guid TXN11 = Guid.Parse("FFB00011-0000-0000-0000-000000000011");

    public static async Task SeedAsync(VTOSDbContext db)
    {
        // ── Ensure Admin account always exists (runs on every startup) ────────
        if (!await db.Users.AnyAsync(u => u.Email == "admin@vtos.com"))
        {
            var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
            if (adminRole != null)
            {
                var adminHash = BCrypt.Net.BCrypt.HashPassword("Test@1234", BCrypt.Net.BCrypt.GenerateSalt(12));
                db.Users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    FullName = "Nguyễn Văn Quản Trị",
                    Email = "admin@vtos.com",
                    PasswordHash = adminHash,
                    Phone = "0905000099",
                    RoleID = adminRole.Id,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
        }

        // Guard: only seed remaining data when database is empty
        if (await db.Roles.AnyAsync()) return;

        var hash = BCrypt.Net.BCrypt.HashPassword("Test@1234", BCrypt.Net.BCrypt.GenerateSalt(12));
        var now  = DateTime.UtcNow;

        // ── Roles ──────────────────────────────────────────────────────────────
        db.Roles.AddRange(
            new Role { Id = ROLE_ADMIN,    RoleName = "Admin",    IsSystemRole = true, CreatedAt = now },
            new Role { Id = ROLE_PARENT,   RoleName = "Parent",   IsSystemRole = true, CreatedAt = now },
            new Role { Id = ROLE_SCHOOL,   RoleName = "School",   IsSystemRole = true, CreatedAt = now },
            new Role { Id = ROLE_PROVIDER, RoleName = "Provider", IsSystemRole = true, CreatedAt = now }
        );
        await db.SaveChangesAsync();

        // ── Schools (Real Da Nang schools) ──────────────────────────────────────
        db.Schools.AddRange(
            new School
            {
                Id = SCH1, SchoolName = "Trường THPT Phan Châu Trinh", Level = "THPT",
                LogoURL = "https://i.ibb.co/placeholder/pct-logo.png",
                ContactInfo = "{\"email\":\"contact@thptphanchautrinh.edu.vn\",\"phone\":\"0236 3822 367\",\"address\":\"154 Lê Lợi, Hải Châu, Đà Nẵng\",\"foundedYear\":1952}",
                CreatedAt = now
            },
            new School
            {
                Id = SCH2, SchoolName = "Trường THPT Trần Phú", Level = "THPT",
                LogoURL = "https://i.ibb.co/placeholder/tp-logo.png",
                ContactInfo = "{\"email\":\"contact@thpttranphu.edu.vn\",\"phone\":\"0236 3895 289\",\"address\":\"11 Lê Thánh Tôn, Hải Châu, Đà Nẵng\",\"foundedYear\":1965}",
                CreatedAt = now
            },
            new School
            {
                Id = SCH3, SchoolName = "Trường THCS Nguyễn Huệ", Level = "THCS",
                LogoURL = "https://i.ibb.co/placeholder/nh-logo.png",
                ContactInfo = "{\"email\":\"contact@thcsnguyenhue.edu.vn\",\"phone\":\"0236 3823 456\",\"address\":\"62 Nguyễn Chí Thanh, Hải Châu, Đà Nẵng\",\"foundedYear\":1975}",
                CreatedAt = now
            }
        );
        await db.SaveChangesAsync();

        // ── Providers (Da Nang garment companies) ──────────────────────────────
        db.Providers.AddRange(
            new Provider { Id = PRV1, ProviderName = "Công ty May Mặc Hoàng Gia", ContactPersonName = "Hoàng Minh Tuấn", Phone = "0905123456", Email = "hoanggia@email.com", Address = "Khu CN Hoà Khánh, Liên Chiểu, Đà Nẵng", Status = ProviderStatus.Active, IsDeleted = false },
            new Provider { Id = PRV2, ProviderName = "Đồng Phục Sơn Trà", ContactPersonName = "Võ Thị Lan Anh", Phone = "0935789012", Email = "sontra@email.com", Address = "78 Ngô Quyền, Sơn Trà, Đà Nẵng", Status = ProviderStatus.Active, IsDeleted = false },
            new Provider { Id = PRV3, ProviderName = "Xưởng May Thanh Khê", ContactPersonName = "Bùi Đình Phong", Phone = "0769456789", Email = "thanhkhe@email.com", Address = "215 Điện Biên Phủ, Thanh Khê, Đà Nẵng", Status = ProviderStatus.Active, IsDeleted = false }
        );
        await db.SaveChangesAsync();

        // ── Wallets (both School + Provider) ──────────────────────────────────
        db.Wallets.AddRange(
            // School Wallets — Balance = sum of OrderPayment - ProviderPayment - Refund
            // WALLET1: +350K(TXN1) +555K(TXN2) +370K(TXN3) = 1,275,000
            new Wallet { Id = WALLET1, OwnerID = SCH1, OwnerType = WalletOwnerType.School, Balance = 1_275_000, BankCode = "VCB", BankName = "Vietcombank", BankAccountNumber = "0491000234567", BankAccountName = "TRUONG THPT PHAN CHAU TRINH", IsActive = true, CreatedAt = now, UpdatedAt = now },
            // WALLET2: +475K(TXN4) +195K(TXN5) -195K(TXN6 ProviderPayment) = 475,000
            new Wallet { Id = WALLET2, OwnerID = SCH2, OwnerType = WalletOwnerType.School, Balance = 475_000, BankCode = "TCB", BankName = "Techcombank", BankAccountNumber = "19035678901234", BankAccountName = "TRUONG THPT TRAN PHU", IsActive = true, CreatedAt = now, UpdatedAt = now },
            // WALLET3: +120K(TXN8 OrderPayment) -120K(TXN7 Refund) = 0
            new Wallet { Id = WALLET3, OwnerID = SCH3, OwnerType = WalletOwnerType.School, Balance = 0, BankCode = "BIDV", BankName = "BIDV", BankAccountNumber = "31410001234567", BankAccountName = "TRUONG THCS NGUYEN HUE", IsActive = true, CreatedAt = now, UpdatedAt = now },
            // Provider Wallets — receive money from School ProviderPayments
            // WALLET_PRV1: +195K(TXN6 ProviderPayment received) = 195,000
            new Wallet { Id = WALLET_PRV1, OwnerID = PRV1, OwnerType = WalletOwnerType.Provider, Balance = 195_000, BankCode = "VCB", BankName = "Vietcombank", BankAccountNumber = "0491000567890", BankAccountName = "CONG TY MAY MAC HOANG GIA", IsActive = true, CreatedAt = now, UpdatedAt = now },
            // WALLET_PRV2: +130K(TXN10) +185K(TXN11) = 315,000
            new Wallet { Id = WALLET_PRV2, OwnerID = PRV2, OwnerType = WalletOwnerType.Provider, Balance = 315_000, BankCode = "TCB", BankName = "Techcombank", BankAccountNumber = "19035678905678", BankAccountName = "DONG PHUC SON TRA", IsActive = true, CreatedAt = now, UpdatedAt = now }
        );
        await db.SaveChangesAsync();

        // ── Users ─────────────────────────────────────────────────────────────
        db.Users.AddRange(
            // School managers
            new User { Id = USR_SCH1, FullName = "Nguyễn Thị Thanh Hà", Email = "school1@vtos.com", PasswordHash = hash, Phone = "0905112233", Avatar = "avatar.jpg", RoleID = ROLE_SCHOOL, IsActive = true, IsDeleted = false, CreatedAt = now },
            new User { Id = USR_SCH2, FullName = "Trần Văn Minh", Email = "school2@vtos.com", PasswordHash = hash, Phone = "0935445566", Avatar = "avatar.jpg", RoleID = ROLE_SCHOOL, IsActive = true, IsDeleted = false, CreatedAt = now },
            new User { Id = USR_SCH3, FullName = "Lê Thị Bích Ngọc", Email = "school3@vtos.com", PasswordHash = hash, Phone = "0769778899", Avatar = "avatar.jpg", RoleID = ROLE_SCHOOL, IsActive = true, IsDeleted = false, CreatedAt = now },
            // Parents
            new User { Id = USR_P0, FullName = "Trần Thị Hương", Email = "parent0@vtos.com", PasswordHash = hash, Phone = "0905101010", Avatar = "avatar.jpg", RoleID = ROLE_PARENT, IsActive = true, IsDeleted = false, CreatedAt = now },
            new User { Id = USR_P1, FullName = "Lê Văn Đức", Email = "parent1@vtos.com", PasswordHash = hash, Phone = "0935202020", Avatar = "avatar.jpg", RoleID = ROLE_PARENT, IsActive = true, IsDeleted = false, CreatedAt = now },
            new User { Id = USR_P2, FullName = "Phạm Thị Mai", Email = "parent2@vtos.com", PasswordHash = hash, Phone = "0769303030", Avatar = "avatar.jpg", RoleID = ROLE_PARENT, IsActive = true, IsDeleted = false, CreatedAt = now },
            new User { Id = USR_P3, FullName = "Ngô Quang Hải", Email = "parent3@vtos.com", PasswordHash = hash, Phone = "0905404040", Avatar = "avatar.jpg", RoleID = ROLE_PARENT, IsActive = true, IsDeleted = false, CreatedAt = now },
            // Providers
            new User { Id = USR_PRV1, FullName = "Hoàng Minh Tuấn", Email = "provider1@vtos.com", PasswordHash = hash, Phone = "0905123456", Avatar = "avatar.jpg", RoleID = ROLE_PROVIDER, IsActive = true, IsDeleted = false, CreatedAt = now },
            new User { Id = USR_PRV2, FullName = "Võ Thị Lan Anh", Email = "provider2@vtos.com", PasswordHash = hash, Phone = "0935789012", Avatar = "avatar.jpg", RoleID = ROLE_PROVIDER, IsActive = true, IsDeleted = false, CreatedAt = now }
        );
        await db.SaveChangesAsync();

        // ── ParentProfiles (parent-specific: DOB, Gender) ────────────────────
        db.ParentProfiles.AddRange(
            new ParentProfile { Id = Guid.NewGuid(), UserID = USR_P0, DOB = new DateTime(1985, 3, 15), Gender = Gender.Female },
            new ParentProfile { Id = Guid.NewGuid(), UserID = USR_P1, DOB = new DateTime(1982, 7, 22), Gender = Gender.Male },
            new ParentProfile { Id = Guid.NewGuid(), UserID = USR_P2, DOB = new DateTime(1990, 11, 8), Gender = Gender.Female },
            new ParentProfile { Id = Guid.NewGuid(), UserID = USR_P3, DOB = new DateTime(1988, 5, 30), Gender = Gender.Male }
        );
        await db.SaveChangesAsync();

        // ── SchoolManagers (link user → school) ──────────────────────────────
        db.SchoolManagers.AddRange(
            new SchoolManager { Id = Guid.NewGuid(), UserID = USR_SCH1, SchoolID = SCH1 },
            new SchoolManager { Id = Guid.NewGuid(), UserID = USR_SCH2, SchoolID = SCH2 },
            new SchoolManager { Id = Guid.NewGuid(), UserID = USR_SCH3, SchoolID = SCH3 }
        );
        await db.SaveChangesAsync();

        // ── ProviderManagers (link user → provider) ──────────────────────────
        db.ProviderManagers.AddRange(
            new ProviderManager { Id = Guid.NewGuid(), UserID = USR_PRV1, ProviderID = PRV1 },
            new ProviderManager { Id = Guid.NewGuid(), UserID = USR_PRV2, ProviderID = PRV2 }
        );
        await db.SaveChangesAsync();

        // ── SizeCharts ────────────────────────────────────────────────────────
        db.SizeCharts.AddRange(
            new SizeChart { Id = SC1, ChartName = "Bảng size THPT Phan Châu Trinh", Unit = "cm", CreatedAt = now },
            new SizeChart { Id = SC2, ChartName = "Bảng size THPT Trần Phú", Unit = "cm", CreatedAt = now },
            new SizeChart { Id = SC3, ChartName = "Bảng size THCS Nguyễn Huệ", Unit = "cm", CreatedAt = now }
        );
        await db.SaveChangesAsync();

        // ── Outfits ───────────────────────────────────────────────────────────
        db.Outfits.AddRange(
            new Outfit { Id = OFT1, SchoolID = SCH1, OutfitName = "Áo sơ mi trắng THPT Phan Châu Trinh", Description = "Áo sơ mi trắng dài tay, logo trường thêu ngực trái", Price = 185_000, OutfitType = OutfitType.Uniform, SizeChartID = SC1, IsAvailable = true, IsCustomizable = false, IsDeleted = false, CreatedAt = now },
            new Outfit { Id = OFT2, SchoolID = SCH2, OutfitName = "Quần tây xanh THPT Trần Phú", Description = "Quần tây xanh đen, vải tốt không nhăn", Price = 195_000, OutfitType = OutfitType.Uniform, SizeChartID = SC2, IsAvailable = true, IsCustomizable = false, IsDeleted = false, CreatedAt = now },
            new Outfit { Id = OFT3, SchoolID = SCH3, OutfitName = "Áo thể dục THCS Nguyễn Huệ", Description = "Áo thể dục cổ tròn, vải thun cotton thoáng mát", Price = 120_000, OutfitType = OutfitType.Sportswear, SizeChartID = SC3, IsAvailable = true, IsCustomizable = false, IsDeleted = false, CreatedAt = now },
            new Outfit { Id = OFT4, SchoolID = SCH1, OutfitName = "Áo dài trắng nữ THPT Phan Châu Trinh", Description = "Áo dài trắng truyền thống dành cho nữ sinh", Price = 350_000, OutfitType = OutfitType.Uniform, SizeChartID = SC1, IsAvailable = true, IsCustomizable = false, IsDeleted = false, CreatedAt = now },
            new Outfit { Id = OFT5, SchoolID = SCH2, OutfitName = "Áo khoác đồng phục THPT Trần Phú", Description = "Áo khoác gió đồng phục, logo trường thêu", Price = 280_000, OutfitType = OutfitType.Uniform, SizeChartID = SC2, IsAvailable = true, IsCustomizable = false, IsDeleted = false, CreatedAt = now }
        );
        await db.SaveChangesAsync();

        // ══════════════════════════════════════════════════════════════════════
        // ── CONTRACTS (must exist BEFORE campaigns — define production prices)
        // ══════════════════════════════════════════════════════════════════════
        db.Set<Contract>().AddRange(
            // CTR1: PCT ↔ Hoàng Gia — Approved (active, used for CAM1 & CAM4)
            new Contract { Id = CTR1, SchoolID = SCH1, ProviderID = PRV1, ContractName = "HĐ May đồng phục PCT - Hoàng Gia 2025-2026", Status = "Approved", CreatedAt = now.AddDays(-60), ApprovedAt = now.AddDays(-55) },
            // CTR2: TP ↔ Sơn Trà — Approved (active, used for CAM2)
            new Contract { Id = CTR2, SchoolID = SCH2, ProviderID = PRV2, ContractName = "HĐ May đồng phục TP - Sơn Trà 2025-2026", Status = "Approved", CreatedAt = now.AddDays(-50), ApprovedAt = now.AddDays(-45) },
            // CTR3: NH ↔ Thanh Khê — Approved (active, used for CAM3)
            new Contract { Id = CTR3, SchoolID = SCH3, ProviderID = PRV3, ContractName = "HĐ May áo thể dục NH - Thanh Khê 2025-2026", Status = "Approved", CreatedAt = now.AddDays(-45), ApprovedAt = now.AddDays(-40) },
            // CTR4: PCT ↔ Sơn Trà — Pending (for testing pending contract flow)
            new Contract { Id = CTR4, SchoolID = SCH1, ProviderID = PRV2, ContractName = "HĐ May áo khoác PCT - Sơn Trà (chờ duyệt)", Status = "Pending", CreatedAt = now.AddDays(-3) }
        );
        await db.SaveChangesAsync();

        // ── ContractItems (production cost per outfit — different from retail price)
        db.Set<ContractItem>().AddRange(
            // CTR1: PCT ↔ Hoàng Gia — áo sơ mi + áo dài
            new ContractItem { Id = Guid.NewGuid(), ContractID = CTR1, OutfitID = OFT1, PricePerUnit = 125_000, MinQuantity = 50, MaxQuantity = 500 },
            new ContractItem { Id = Guid.NewGuid(), ContractID = CTR1, OutfitID = OFT4, PricePerUnit = 230_000, MinQuantity = 30, MaxQuantity = 200 },
            // CTR2: TP ↔ Sơn Trà — quần tây + áo khoác
            new ContractItem { Id = Guid.NewGuid(), ContractID = CTR2, OutfitID = OFT2, PricePerUnit = 130_000, MinQuantity = 40, MaxQuantity = 400 },
            new ContractItem { Id = Guid.NewGuid(), ContractID = CTR2, OutfitID = OFT5, PricePerUnit = 185_000, MinQuantity = 30, MaxQuantity = 300 },
            // CTR3: NH ↔ Thanh Khê — áo thể dục
            new ContractItem { Id = Guid.NewGuid(), ContractID = CTR3, OutfitID = OFT3, PricePerUnit = 75_000, MinQuantity = 50, MaxQuantity = 350 },
            // CTR4: Pending — áo khoác from different provider
            new ContractItem { Id = Guid.NewGuid(), ContractID = CTR4, OutfitID = OFT5, PricePerUnit = 175_000, MinQuantity = 20, MaxQuantity = 200 }
        );
        await db.SaveChangesAsync();

        // ── Campaigns ─────────────────────────────────────────────────────────
        db.Campaigns.AddRange(
            new Campaign { Id = CAM1, SchoolID = SCH1, CampaignName = "Đồng phục Năm học 2025-2026 - PCT", Description = "Chiến dịch đặt đồng phục chính thức THPT Phan Châu Trinh", StartDate = new DateTime(2026,1,15), EndDate = new DateTime(2026,4,30), Status = CampaignStatus.Active, CreatedAt = now.AddDays(-30) },
            new Campaign { Id = CAM2, SchoolID = SCH2, CampaignName = "Đồng phục Năm học 2025-2026 - TP", Description = "Đặt đồng phục THPT Trần Phú", StartDate = new DateTime(2026,2,1), EndDate = new DateTime(2026,5,15), Status = CampaignStatus.Active, CreatedAt = now.AddDays(-25) },
            new Campaign { Id = CAM3, SchoolID = SCH3, CampaignName = "Đồng phục Năm học 2025-2026 - NH", Description = "Chiến dịch đồng phục THCS Nguyễn Huệ", StartDate = new DateTime(2026,1,20), EndDate = new DateTime(2026,3,31), Status = CampaignStatus.Active, CreatedAt = now.AddDays(-28) },
            new Campaign { Id = CAM4, SchoolID = SCH1, CampaignName = "Đồng phục Hè 2026 - PCT (Đã khóa)", Description = "Chiến dịch đồng phục mùa hè - đã khóa để tạo đơn sản xuất", StartDate = new DateTime(2026,3,1), EndDate = new DateTime(2026,6,30), Status = CampaignStatus.Locked, CreatedAt = now.AddDays(-20) }
        );
        await db.SaveChangesAsync();

        // ── CampaignOutfits ───────────────────────────────────────────────────
        db.CampaignOutfits.AddRange(
            new CampaignOutfit { Id = Guid.NewGuid(), CampaignID = CAM1, OutfitID = OFT1, ProviderID = PRV1, CampaignPrice = 185_000, MaxQuantity = 500 },
            new CampaignOutfit { Id = Guid.NewGuid(), CampaignID = CAM1, OutfitID = OFT4, ProviderID = PRV1, CampaignPrice = 350_000, MaxQuantity = 200 },
            new CampaignOutfit { Id = Guid.NewGuid(), CampaignID = CAM2, OutfitID = OFT2, ProviderID = PRV2, CampaignPrice = 195_000, MaxQuantity = 400 },
            new CampaignOutfit { Id = Guid.NewGuid(), CampaignID = CAM2, OutfitID = OFT5, ProviderID = PRV2, CampaignPrice = 280_000, MaxQuantity = 300 },
            new CampaignOutfit { Id = Guid.NewGuid(), CampaignID = CAM3, OutfitID = OFT3, ProviderID = PRV3, CampaignPrice = 120_000, MaxQuantity = 350 },
            new CampaignOutfit { Id = Guid.NewGuid(), CampaignID = CAM4, OutfitID = OFT1, ProviderID = PRV1, CampaignPrice = 185_000, MaxQuantity = 500 }
        );
        await db.SaveChangesAsync();

        // ── Children ──────────────────────────────────────────────────────────
        db.ChildProfiles.AddRange(
            new ChildProfile { Id = CHILD0, ParentUserID = USR_P0, FullName = "Trần Minh Khôi", DOB = new DateTime(2010,3,15), Age = 16, Grade = "Lớp 10A1", Gender = Gender.Male, SchoolID = SCH1, IsDeleted = false, Avatar = "avatar.jpg", HeightCm = 168, WeightKg = 55 },
            new ChildProfile { Id = CHILD1, ParentUserID = USR_P1, FullName = "Lê Ngọc Bảo Trân", DOB = new DateTime(2009,8,22), Age = 17, Grade = "Lớp 11B3", Gender = Gender.Female, SchoolID = SCH2, IsDeleted = false, Avatar = "avatar.jpg", HeightCm = 158, WeightKg = 48 },
            new ChildProfile { Id = CHILD2, ParentUserID = USR_P2, FullName = "Phạm Gia Huy", DOB = new DateTime(2012,11,5), Age = 14, Grade = "Lớp 8A2", Gender = Gender.Male, SchoolID = SCH3, IsDeleted = false, Avatar = "avatar.jpg", HeightCm = 155, WeightKg = 45 },
            new ChildProfile { Id = CHILD3, ParentUserID = USR_P3, FullName = "Ngô Thùy Linh", DOB = new DateTime(2010,6,18), Age = 16, Grade = "Lớp 10A5", Gender = Gender.Female, SchoolID = SCH1, IsDeleted = false, Avatar = "avatar.jpg", HeightCm = 160, WeightKg = 50 }
        );
        await db.SaveChangesAsync();

        // ── ProductVariants ───────────────────────────────────────────────────
        db.ProductVariants.AddRange(
            // Outfit 1 — Áo sơ mi PCT
            new ProductVariant { Id = PV1_S, OutfitID = OFT1, Size = "S", Price = 185_000, StockQuantity = 100, SKUCode = "PCT-AOSOMI-S", IsDeleted = false },
            new ProductVariant { Id = PV1_M, OutfitID = OFT1, Size = "M", Price = 185_000, StockQuantity = 100, SKUCode = "PCT-AOSOMI-M", IsDeleted = false },
            new ProductVariant { Id = PV1_L, OutfitID = OFT1, Size = "L", Price = 185_000, StockQuantity = 100, SKUCode = "PCT-AOSOMI-L", IsDeleted = false },
            // Outfit 2 — Quần tây TP
            new ProductVariant { Id = PV2_S, OutfitID = OFT2, Size = "S", Price = 195_000, StockQuantity = 80, SKUCode = "TP-QUANTAY-S", IsDeleted = false },
            new ProductVariant { Id = PV2_M, OutfitID = OFT2, Size = "M", Price = 195_000, StockQuantity = 80, SKUCode = "TP-QUANTAY-M", IsDeleted = false },
            // Outfit 3 — Áo thể dục NH
            new ProductVariant { Id = PV3_M, OutfitID = OFT3, Size = "M", Price = 120_000, StockQuantity = 80, SKUCode = "NH-AOTD-M", IsDeleted = false }
        );
        await db.SaveChangesAsync();

        // ══════════════════════════════════════════════════════════════════════
        // ── ORDERS — All 8 OrderStatus values covered for comprehensive testing
        // ══════════════════════════════════════════════════════════════════════
        db.Orders.AddRange(
            // ORD1: Pending (chờ thanh toán)
            new Order { Id = ORD1, ChildProfileID = CHILD0, CampaignID = CAM1, OrderDate = now.AddDays(-1), OrderStatus = OrderStatus.Pending, TotalAmount = 185_000, ShippingAddress = "42 Nguyễn Văn Linh, Hải Châu, Đà Nẵng", CreatedAt = now.AddDays(-1) },
            // ORD2: Paid (đã thanh toán, chờ xác nhận)
            new Order { Id = ORD2, ChildProfileID = CHILD0, CampaignID = CAM1, OrderDate = now.AddDays(-5), OrderStatus = OrderStatus.Paid, TotalAmount = 350_000, ShippingAddress = "42 Nguyễn Văn Linh, Hải Châu, Đà Nẵng", CreatedAt = now.AddDays(-5) },
            // ORD3: Confirmed (đã xác nhận, chờ xử lý) — on locked campaign
            new Order { Id = ORD3, ChildProfileID = CHILD0, CampaignID = CAM4, OrderDate = now.AddDays(-10), OrderStatus = OrderStatus.Confirmed, TotalAmount = 555_000, ShippingAddress = "42 Nguyễn Văn Linh, Hải Châu, Đà Nẵng", CreatedAt = now.AddDays(-10) },
            // ORD4: Processed (đã xử lý, chờ giao hàng)
            new Order { Id = ORD4, ChildProfileID = CHILD3, CampaignID = CAM4, OrderDate = now.AddDays(-12), OrderStatus = OrderStatus.Processed, TotalAmount = 370_000, ShippingAddress = "15 Phan Đăng Lưu, Hải Châu, Đà Nẵng", CreatedAt = now.AddDays(-12) },
            // ORD5: Shipped (đang giao hàng)
            new Order { Id = ORD5, ChildProfileID = CHILD1, CampaignID = CAM2, OrderDate = now.AddDays(-20), OrderStatus = OrderStatus.Shipped, TotalAmount = 475_000, ShippingAddress = "88 Trần Cao Vân, Thanh Khê, Đà Nẵng", IsProviderPaid = true, CreatedAt = now.AddDays(-20) },
            // ORD6: Delivered (đã phân phối — full flow complete)
            new Order { Id = ORD6, ChildProfileID = CHILD1, CampaignID = CAM2, OrderDate = now.AddDays(-30), OrderStatus = OrderStatus.Delivered, TotalAmount = 195_000, ShippingAddress = "88 Trần Cao Vân, Thanh Khê, Đà Nẵng", IsProviderPaid = true, CreatedAt = now.AddDays(-30) },
            // ORD7: Cancelled (đã hủy — with cancel reason)
            new Order { Id = ORD7, ChildProfileID = CHILD2, CampaignID = CAM3, OrderDate = now.AddDays(-3), OrderStatus = OrderStatus.Cancelled, TotalAmount = 120_000, ShippingAddress = "23 Lê Duẩn, Hải Châu, Đà Nẵng", CancelReason = "Phụ huynh đặt nhầm size, muốn đặt lại", CreatedAt = now.AddDays(-3) },
            // ORD8: Refunded (đã hoàn tiền — full refund flow)
            new Order { Id = ORD8, ChildProfileID = CHILD2, CampaignID = CAM3, OrderDate = now.AddDays(-25), OrderStatus = OrderStatus.Refunded, TotalAmount = 120_000, ShippingAddress = "23 Lê Duẩn, Hải Châu, Đà Nẵng", CancelReason = "Sản phẩm bị lỗi, hoàn tiền theo yêu cầu", CreatedAt = now.AddDays(-25) }
        );
        await db.SaveChangesAsync();

        // ── OrderItems ────────────────────────────────────────────────────────
        db.OrderItems.AddRange(
            // ORD1: 1x áo sơ mi M = 185K (Pending)
            new OrderItem { Id = Guid.NewGuid(), OrderID = ORD1, ProductVariantID = PV1_M, Quantity = 1, UnitPrice = 185_000, SizeOrdered = "M", IsCustomOrder = false, CreatedAt = now.AddDays(-1) },
            // ORD2: 1x áo dài = 350K (Paid) — using PV1_L as placeholder variant
            new OrderItem { Id = Guid.NewGuid(), OrderID = ORD2, ProductVariantID = PV1_L, Quantity = 1, UnitPrice = 350_000, SizeOrdered = "M", IsCustomOrder = false, CreatedAt = now.AddDays(-5) },
            // ORD3: 2x áo sơ mi S + 1x M = 555K (Confirmed)
            new OrderItem { Id = Guid.NewGuid(), OrderID = ORD3, ProductVariantID = PV1_S, Quantity = 2, UnitPrice = 185_000, SizeOrdered = "S", IsCustomOrder = false, CreatedAt = now.AddDays(-10) },
            new OrderItem { Id = Guid.NewGuid(), OrderID = ORD3, ProductVariantID = PV1_M, Quantity = 1, UnitPrice = 185_000, SizeOrdered = "M", IsCustomOrder = false, CreatedAt = now.AddDays(-10) },
            // ORD4: 1x áo sơ mi M + 1x L = 370K (Processed)
            new OrderItem { Id = Guid.NewGuid(), OrderID = ORD4, ProductVariantID = PV1_M, Quantity = 1, UnitPrice = 185_000, SizeOrdered = "M", IsCustomOrder = false, CreatedAt = now.AddDays(-12) },
            new OrderItem { Id = Guid.NewGuid(), OrderID = ORD4, ProductVariantID = PV1_L, Quantity = 1, UnitPrice = 185_000, SizeOrdered = "L", IsCustomOrder = false, CreatedAt = now.AddDays(-12) },
            // ORD5: 1x quần tây S + 1x áo khoác M = 475K (Shipped)
            new OrderItem { Id = Guid.NewGuid(), OrderID = ORD5, ProductVariantID = PV2_S, Quantity = 1, UnitPrice = 195_000, SizeOrdered = "S", IsCustomOrder = false, CreatedAt = now.AddDays(-20) },
            new OrderItem { Id = Guid.NewGuid(), OrderID = ORD5, ProductVariantID = PV2_M, Quantity = 1, UnitPrice = 280_000, SizeOrdered = "M", IsCustomOrder = false, CreatedAt = now.AddDays(-20) },
            // ORD6: 1x quần tây M = 195K (Delivered)
            new OrderItem { Id = Guid.NewGuid(), OrderID = ORD6, ProductVariantID = PV2_M, Quantity = 1, UnitPrice = 195_000, SizeOrdered = "M", IsCustomOrder = false, CreatedAt = now.AddDays(-30) },
            // ORD7: 1x áo thể dục M = 120K (Cancelled)
            new OrderItem { Id = Guid.NewGuid(), OrderID = ORD7, ProductVariantID = PV3_M, Quantity = 1, UnitPrice = 120_000, SizeOrdered = "M", IsCustomOrder = false, CreatedAt = now.AddDays(-3) },
            // ORD8: 1x áo thể dục M = 120K (Refunded)
            new OrderItem { Id = Guid.NewGuid(), OrderID = ORD8, ProductVariantID = PV3_M, Quantity = 1, UnitPrice = 120_000, SizeOrdered = "M", IsCustomOrder = false, CreatedAt = now.AddDays(-25) }
        );
        await db.SaveChangesAsync();

        // ══════════════════════════════════════════════════════════════════════
        // ── PAYMENT TRANSACTIONS — All 3 TransactionTypes + all PaymentStatus
        // ══════════════════════════════════════════════════════════════════════
        db.PaymentTransactions.AddRange(
            // TXN1: OrderPayment — ORD2 paid (→ funds SCH1 wallet) ✅ Completed
            new PaymentTransaction { Id = TXN1, OrderID = ORD2, WalletID = WALLET1, Amount = 350_000, TransactionStatus = PaymentStatus.Completed, GatewayType = PaymentGatewayType.PayOS, TransactionType = TransactionType.OrderPayment, Description = "Trần Thị Hương thanh toán đơn hàng áo dài", TransactionTimestamp = now.AddDays(-4), CreatedAt = now.AddDays(-4) },
            // TXN2: OrderPayment — ORD3 confirmed (→ funds SCH1 wallet) ✅ Completed
            new PaymentTransaction { Id = TXN2, OrderID = ORD3, WalletID = WALLET1, Amount = 555_000, TransactionStatus = PaymentStatus.Completed, GatewayType = PaymentGatewayType.PayOS, TransactionType = TransactionType.OrderPayment, Description = "Trần Thị Hương thanh toán 3 áo sơ mi PCT", TransactionTimestamp = now.AddDays(-9), CreatedAt = now.AddDays(-9) },
            // TXN3: OrderPayment — ORD4 processed (→ funds SCH1 wallet) ✅ Completed
            new PaymentTransaction { Id = TXN3, OrderID = ORD4, WalletID = WALLET1, Amount = 370_000, TransactionStatus = PaymentStatus.Completed, GatewayType = PaymentGatewayType.PayOS, TransactionType = TransactionType.OrderPayment, Description = "Ngô Quang Hải thanh toán đơn đồng phục hè", TransactionTimestamp = now.AddDays(-11), CreatedAt = now.AddDays(-11) },
            // TXN4: OrderPayment — ORD5 shipped (→ funds SCH2 wallet) ✅ Completed
            new PaymentTransaction { Id = TXN4, OrderID = ORD5, WalletID = WALLET2, Amount = 475_000, TransactionStatus = PaymentStatus.Completed, GatewayType = PaymentGatewayType.PayOS, TransactionType = TransactionType.OrderPayment, Description = "Lê Văn Đức thanh toán đồng phục Trần Phú", TransactionTimestamp = now.AddDays(-19), CreatedAt = now.AddDays(-19) },
            // TXN5: OrderPayment — ORD6 delivered (→ funds SCH2 wallet) ✅ Completed
            new PaymentTransaction { Id = TXN5, OrderID = ORD6, WalletID = WALLET2, Amount = 195_000, TransactionStatus = PaymentStatus.Completed, GatewayType = PaymentGatewayType.PayOS, TransactionType = TransactionType.OrderPayment, Description = "Lê Văn Đức thanh toán quần tây Trần Phú", TransactionTimestamp = now.AddDays(-29), CreatedAt = now.AddDays(-29) },
            // TXN6: ProviderPayment — School pays Provider for ORD5 production ✅ Completed
            new PaymentTransaction { Id = TXN6, OrderID = ORD5, WalletID = WALLET2, Amount = 195_000, TransactionStatus = PaymentStatus.Completed, GatewayType = PaymentGatewayType.Other, TransactionType = TransactionType.ProviderPayment, Description = "Thanh toán NCC Sơn Trà — lô sản xuất quần tây", TransactionTimestamp = now.AddDays(-15), CreatedAt = now.AddDays(-15) },
            // TXN7: Refund — ORD8 refunded back to parent ✅ Completed
            new PaymentTransaction { Id = TXN7, OrderID = ORD8, WalletID = WALLET3, Amount = 120_000, TransactionStatus = PaymentStatus.Completed, GatewayType = PaymentGatewayType.PayOS, TransactionType = TransactionType.Refund, Description = "Hoàn tiền cho Phạm Thị Mai — sản phẩm lỗi", TransactionTimestamp = now.AddDays(-22), CreatedAt = now.AddDays(-22) },
            // TXN8: OrderPayment — ORD8 was paid BEFORE being refunded (→ funds SCH3 wallet, then refunded)
            new PaymentTransaction { Id = TXN8, OrderID = ORD8, WalletID = WALLET3, Amount = 120_000, TransactionStatus = PaymentStatus.Completed, GatewayType = PaymentGatewayType.PayOS, TransactionType = TransactionType.OrderPayment, Description = "Phạm Thị Mai thanh toán áo thể dục (sau đó hoàn tiền)", TransactionTimestamp = now.AddDays(-24), CreatedAt = now.AddDays(-24) },
            // ── Provider Wallet Transactions (TXN9-TXN11) ──────────────────────
            // TXN9: ProviderPayment → WALLET_PRV1 (Hoàng Gia nhận tiền từ PCT cho lô áo sơ mi)
            new PaymentTransaction { Id = TXN9, OrderID = ORD5, WalletID = WALLET_PRV1, Amount = 195_000, TransactionStatus = PaymentStatus.Completed, GatewayType = PaymentGatewayType.Other, TransactionType = TransactionType.ProviderPayment, Description = "Nhận thanh toán từ Trường THPT Trần Phú — lô quần tây", TransactionTimestamp = now.AddDays(-14), CreatedAt = now.AddDays(-14) },
            // TXN10: ProviderPayment → WALLET_PRV2 (Sơn Trà nhận tiền từ TP cho lô quần tây)
            new PaymentTransaction { Id = TXN10, OrderID = ORD6, WalletID = WALLET_PRV2, Amount = 130_000, TransactionStatus = PaymentStatus.Completed, GatewayType = PaymentGatewayType.Other, TransactionType = TransactionType.ProviderPayment, Description = "Nhận thanh toán từ Trường THPT Trần Phú — quần tây đã giao", TransactionTimestamp = now.AddDays(-10), CreatedAt = now.AddDays(-10) },
            // TXN11: ProviderPayment → WALLET_PRV2 (thêm 1 khoản nữa cho Sơn Trà)
            new PaymentTransaction { Id = TXN11, OrderID = ORD5, WalletID = WALLET_PRV2, Amount = 185_000, TransactionStatus = PaymentStatus.Completed, GatewayType = PaymentGatewayType.Other, TransactionType = TransactionType.ProviderPayment, Description = "Nhận thanh toán từ Trường THPT Trần Phú — áo khoác lô 2", TransactionTimestamp = now.AddDays(-7), CreatedAt = now.AddDays(-7) }
        );
        await db.SaveChangesAsync();

        // ── Refunds (linked to TXN7) ─────────────────────────────────────────
        db.Set<Refund>().AddRange(
            new Refund { Id = Guid.NewGuid(), PaymentID = TXN7, RefundAmount = 120_000, RefundStatus = RefundStatus.Completed, DisputeReason = "Áo thể dục bị lỗi in logo, hoàn tiền 100%", CreatedAt = now.AddDays(-22) }
        );
        await db.SaveChangesAsync();

        // ── ProductionBatches ─────────────────────────────────────────────────
        db.ProductionBatches.AddRange(
            new ProductionBatch { Id = BATCH1, CampaignID = CAM1, ProviderID = PRV1, BatchName = "Lô SX PCT - Áo sơ mi HK2", TotalQuantity = 150, CreatedDate = now.AddDays(-15), Status = ProductionBatchStatus.Pending, DeliveryDeadline = new DateTime(2026,4,15), IsDeleted = false },
            new ProductionBatch { Id = BATCH2, CampaignID = CAM2, ProviderID = PRV2, BatchName = "Lô SX TP - Quần tây HK2", TotalQuantity = 100, CreatedDate = now.AddDays(-12), Status = ProductionBatchStatus.Approved, DeliveryDeadline = new DateTime(2026,5,1), IsDeleted = false },
            new ProductionBatch { Id = BATCH3, CampaignID = CAM3, ProviderID = PRV3, BatchName = "Lô SX NH - Áo thể dục", TotalQuantity = 80, CreatedDate = now.AddDays(-10), Status = ProductionBatchStatus.InProduction, DeliveryDeadline = new DateTime(2026,3,20), IsDeleted = false, ProcessedAt = now.AddDays(-7) }
        );
        await db.SaveChangesAsync();

        // ── ProductionBatchItems ──────────────────────────────────────────────
        db.ProductionBatchItems.AddRange(
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH1, OutfitID = OFT1, Size = "S", Quantity = 50, UnitPrice = 125_000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH1, OutfitID = OFT1, Size = "M", Quantity = 65, UnitPrice = 125_000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH1, OutfitID = OFT1, Size = "L", Quantity = 35, UnitPrice = 125_000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH2, OutfitID = OFT2, Size = "M", Quantity = 55, UnitPrice = 130_000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH2, OutfitID = OFT2, Size = "L", Quantity = 45, UnitPrice = 130_000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH3, OutfitID = OFT3, Size = "M", Quantity = 80, UnitPrice = 75_000 }
        );
        await db.SaveChangesAsync();

        // ── Complaints ────────────────────────────────────────────────────────
        db.Complaints.AddRange(
            new Complaint { Id = Guid.NewGuid(), CampaignID = CAM1, BatchID = BATCH1, SchoolID = SCH1, ProviderID = PRV1, Title = "Áo sơ mi bị phai màu sau khi giặt", Description = "Một số áo lô đầu bị phai vàng sau 2 lần giặt máy", Status = ComplaintStatus.Open, CreatedAt = now.AddDays(-3) },
            new Complaint { Id = Guid.NewGuid(), CampaignID = CAM1, BatchID = BATCH1, SchoolID = SCH1, ProviderID = PRV1, Title = "Thiếu hàng size M 3 sản phẩm", Description = "Đơn giao 65 cái size M nhưng chỉ nhận 62, thiếu 3", Status = ComplaintStatus.InProgress, CreatedAt = now.AddDays(-5) },
            new Complaint { Id = Guid.NewGuid(), CampaignID = CAM2, BatchID = BATCH2, SchoolID = SCH2, ProviderID = PRV2, Title = "Giao hàng trễ hạn 10 ngày", Description = "Hạn giao 01/03 nhưng 11/03 vẫn chưa nhận được hàng", Status = ComplaintStatus.Resolved, CreatedAt = now.AddDays(-12) }
        );
        await db.SaveChangesAsync();
    }
}
