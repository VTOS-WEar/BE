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
    private static readonly Guid ROLE_ADMIN  = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ROLE_PARENT = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ROLE_SCHOOL = Guid.Parse("33333333-3333-3333-3333-333333333333");
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

    // Locked campaign for testing GenerateProductionOrder
    private static readonly Guid CAM4 = Guid.Parse("CC400001-0000-0000-0000-000000000004");
    // Product variants for Outfit 1 (Áo sơ mi PCT)
    private static readonly Guid PV1_S = Guid.Parse("DD100001-0000-0000-0000-000000000001");
    private static readonly Guid PV1_M = Guid.Parse("DD100002-0000-0000-0000-000000000002");
    private static readonly Guid PV1_L = Guid.Parse("DD100003-0000-0000-0000-000000000003");
    // Product variants for Outfit 2 (Quần tây TP)
    private static readonly Guid PV2_S = Guid.Parse("DD200001-0000-0000-0000-000000000001");
    private static readonly Guid PV2_M = Guid.Parse("DD200002-0000-0000-0000-000000000002");
    // Orders
    private static readonly Guid ORD1 = Guid.Parse("EE100001-0000-0000-0000-000000000001");
    private static readonly Guid ORD2 = Guid.Parse("EE100002-0000-0000-0000-000000000002");
    private static readonly Guid ORD3 = Guid.Parse("EE100003-0000-0000-0000-000000000003");
    private static readonly Guid ORD4 = Guid.Parse("EE100004-0000-0000-0000-000000000004");
    private static readonly Guid ORD5 = Guid.Parse("EE100005-0000-0000-0000-000000000005");
    private static readonly Guid CHILD0 = Guid.Parse("A319CD79-2B45-4507-89FD-318E26A5A26A");
    private static readonly Guid CHILD1 = Guid.Parse("4FCC8D2B-1488-47D8-ABD9-C1B2A36B6BA4");
    private static readonly Guid CHILD2 = Guid.Parse("67FF78A4-78A9-4BAF-9BE2-DF0331E6A7DE");
    private static readonly Guid CHILD3 = Guid.Parse("7C1FC73C-78E3-4F67-957D-9BB9FF0DFF99");

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
                    Gender = Gender.Male,
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

        // BCrypt hash with WorkFactor 12 — same as app's PasswordHasher
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
                Id = SCH1,
                SchoolName = "Trường THPT Phan Châu Trinh",
                Level = "THPT",
                LogoURL = "https://tamkhoiphat.com/wp-content/uploads/2024/03/logo-phan-chau-trinh.png",
                ContactInfo = "{\"email\":\"contact@thptphanchautrinh.edu.vn\",\"phone\":\"0236 3822 367\",\"address\":\"154 Lê Lợi, Hải Châu, Đà Nẵng\",\"academicYear\":\"2025-2026\",\"foundedYear\":1952,\"website\":\"https://thptphanchautrinh.edu.vn\",\"description\":\"Trường THPT trọng điểm quốc gia tại Đà Nẵng, nổi tiếng với chất lượng giáo dục hàng đầu khu vực miền Trung.\"}",
                CreatedAt = now
            },
            new School
            {
                Id = SCH2,
                SchoolName = "Trường THPT Trần Phú",
                Level = "THPT",
                LogoURL = "https://tamkhoiphat.com/wp-content/uploads/2025/10/logo-truong-tran-phu-da-nang.png",
                ContactInfo = "{\"email\":\"contact@thpttranphu.edu.vn\",\"phone\":\"0236 3895 289\",\"address\":\"11 Lê Thánh Tôn, Hải Châu, Đà Nẵng\",\"academicYear\":\"2025-2026\",\"foundedYear\":1965,\"website\":\"https://thpttranphu.edu.vn\",\"description\":\"Trường THPT công lập chất lượng cao tại trung tâm thành phố Đà Nẵng.\"}",
                CreatedAt = now
            },
            new School
            {
                Id = SCH3,
                SchoolName = "Trường THCS Nguyễn Huệ",
                Level = "THCS",
                LogoURL = "https://lh5.googleusercontent.com/proxy/bjv3ZY933qljz5p5wmzzE6BIxUZHz7MyOnZHJ-lceOCV_sWtg_OpkxzyTmws1SIPAXMHoY2fY0gshhLB0aUUksRIaKzAriSS9GbcZOC-eZELICGp-eet5PCTFxWlTOQ3hXBaP2BdgovEn_g5eFjQ5pRUIyhogvCoAmaOSGGIRws",
                ContactInfo = "{\"email\":\"contact@thcsnguyenhue.edu.vn\",\"phone\":\"0236 3823 456\",\"address\":\"62 Nguyễn Chí Thanh, Hải Châu, Đà Nẵng\",\"academicYear\":\"2025-2026\",\"foundedYear\":1975,\"website\":\"https://thcsnguyenhue.edu.vn\",\"description\":\"Trường THCS hàng đầu quận Hải Châu, Đà Nẵng với nhiều thành tích học tập xuất sắc.\"}",
                CreatedAt = now
            }
        );
        await db.SaveChangesAsync();

        // ── SchoolWallets ──────────────────────────────────────────────────────
        db.SchoolWallets.AddRange(
            new SchoolWallet { Id = Guid.NewGuid(), SchoolID = SCH1, Balance = 2500000, BankCode = "VCB", BankName = "Vietcombank", BankAccountNumber = "0491000234567", BankAccountName = "TRUONG THPT PHAN CHAU TRINH", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new SchoolWallet { Id = Guid.NewGuid(), SchoolID = SCH2, Balance = 1800000, BankCode = "TCB", BankName = "Techcombank", BankAccountNumber = "19035678901234", BankAccountName = "TRUONG THPT TRAN PHU", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new SchoolWallet { Id = Guid.NewGuid(), SchoolID = SCH3, Balance = 950000, BankCode = "BIDV", BankName = "BIDV", BankAccountNumber = "31410001234567", BankAccountName = "TRUONG THCS NGUYEN HUE", IsActive = true, CreatedAt = now, UpdatedAt = now }
        );
        await db.SaveChangesAsync();

        // ── Providers (Da Nang garment companies) ──────────────────────────────
        db.Providers.AddRange(
            new Provider { Id = PRV1, ProviderName = "Công ty May Mặc Hoàng Gia", ContactPersonName = "Hoàng Minh Tuấn", Phone = "0905123456", Address = "Khu CN Hoà Khánh, Liên Chiểu, Đà Nẵng", Status = ProviderStatus.Active, IsDeleted = false },
            new Provider { Id = PRV2, ProviderName = "Đồng Phục Sơn Trà", ContactPersonName = "Võ Thị Lan Anh", Phone = "0935789012", Address = "78 Ngô Quyền, Sơn Trà, Đà Nẵng", Status = ProviderStatus.Active, IsDeleted = false },
            new Provider { Id = PRV3, ProviderName = "Xưởng May Thanh Khê", ContactPersonName = "Bùi Đình Phong", Phone = "0769456789", Address = "215 Điện Biên Phủ, Thanh Khê, Đà Nẵng", Status = ProviderStatus.Active, IsDeleted = false }
        );
        await db.SaveChangesAsync();

        // ── Users ─────────────────────────────────────────────────────────────
        db.Users.AddRange(
            // School manager accounts (realistic Da Nang Vietnamese names)
            new User { Id = USR_SCH1, FullName = "Nguyễn Thị Thanh Hà", Email = "school1@vtos.com", PasswordHash = hash, Phone = "0905112233", Gender = Gender.Female, Avatar = "avatar.jpg", RoleID = ROLE_SCHOOL, SchoolID = SCH1, IsActive = true, IsDeleted = false, CreatedAt = now },
            new User { Id = USR_SCH2, FullName = "Trần Văn Minh", Email = "school2@vtos.com", PasswordHash = hash, Phone = "0935445566", Gender = Gender.Male, Avatar = "avatar.jpg", RoleID = ROLE_SCHOOL, SchoolID = SCH2, IsActive = true, IsDeleted = false, CreatedAt = now },
            new User { Id = USR_SCH3, FullName = "Lê Thị Bích Ngọc", Email = "school3@vtos.com", PasswordHash = hash, Phone = "0769778899", Gender = Gender.Female, Avatar = "avatar.jpg", RoleID = ROLE_SCHOOL, SchoolID = SCH3, IsActive = true, IsDeleted = false, CreatedAt = now },
            // Parent accounts
            new User { Id = USR_P0, FullName = "Trần Thị Hương", Email = "parent0@vtos.com", PasswordHash = hash, Phone = "0905101010", Gender = Gender.Female, Avatar = "avatar.jpg", RoleID = ROLE_PARENT, IsActive = true, IsDeleted = false, CreatedAt = now },
            new User { Id = USR_P1, FullName = "Lê Văn Đức", Email = "parent1@vtos.com", PasswordHash = hash, Phone = "0935202020", Gender = Gender.Male, Avatar = "avatar.jpg", RoleID = ROLE_PARENT, IsActive = true, IsDeleted = false, CreatedAt = now },
            new User { Id = USR_P2, FullName = "Phạm Thị Mai", Email = "parent2@vtos.com", PasswordHash = hash, Phone = "0769303030", Gender = Gender.Female, Avatar = "avatar.jpg", RoleID = ROLE_PARENT, IsActive = true, IsDeleted = false, CreatedAt = now },
            new User { Id = USR_P3, FullName = "Ngô Quang Hải", Email = "parent3@vtos.com", PasswordHash = hash, Phone = "0905404040", Gender = Gender.Male, Avatar = "avatar.jpg", RoleID = ROLE_PARENT, IsActive = true, IsDeleted = false, CreatedAt = now },
            // Provider accounts
            new User { Id = USR_PRV1, FullName = "Hoàng Minh Tuấn", Email = "provider1@vtos.com", PasswordHash = hash, Phone = "0905123456", Gender = Gender.Male, Avatar = "avatar.jpg", RoleID = ROLE_PROVIDER, ProviderID = PRV1, IsActive = true, IsDeleted = false, CreatedAt = now },
            new User { Id = USR_PRV2, FullName = "Võ Thị Lan Anh", Email = "provider2@vtos.com", PasswordHash = hash, Phone = "0935789012", Gender = Gender.Female, Avatar = "avatar.jpg", RoleID = ROLE_PROVIDER, ProviderID = PRV2, IsActive = true, IsDeleted = false, CreatedAt = now }
        );
        await db.SaveChangesAsync();

        // ── SizeCharts ────────────────────────────────────────────────────────
        db.SizeCharts.AddRange(
            new SizeChart { Id = SC1, ChartName = "Bảng size THPT Phan Châu Trinh", Unit = "cm", CreatedAt = now },
            new SizeChart { Id = SC2, ChartName = "Bảng size THPT Trần Phú", Unit = "cm", CreatedAt = now },
            new SizeChart { Id = SC3, ChartName = "Bảng size THCS Nguyễn Huệ", Unit = "cm", CreatedAt = now }
        );
        await db.SaveChangesAsync();

        // ── Outfits (realistic uniform items) ─────────────────────────────────
        db.Outfits.AddRange(
            new Outfit { Id = OFT1, SchoolID = SCH1, OutfitName = "Áo sơ mi trắng THPT Phan Châu Trinh", Description = "Áo sơ mi trắng dài tay, logo trường thêu ngực trái, vải kate cao cấp", Price = 185000, OutfitType = OutfitType.Uniform, SizeChartID = SC1, IsAvailable = true, IsCustomizable = false, IsDeleted = false, CreatedAt = now },
            new Outfit { Id = OFT2, SchoolID = SCH2, OutfitName = "Quần tây xanh THPT Trần Phú", Description = "Quần tây xanh đen, vải tốt không nhăn, ống suông", Price = 195000, OutfitType = OutfitType.Uniform, SizeChartID = SC2, IsAvailable = true, IsCustomizable = false, IsDeleted = false, CreatedAt = now },
            new Outfit { Id = OFT3, SchoolID = SCH3, OutfitName = "Áo thể dục THCS Nguyễn Huệ", Description = "Áo thể dục cổ tròn, logo trường in ngực, vải thun cotton thoáng mát", Price = 120000, OutfitType = OutfitType.Sportswear, SizeChartID = SC3, IsAvailable = true, IsCustomizable = false, IsDeleted = false, CreatedAt = now },
            new Outfit { Id = OFT4, SchoolID = SCH1, OutfitName = "Áo dài trắng nữ THPT Phan Châu Trinh", Description = "Áo dài trắng truyền thống, vải lụa mềm mại, dành cho nữ sinh", Price = 350000, OutfitType = OutfitType.Uniform, SizeChartID = SC1, IsAvailable = true, IsCustomizable = false, IsDeleted = false, CreatedAt = now },
            new Outfit { Id = OFT5, SchoolID = SCH2, OutfitName = "Áo khoác đồng phục THPT Trần Phú", Description = "Áo khoác gió đồng phục, logo trường thêu, có mũ trùm", Price = 280000, OutfitType = OutfitType.Uniform, SizeChartID = SC2, IsAvailable = true, IsCustomizable = false, IsDeleted = false, CreatedAt = now }
        );
        await db.SaveChangesAsync();

        // ── Campaigns ─────────────────────────────────────────────────────────
        db.Campaigns.AddRange(
            new Campaign { Id = CAM1, SchoolID = SCH1, CampaignName = "Đồng phục Năm học 2025-2026 - PCT", Description = "Chiến dịch đặt đồng phục chính thức cho năm học mới 2025-2026 tại THPT Phan Châu Trinh", StartDate = new DateTime(2026,1,15), EndDate = new DateTime(2026,4,30), Status = CampaignStatus.Active, CreatedAt = now },
            new Campaign { Id = CAM2, SchoolID = SCH2, CampaignName = "Đồng phục Năm học 2025-2026 - TP", Description = "Đặt đồng phục cho học sinh THPT Trần Phú năm học 2025-2026", StartDate = new DateTime(2026,2,1), EndDate = new DateTime(2026,5,15), Status = CampaignStatus.Active, CreatedAt = now },
            new Campaign { Id = CAM3, SchoolID = SCH3, CampaignName = "Đồng phục Năm học 2025-2026 - NH", Description = "Chiến dịch đồng phục mới cho THCS Nguyễn Huệ, bao gồm áo thể dục và đồng phục hàng ngày", StartDate = new DateTime(2026,1,20), EndDate = new DateTime(2026,3,31), Status = CampaignStatus.Active, CreatedAt = now }
        );
        await db.SaveChangesAsync();

        // ── CampaignOutfits ───────────────────────────────────────────────────
        db.CampaignOutfits.AddRange(
            new CampaignOutfit { Id = Guid.NewGuid(), CampaignID = CAM1, OutfitID = OFT1, ProviderID = PRV1, CampaignPrice = 185000, MaxQuantity = 500 },
            new CampaignOutfit { Id = Guid.NewGuid(), CampaignID = CAM1, OutfitID = OFT4, ProviderID = PRV1, CampaignPrice = 350000, MaxQuantity = 200 },
            new CampaignOutfit { Id = Guid.NewGuid(), CampaignID = CAM2, OutfitID = OFT2, ProviderID = PRV2, CampaignPrice = 195000, MaxQuantity = 400 },
            new CampaignOutfit { Id = Guid.NewGuid(), CampaignID = CAM2, OutfitID = OFT5, ProviderID = PRV2, CampaignPrice = 280000, MaxQuantity = 300 },
            new CampaignOutfit { Id = Guid.NewGuid(), CampaignID = CAM3, OutfitID = OFT3, ProviderID = PRV3, CampaignPrice = 120000, MaxQuantity = 350 }
        );
        await db.SaveChangesAsync();

        // ── Children (realistic Da Nang student profiles) ─────────────────────
        db.ChildProfiles.AddRange(
            new ChildProfile { Id = CHILD0, ParentUserID = USR_P0, FullName = "Trần Minh Khôi", DOB = new DateTime(2010, 3, 15), Age = 16, Grade = "Lớp 10A1", Gender = Gender.Male, SchoolID = SCH1, IsDeleted = false, Avatar = "avatar.jpg", HeightCm = 168, WeightKg = 55 },
            new ChildProfile { Id = CHILD1, ParentUserID = USR_P1, FullName = "Lê Ngọc Bảo Trân", DOB = new DateTime(2009, 8, 22), Age = 17, Grade = "Lớp 11B3", Gender = Gender.Female, SchoolID = SCH2, IsDeleted = false, Avatar = "avatar.jpg", HeightCm = 158, WeightKg = 48 },
            new ChildProfile { Id = CHILD2, ParentUserID = USR_P2, FullName = "Phạm Gia Huy", DOB = new DateTime(2012, 11, 5), Age = 14, Grade = "Lớp 8A2", Gender = Gender.Male, SchoolID = SCH3, IsDeleted = false, Avatar = "avatar.jpg", HeightCm = 155, WeightKg = 45 },
            new ChildProfile { Id = CHILD3, ParentUserID = USR_P3, FullName = "Ngô Thùy Linh", DOB = new DateTime(2010, 6, 18), Age = 16, Grade = "Lớp 10A5", Gender = Gender.Female, SchoolID = SCH1, IsDeleted = false, Avatar = "avatar.jpg", HeightCm = 160, WeightKg = 50 }
        );
        await db.SaveChangesAsync();

        // ── CAM4: Locked campaign for testing GenerateProductionOrder ─────
        db.Campaigns.Add(
            new Campaign { Id = CAM4, SchoolID = SCH1, CampaignName = "Đồng phục Hè 2026 - PCT (Đã khóa)", Description = "Chiến dịch đồng phục mùa hè cho THPT Phan Châu Trinh - đã khóa để tạo đơn sản xuất", StartDate = new DateTime(2026,3,1), EndDate = new DateTime(2026,6,30), Status = CampaignStatus.Locked, CreatedAt = now }
        );
        await db.SaveChangesAsync();

        db.CampaignOutfits.Add(
            new CampaignOutfit { Id = Guid.NewGuid(), CampaignID = CAM4, OutfitID = OFT1, ProviderID = PRV1, CampaignPrice = 185000, MaxQuantity = 500 }
        );
        await db.SaveChangesAsync();

        // ── ProductVariants for Outfit 1 (Áo sơ mi PCT) ──────────────────
        db.ProductVariants.AddRange(
            new ProductVariant { Id = PV1_S, OutfitID = OFT1, Size = "S", Price = 185000, StockQuantity = 100, SKUCode = "PCT-AOSOMI-S", IsDeleted = false },
            new ProductVariant { Id = PV1_M, OutfitID = OFT1, Size = "M", Price = 185000, StockQuantity = 100, SKUCode = "PCT-AOSOMI-M", IsDeleted = false },
            new ProductVariant { Id = PV1_L, OutfitID = OFT1, Size = "L", Price = 185000, StockQuantity = 100, SKUCode = "PCT-AOSOMI-L", IsDeleted = false }
        );
        // ── ProductVariants for Outfit 2 (Quần tây TP) ────────────────────
        db.ProductVariants.AddRange(
            new ProductVariant { Id = PV2_S, OutfitID = OFT2, Size = "S", Price = 195000, StockQuantity = 80, SKUCode = "TP-QUANTAY-S", IsDeleted = false },
            new ProductVariant { Id = PV2_M, OutfitID = OFT2, Size = "M", Price = 195000, StockQuantity = 80, SKUCode = "TP-QUANTAY-M", IsDeleted = false }
        );
        await db.SaveChangesAsync();

        // ── Parent Orders (mixed statuses for realistic testing) ──────────
        db.Orders.AddRange(
            // ORD1: Trần Minh Khôi (child of Trần Thị Hương) — CAM4 Locked campaign
            new Order { Id = ORD1, ChildProfileID = CHILD0, CampaignID = CAM4, OrderDate = now.AddDays(-10), OrderStatus = OrderStatus.Confirmed, TotalAmount = 555000, ShippingAddress = "42 Nguyễn Văn Linh, Hải Châu, Đà Nẵng", CreatedAt = now.AddDays(-10) },
            // ORD2: Ngô Thùy Linh (child of Ngô Quang Hải) — CAM4 Locked campaign
            new Order { Id = ORD2, ChildProfileID = CHILD3, CampaignID = CAM4, OrderDate = now.AddDays(-8), OrderStatus = OrderStatus.Confirmed, TotalAmount = 370000, ShippingAddress = "15 Phan Đăng Lưu, Hải Châu, Đà Nẵng", CreatedAt = now.AddDays(-8) },
            // ORD3: Trần Minh Khôi — Active campaign CAM1
            new Order { Id = ORD3, ChildProfileID = CHILD0, CampaignID = CAM1, OrderDate = now.AddDays(-5), OrderStatus = OrderStatus.Paid, TotalAmount = 185000, ShippingAddress = "42 Nguyễn Văn Linh, Hải Châu, Đà Nẵng", CreatedAt = now.AddDays(-5) },
            // ORD4: Lê Ngọc Bảo Trân — Active campaign CAM2 — Delivered
            new Order { Id = ORD4, ChildProfileID = CHILD1, CampaignID = CAM2, OrderDate = now.AddDays(-20), OrderStatus = OrderStatus.Delivered, TotalAmount = 475000, ShippingAddress = "88 Trần Cao Vân, Thanh Khê, Đà Nẵng", CreatedAt = now.AddDays(-20) },
            // ORD5: Phạm Gia Huy — Active campaign CAM3 — Cancelled
            new Order { Id = ORD5, ChildProfileID = CHILD2, CampaignID = CAM3, OrderDate = now.AddDays(-3), OrderStatus = OrderStatus.Cancelled, TotalAmount = 120000, ShippingAddress = "23 Lê Duẩn, Hải Châu, Đà Nẵng", CreatedAt = now.AddDays(-3) }
        );
        await db.SaveChangesAsync();

        // ── OrderItems ────────────────────────────────────────────────────────
        db.OrderItems.AddRange(
            // ORD1: 2x áo sơ mi size S + 1x size M = 555,000
            new OrderItem { Id = Guid.NewGuid(), OrderID = ORD1, ProductVariantID = PV1_S, Quantity = 2, UnitPrice = 185000, SizeOrdered = "S", IsCustomOrder = false, CreatedAt = now.AddDays(-10) },
            new OrderItem { Id = Guid.NewGuid(), OrderID = ORD1, ProductVariantID = PV1_M, Quantity = 1, UnitPrice = 185000, SizeOrdered = "M", IsCustomOrder = false, CreatedAt = now.AddDays(-10) },
            // ORD2: 1x áo sơ mi size M + 1x size L = 370,000
            new OrderItem { Id = Guid.NewGuid(), OrderID = ORD2, ProductVariantID = PV1_M, Quantity = 1, UnitPrice = 185000, SizeOrdered = "M", IsCustomOrder = false, CreatedAt = now.AddDays(-8) },
            new OrderItem { Id = Guid.NewGuid(), OrderID = ORD2, ProductVariantID = PV1_L, Quantity = 1, UnitPrice = 185000, SizeOrdered = "L", IsCustomOrder = false, CreatedAt = now.AddDays(-8) },
            // ORD3: 1x áo sơ mi size M = 185,000
            new OrderItem { Id = Guid.NewGuid(), OrderID = ORD3, ProductVariantID = PV1_M, Quantity = 1, UnitPrice = 185000, SizeOrdered = "M", IsCustomOrder = false, CreatedAt = now.AddDays(-5) },
            // ORD4: 1x quần tây size S + 1x áo khoác = 475,000
            new OrderItem { Id = Guid.NewGuid(), OrderID = ORD4, ProductVariantID = PV2_S, Quantity = 1, UnitPrice = 195000, SizeOrdered = "S", IsCustomOrder = false, CreatedAt = now.AddDays(-20) },
            new OrderItem { Id = Guid.NewGuid(), OrderID = ORD4, ProductVariantID = PV2_M, Quantity = 1, UnitPrice = 280000, SizeOrdered = "M", IsCustomOrder = false, CreatedAt = now.AddDays(-20) },
            // ORD5: 1x áo thể dục size M = 120,000
            new OrderItem { Id = Guid.NewGuid(), OrderID = ORD5, ProductVariantID = PV1_M, Quantity = 1, UnitPrice = 120000, SizeOrdered = "M", IsCustomOrder = false, CreatedAt = now.AddDays(-3) }
        );
        await db.SaveChangesAsync();

        // ── ProductionBatches ─────────────────────────────────────────────────
        db.ProductionBatches.AddRange(
            new ProductionBatch { Id = BATCH1, CampaignID = CAM1, ProviderID = PRV1, BatchName = "Lô sản xuất PCT - Áo sơ mi HK2", TotalQuantity = 150, CreatedDate = now.AddDays(-15), Status = ProductionBatchStatus.Pending,      DeliveryDeadline = new DateTime(2026,4,15), IsDeleted = false },
            new ProductionBatch { Id = BATCH2, CampaignID = CAM2, ProviderID = PRV2, BatchName = "Lô sản xuất TP - Quần tây HK2", TotalQuantity = 100, CreatedDate = now.AddDays(-12), Status = ProductionBatchStatus.Approved,     DeliveryDeadline = new DateTime(2026,5,1),  IsDeleted = false },
            new ProductionBatch { Id = BATCH3, CampaignID = CAM3, ProviderID = PRV3, BatchName = "Lô sản xuất NH - Áo thể dục", TotalQuantity = 80,  CreatedDate = now.AddDays(-10), Status = ProductionBatchStatus.InProduction, DeliveryDeadline = new DateTime(2026,3,20), IsDeleted = false, ProcessedAt = now.AddDays(-7) }
        );
        await db.SaveChangesAsync();

        // ── ProductionBatchItems ──────────────────────────────────────────────
        db.ProductionBatchItems.AddRange(
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH1, OutfitID = OFT1, Size = "S", Quantity = 50, UnitPrice = 125000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH1, OutfitID = OFT1, Size = "M", Quantity = 65, UnitPrice = 125000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH1, OutfitID = OFT1, Size = "L", Quantity = 35, UnitPrice = 125000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH2, OutfitID = OFT2, Size = "M", Quantity = 55, UnitPrice = 130000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH2, OutfitID = OFT2, Size = "L", Quantity = 45, UnitPrice = 130000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH3, OutfitID = OFT3, Size = "M", Quantity = 80, UnitPrice = 75000 }
        );
        await db.SaveChangesAsync();

        // ── Complaints ────────────────────────────────────────────────────────
        db.Complaints.AddRange(
            new Complaint { Id = Guid.NewGuid(), CampaignID = CAM1, BatchID = BATCH1, SchoolID = SCH1, ProviderID = PRV1, Title = "Áo sơ mi bị phai màu sau khi giặt", Description = "Một số áo sơ mi lô đầu tiên bị phai vàng sau 2 lần giặt máy, nghi do vải không đạt chuẩn", Status = ComplaintStatus.Open, CreatedAt = now.AddDays(-3) },
            new Complaint { Id = Guid.NewGuid(), CampaignID = CAM1, BatchID = BATCH1, SchoolID = SCH1, ProviderID = PRV1, Title = "Thiếu hàng size M 3 sản phẩm", Description = "Đơn giao 65 cái size M nhưng chỉ nhận được 62, thiếu 3 cái", Status = ComplaintStatus.InProgress, CreatedAt = now.AddDays(-5) },
            new Complaint { Id = Guid.NewGuid(), CampaignID = CAM2, BatchID = BATCH2, SchoolID = SCH2, ProviderID = PRV2, Title = "Giao hàng trễ hạn 10 ngày", Description = "Hạn giao là 01/03 nhưng đến 11/03 vẫn chưa nhận được hàng. Ảnh hưởng đến lịch phát đồng phục cho học sinh", Status = ComplaintStatus.Resolved, CreatedAt = now.AddDays(-12) }
        );
        await db.SaveChangesAsync();

        // ── PaymentTransactions (for wallet seeded balance) ───────────────────
        db.PaymentTransactions.AddRange(
            new PaymentTransaction { Id = Guid.NewGuid(), OrderID = ORD4, Amount = 475000, TransactionStatus = PaymentStatus.Completed, GatewayType = PaymentGatewayType.PayOS, TransactionType = TransactionType.OrderPayment, Description = "Phụ huynh Lê Văn Đức thanh toán đơn hàng ORD4", TransactionTimestamp = now.AddDays(-19), CreatedAt = now.AddDays(-19) },
            new PaymentTransaction { Id = Guid.NewGuid(), OrderID = ORD3, Amount = 185000, TransactionStatus = PaymentStatus.Completed, GatewayType = PaymentGatewayType.PayOS, TransactionType = TransactionType.OrderPayment, Description = "Phụ huynh Trần Thị Hương thanh toán đơn hàng ORD3", TransactionTimestamp = now.AddDays(-4), CreatedAt = now.AddDays(-4) }
        );
        await db.SaveChangesAsync();
    }
}
