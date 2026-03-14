using Microsoft.EntityFrameworkCore;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;
using VTOS.Infrastructure.Persistence;

namespace VTOS.Infrastructure;

/// <summary>
/// Seeds initial test data into the database at application startup.
/// Runs only when the Role table is empty — safe to call on every startup.
///
/// Test accounts (password: Test@1234):
///   school1@vtos.com   — School role, linked to School 1
///   school2@vtos.com   — School role, linked to School 2
///   school3@vtos.com   — School role, linked to School 3
///   parent0@vtos.com   — Parent role
///   parent1@vtos.com   — Parent role
///   provider1@vtos.com — Provider role, linked to Provider 1
///   provider2@vtos.com — Provider role, linked to Provider 2
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

    private static readonly Guid SC1 = Guid.Parse("B810392C-E8B6-4FB2-8D61-A9D55ED2F101");
    private static readonly Guid SC2 = Guid.Parse("4C9F7AE2-8E8E-4951-8F2C-E584A3C92E8A");
    private static readonly Guid SC3 = Guid.Parse("C524C3A4-8645-459D-AD1D-03886DE08CC7");

    private static readonly Guid BATCH1 = Guid.Parse("AAA00001-0000-0000-0000-000000000001");
    private static readonly Guid BATCH2 = Guid.Parse("AAA00002-0000-0000-0000-000000000002");
    private static readonly Guid BATCH3 = Guid.Parse("AAA00003-0000-0000-0000-000000000003");

    public static async Task SeedAsync(VTOSDbContext db)
    {
        // Guard: only seed when database is empty
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

        // ── Schools ────────────────────────────────────────────────────────────
        db.Schools.AddRange(
            new School { Id = SCH1, SchoolName = "Truong THPT So 1", LogoURL = "https://logo1.png", ContactInfo = "Dia chi 1", CreatedAt = now },
            new School { Id = SCH2, SchoolName = "Truong THPT So 2", LogoURL = "https://logo2.png", ContactInfo = "Dia chi 2", CreatedAt = now },
            new School { Id = SCH3, SchoolName = "Truong THPT So 3", LogoURL = "https://logo3.png", ContactInfo = "Dia chi 3", CreatedAt = now }
        );
        await db.SaveChangesAsync();

        // ── SchoolWallets ──────────────────────────────────────────────────────
        db.SchoolWallets.AddRange(
            new SchoolWallet { Id = Guid.NewGuid(), SchoolID = SCH1, Balance = 0, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new SchoolWallet { Id = Guid.NewGuid(), SchoolID = SCH2, Balance = 0, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new SchoolWallet { Id = Guid.NewGuid(), SchoolID = SCH3, Balance = 0, IsActive = true, CreatedAt = now, UpdatedAt = now }
        );
        await db.SaveChangesAsync();

        // ── Providers ─────────────────────────────────────────────────────────
        db.Providers.AddRange(
            new Provider { Id = PRV1, ProviderName = "Nha cung cap 1", Status = "Active", IsDeleted = false },
            new Provider { Id = PRV2, ProviderName = "Nha cung cap 2", Status = "Active", IsDeleted = false },
            new Provider { Id = PRV3, ProviderName = "Nha cung cap 3", Status = "Active", IsDeleted = false }
        );
        await db.SaveChangesAsync();

        // ── Users ─────────────────────────────────────────────────────────────
        db.Users.AddRange(
            // School accounts
            new User { Id = USR_SCH1, FullName = "Quan ly Truong 1", Email = "school1@vtos.com", PasswordHash = hash, Phone = "0900000011", Gender = Gender.Male, Avatar = "avatar.jpg", RoleID = ROLE_SCHOOL, SchoolID = SCH1, IsActive = true, IsDeleted = false, CreatedAt = now },
            new User { Id = USR_SCH2, FullName = "Quan ly Truong 2", Email = "school2@vtos.com", PasswordHash = hash, Phone = "0900000012", Gender = Gender.Male, Avatar = "avatar.jpg", RoleID = ROLE_SCHOOL, SchoolID = SCH2, IsActive = true, IsDeleted = false, CreatedAt = now },
            new User { Id = USR_SCH3, FullName = "Quan ly Truong 3", Email = "school3@vtos.com", PasswordHash = hash, Phone = "0900000013", Gender = Gender.Male, Avatar = "avatar.jpg", RoleID = ROLE_SCHOOL, SchoolID = SCH3, IsActive = true, IsDeleted = false, CreatedAt = now },
            // Parent accounts
            new User { Id = USR_P0, FullName = "Phu huynh 0", Email = "parent0@vtos.com", PasswordHash = hash, Phone = "0900000000", Gender = Gender.Male, Avatar = "avatar.jpg", RoleID = ROLE_PARENT, IsActive = true, IsDeleted = false, CreatedAt = now },
            new User { Id = USR_P1, FullName = "Phu huynh 1", Email = "parent1@vtos.com", PasswordHash = hash, Phone = "0900000001", Gender = Gender.Male, Avatar = "avatar.jpg", RoleID = ROLE_PARENT, IsActive = true, IsDeleted = false, CreatedAt = now },
            new User { Id = USR_P2, FullName = "Phu huynh 2", Email = "parent2@vtos.com", PasswordHash = hash, Phone = "0900000002", Gender = Gender.Male, Avatar = "avatar.jpg", RoleID = ROLE_PARENT, IsActive = true, IsDeleted = false, CreatedAt = now },
            new User { Id = USR_P3, FullName = "Phu huynh 3", Email = "parent3@vtos.com", PasswordHash = hash, Phone = "0900000003", Gender = Gender.Male, Avatar = "avatar.jpg", RoleID = ROLE_PARENT, IsActive = true, IsDeleted = false, CreatedAt = now },
            // Provider accounts
            new User { Id = USR_PRV1, FullName = "Nha cung cap 1", Email = "provider1@vtos.com", PasswordHash = hash, Phone = "0900000021", Gender = Gender.Male, Avatar = "avatar.jpg", RoleID = ROLE_PROVIDER, ProviderID = PRV1, IsActive = true, IsDeleted = false, CreatedAt = now },
            new User { Id = USR_PRV2, FullName = "Nha cung cap 2", Email = "provider2@vtos.com", PasswordHash = hash, Phone = "0900000022", Gender = Gender.Male, Avatar = "avatar.jpg", RoleID = ROLE_PROVIDER, ProviderID = PRV2, IsActive = true, IsDeleted = false, CreatedAt = now }
        );
        await db.SaveChangesAsync();

        // ── SizeCharts ────────────────────────────────────────────────────────
        db.SizeCharts.AddRange(
            new SizeChart { Id = SC1, ChartName = "Bang size Truong 1", Unit = "cm", CreatedAt = now },
            new SizeChart { Id = SC2, ChartName = "Bang size Truong 2", Unit = "cm", CreatedAt = now },
            new SizeChart { Id = SC3, ChartName = "Bang size Truong 3", Unit = "cm", CreatedAt = now }
        );
        await db.SaveChangesAsync();

        // ── Outfits ───────────────────────────────────────────────────────────
        db.Outfits.AddRange(
            new Outfit { Id = OFT1, SchoolID = SCH1, OutfitName = "Dong phuc Truong 1", Price = 200000, OutfitType = OutfitType.Uniform, SizeChartID = SC1, IsAvailable = true, IsCustomizable = false, IsDeleted = false, CreatedAt = now },
            new Outfit { Id = OFT2, SchoolID = SCH2, OutfitName = "Dong phuc Truong 2", Price = 200000, OutfitType = OutfitType.Uniform, SizeChartID = SC2, IsAvailable = true, IsCustomizable = false, IsDeleted = false, CreatedAt = now },
            new Outfit { Id = OFT3, SchoolID = SCH3, OutfitName = "Dong phuc Truong 3", Price = 200000, OutfitType = OutfitType.Uniform, SizeChartID = SC3, IsAvailable = true, IsCustomizable = false, IsDeleted = false, CreatedAt = now }
        );
        await db.SaveChangesAsync();

        // ── Campaigns ─────────────────────────────────────────────────────────
        db.Campaigns.AddRange(
            new Campaign { Id = CAM1, SchoolID = SCH1, CampaignName = "Chien dich Truong 1 - 2026", StartDate = new DateTime(2026,1,1), EndDate = new DateTime(2026,12,31), Status = CampaignStatus.Active, CreatedAt = now },
            new Campaign { Id = CAM2, SchoolID = SCH2, CampaignName = "Chien dich Truong 2 - 2026", StartDate = new DateTime(2026,1,1), EndDate = new DateTime(2026,12,31), Status = CampaignStatus.Active, CreatedAt = now },
            new Campaign { Id = CAM3, SchoolID = SCH3, CampaignName = "Chien dich Truong 3 - 2026", StartDate = new DateTime(2026,1,1), EndDate = new DateTime(2026,12,31), Status = CampaignStatus.Active, CreatedAt = now }
        );
        await db.SaveChangesAsync();

        // ── CampaignOutfits ───────────────────────────────────────────────────
        db.CampaignOutfits.AddRange(
            new CampaignOutfit { Id = Guid.NewGuid(), CampaignID = CAM1, OutfitID = OFT1, ProviderID = PRV1, CampaignPrice = 200000, MaxQuantity = 500 },
            new CampaignOutfit { Id = Guid.NewGuid(), CampaignID = CAM2, OutfitID = OFT2, ProviderID = PRV2, CampaignPrice = 200000, MaxQuantity = 300 },
            new CampaignOutfit { Id = Guid.NewGuid(), CampaignID = CAM3, OutfitID = OFT3, ProviderID = PRV3, CampaignPrice = 200000, MaxQuantity = 400 }
        );
        await db.SaveChangesAsync();

        // ── Children ──────────────────────────────────────────────────────────
        db.ChildProfiles.AddRange(
            new ChildProfile { Id = Guid.Parse("A319CD79-2B45-4507-89FD-318E26A5A26A"), ParentUserID = USR_P0, FullName = "Hoc sinh 0", Age = 10, Grade = "Lop 5", Gender = Gender.Male, SchoolID = SCH1, IsDeleted = false, Avatar = "avatar.jpg", HeightCm = 140, WeightKg = 35 },
            new ChildProfile { Id = Guid.Parse("4FCC8D2B-1488-47D8-ABD9-C1B2A36B6BA4"), ParentUserID = USR_P1, FullName = "Hoc sinh 1", Age = 10, Grade = "Lop 5", Gender = Gender.Male, SchoolID = SCH2, IsDeleted = false, Avatar = "avatar.jpg", HeightCm = 140, WeightKg = 35 },
            new ChildProfile { Id = Guid.Parse("67FF78A4-78A9-4BAF-9BE2-DF0331E6A7DE"), ParentUserID = USR_P2, FullName = "Hoc sinh 2", Age = 10, Grade = "Lop 5", Gender = Gender.Male, SchoolID = SCH3, IsDeleted = false, Avatar = "avatar.jpg", HeightCm = 140, WeightKg = 35 },
            new ChildProfile { Id = Guid.Parse("7C1FC73C-78E3-4F67-957D-9BB9FF0DFF99"), ParentUserID = USR_P3, FullName = "Hoc sinh 3", Age = 10, Grade = "Lop 5", Gender = Gender.Male, SchoolID = SCH1, IsDeleted = false, Avatar = "avatar.jpg", HeightCm = 140, WeightKg = 35 }
        );
        await db.SaveChangesAsync();

        // ── ProductionBatches (UC 3.9 test data) ─────────────────────────────
        db.ProductionBatches.AddRange(
            new ProductionBatch { Id = BATCH1, CampaignID = CAM1, ProviderID = PRV1, BatchName = "Don san xuat Truong 1", TotalQuantity = 15, CreatedDate = now, Status = ProductionBatchStatus.Pending,      DeliveryDeadline = new DateTime(2026,6,30),  IsDeleted = false },
            new ProductionBatch { Id = BATCH2, CampaignID = CAM2, ProviderID = PRV2, BatchName = "Don san xuat Truong 2", TotalQuantity = 10, CreatedDate = now, Status = ProductionBatchStatus.Approved,     DeliveryDeadline = new DateTime(2026,7,15),  IsDeleted = false },
            new ProductionBatch { Id = BATCH3, CampaignID = CAM3, ProviderID = PRV3, BatchName = "Don san xuat Truong 3", TotalQuantity =  8, CreatedDate = now, Status = ProductionBatchStatus.InProduction, DeliveryDeadline = new DateTime(2026,8,1),   IsDeleted = false, ProcessedAt = now }
        );
        await db.SaveChangesAsync();

        // ── ProductionBatchItems ──────────────────────────────────────────────
        db.ProductionBatchItems.AddRange(
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH1, OutfitID = OFT1, Size = "S", Quantity = 5, UnitPrice = 200000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH1, OutfitID = OFT1, Size = "M", Quantity = 7, UnitPrice = 200000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH1, OutfitID = OFT1, Size = "L", Quantity = 3, UnitPrice = 200000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH2, OutfitID = OFT2, Size = "M", Quantity = 6, UnitPrice = 200000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH2, OutfitID = OFT2, Size = "L", Quantity = 4, UnitPrice = 200000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH3, OutfitID = OFT3, Size = "M", Quantity = 8, UnitPrice = 200000 }
        );
        await db.SaveChangesAsync();

        // ── Complaints ────────────────────────────────────────────────────────
        db.Complaints.AddRange(
            new Complaint { Id = Guid.NewGuid(), CampaignID = CAM1, BatchID = BATCH1, SchoolID = SCH1, ProviderID = PRV1, Title = "An pham bi loi mau",  Description = "Trang phuc bi in sai mau so voi mau chon", Status = ComplaintStatus.Open,       CreatedAt = now },
            new Complaint { Id = Guid.NewGuid(), CampaignID = CAM1, BatchID = BATCH1, SchoolID = SCH1, ProviderID = PRV1, Title = "Kich thuoc sai",       Description = "Size M giao thieu 3 san pham",             Status = ComplaintStatus.InProgress, CreatedAt = now },
            new Complaint { Id = Guid.NewGuid(), CampaignID = CAM2, BatchID = BATCH2, SchoolID = SCH2, ProviderID = PRV2, Title = "Giao hang tre han",    Description = "Qua han giao 2 tuan chua nhan duoc hang",  Status = ComplaintStatus.Resolved,   CreatedAt = now }
        );
        await db.SaveChangesAsync();
    }
}
