using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using VTOS.Domain.Entities;
using VTOS.Domain.Enums;
using VTOS.Infrastructure.Persistence;

namespace VTOS.Infrastructure;

/// <summary>
/// Seeds deterministic development data aligned with the current parent-direct
/// business flow:
/// School -> ClassGroup -> Teacher -> Student -> Parent -> SemesterPublication
/// -> Provider -> Order
///
/// Default password for seeded users: Test@1234
/// </summary>
public static class DbInitializer
{
    private const string DefaultPassword = "Test@1234";
    private const string TinySignature =
        "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

    private static readonly Guid RoleAdmin = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid RoleParent = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid RoleSchool = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid RoleProvider = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid RoleTeacher = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid AdminUserId = Guid.Parse("AAAAAAAA-1111-1111-1111-111111111111");

    public static async Task SeedAsync(VTOSDbContext db)
    {
        if (await db.Roles.AnyAsync())
            return;

        var now = DateTime.UtcNow;
        var academicYear = GetAcademicYear(now);
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(DefaultPassword, BCrypt.Net.BCrypt.GenerateSalt(12));

        var bundle = BuildSeedBundle(now, academicYear, passwordHash);

        db.Roles.AddRange(BuildRoles(now));
        db.Users.AddRange(bundle.Users);
        db.Schools.AddRange(bundle.Schools);
        db.Providers.AddRange(bundle.Providers);
        db.Wallets.AddRange(bundle.Wallets);
        db.Set<ParentProfile>().AddRange(bundle.ParentProfiles);
        db.SchoolManagers.AddRange(bundle.SchoolManagers);
        db.ProviderManagers.AddRange(bundle.ProviderManagers);
        db.SizeCharts.AddRange(bundle.SizeCharts);
        db.Set<SizeChartDetail>().AddRange(bundle.SizeChartDetails);
        db.Set<SizeChartMeasurement>().AddRange(bundle.SizeChartMeasurements);
        db.Outfits.AddRange(bundle.Outfits);
        db.ProductVariants.AddRange(bundle.ProductVariants);
        db.Set<Contract>().AddRange(bundle.Contracts);
        db.Set<ContractItem>().AddRange(bundle.ContractItems);
        db.ClassGroups.AddRange(bundle.ClassGroups);
        db.ChildProfiles.AddRange(bundle.ChildProfiles);
        db.Set<StudentDataImport>().AddRange(bundle.StudentDataImports);
        db.Set<SemesterPublication>().AddRange(bundle.SemesterPublications);
        db.Set<SemesterPublicationOutfit>().AddRange(bundle.SemesterPublicationOutfits);
        db.Set<SemesterPublicationProvider>().AddRange(bundle.SemesterPublicationProviders);
        db.Orders.AddRange(bundle.Orders);
        db.OrderItems.AddRange(bundle.OrderItems);
        db.PaymentTransactions.AddRange(bundle.PaymentTransactions);
        db.Feedbacks.AddRange(bundle.Feedbacks);
        db.Set<TeacherReport>().AddRange(bundle.TeacherReports);

<<<<<<< HEAD
        // ── Roles ──────────────────────────────────────────────────────────────
        db.Roles.AddRange(
            new Role { Id = ROLE_ADMIN,    RoleName = "Admin",    IsSystemRole = true, CreatedAt = now },
            new Role { Id = ROLE_PARENT,   RoleName = "Parent",   IsSystemRole = true, CreatedAt = now },
            new Role { Id = ROLE_SCHOOL,   RoleName = "School",   IsSystemRole = true, CreatedAt = now },
            new Role { Id = ROLE_PROVIDER, RoleName = "Provider", IsSystemRole = true, CreatedAt = now },
            new Role { Id = ROLE_TEACHER,  RoleName = "HomeroomTeacher", IsSystemRole = true, CreatedAt = now }
        );
        await db.SaveChangesAsync();

        // ── Schools (Real Da Nang schools) ──────────────────────────────────────
        db.Schools.AddRange(
            new School
            {
                Id = SCH1, SchoolName = "Trường THPT Phan Châu Trinh", Level = "THPT",
                LogoURL = "https://media.vtos.homes/media/schools/2026/04/05/bea91cc23bd34907bdd15909feb552c3.png",
                ContactInfo = "{\"email\":\"contact@thptphanchautrinh.edu.vn\",\"phone\":\"0236 3822 367\",\"address\":\"154 Lê Lợi, Hải Châu, Đà Nẵng\",\"foundedYear\":1952}",
                CreatedAt = now
            },
            new School
            {
                Id = SCH2, SchoolName = "Trường THPT Trần Phú", Level = "THPT",
                LogoURL = "https://media.vtos.homes/media/schools/2026/03/30/91e5dc3478584f6e8591f285963320ee.jpg",
                ContactInfo = "{\"email\":\"contact@thpttranphu.edu.vn\",\"phone\":\"0236 3895 289\",\"address\":\"11 Lê Thánh Tôn, Hải Châu, Đà Nẵng\",\"foundedYear\":1965}",
                CreatedAt = now
            },
            new School
            {
                Id = SCH3, SchoolName = "Trường THCS Nguyễn Huệ", Level = "THCS",
                LogoURL = "https://media.vtos.homes/media/schools/2026/04/05/fd2c3f02fafd4ff1b6293505e6f84ccf.jpg",
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
            new Outfit { Id = OFT1, SchoolID = SCH1, OutfitName = "Áo sơ mi trắng THPT Phan Châu Trinh", Description = "Áo sơ mi trắng dài tay, logo trường thêu ngực trái", MainImageURL = "https://media.vtos.homes/media/outfits/2026/04/06/174a00210048433dbc14f528d7b76a33.png", Price = 185_000, OutfitType = OutfitType.Uniform, SizeChartID = SC1, IsAvailable = true, IsCustomizable = false, IsDeleted = false, CreatedAt = now },
            new Outfit { Id = OFT2, SchoolID = SCH2, OutfitName = "Quần tây xanh THPT Trần Phú", Description = "Quần tây xanh đen, vải tốt không nhăn", MainImageURL = "https://media.vtos.homes/media/outfits/2026/04/05/bf4271679702402c9d6abfdf43fb3da3.png", Price = 195_000, OutfitType = OutfitType.Uniform, SizeChartID = SC2, IsAvailable = true, IsCustomizable = false, IsDeleted = false, CreatedAt = now },
            new Outfit { Id = OFT3, SchoolID = SCH3, OutfitName = "Áo thể dục THCS Nguyễn Huệ", Description = "Áo thể dục cổ tròn, vải thun cotton thoáng mát", MainImageURL = "https://media.vtos.homes/media/outfits/2026/04/05/d5dd53e035a64d05bd19c0035f88a70b.png", Price = 120_000, OutfitType = OutfitType.Sportswear, SizeChartID = SC3, IsAvailable = true, IsCustomizable = false, IsDeleted = false, CreatedAt = now },
            new Outfit { Id = OFT4, SchoolID = SCH1, OutfitName = "Áo dài trắng nữ THPT Phan Châu Trinh", Description = "Áo dài trắng truyền thống dành cho nữ sinh", Price = 350_000, OutfitType = OutfitType.Uniform, SizeChartID = SC1, IsAvailable = true, IsCustomizable = false, IsDeleted = false, CreatedAt = now },
            new Outfit { Id = OFT5, SchoolID = SCH2, OutfitName = "Áo khoác đồng phục THPT Trần Phú", Description = "Áo khoác gió đồng phục, logo trường thêu", Price = 280_000, OutfitType = OutfitType.Uniform, SizeChartID = SC2, IsAvailable = true, IsCustomizable = false, IsDeleted = false, CreatedAt = now }
        );
        await db.SaveChangesAsync();

        // ══════════════════════════════════════════════════════════════════════
        // ── CONTRACTS (supplier agreements used before semester publications/orders)
        // ══════════════════════════════════════════════════════════════════════
        db.Set<Contract>().AddRange(
            // CTR1: PCT ↔ Hoàng Gia — Active (fully signed, used for CAM1 & CAM4)
            new Contract
            {
                Id = CTR1, SchoolID = SCH1, ProviderID = PRV1,
                ContractName = "HĐ May đồng phục PCT - Hoàng Gia 2025-2026",
                ContractNumber = $"HĐ-{now.Year}-A1B2C3",
                Status = "Active", CreatedAt = now.AddDays(-60), ApprovedAt = now.AddDays(-55),
                ExpiresAt = now.AddYears(1),
                SchoolSignature = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
                SchoolSignedAt = now.AddDays(-54),
                ProviderSignature = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
                ProviderSignedAt = now.AddDays(-53)
            },
            // CTR2: TP ↔ Sơn Trà — Active (fully signed, used for CAM2)
            new Contract
            {
                Id = CTR2, SchoolID = SCH2, ProviderID = PRV2,
                ContractName = "HĐ May đồng phục TP - Sơn Trà 2025-2026",
                ContractNumber = $"HĐ-{now.Year}-D4E5F6",
                Status = "Active", CreatedAt = now.AddDays(-50), ApprovedAt = now.AddDays(-45),
                ExpiresAt = now.AddYears(1),
                SchoolSignature = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
                SchoolSignedAt = now.AddDays(-44),
                ProviderSignature = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
                ProviderSignedAt = now.AddDays(-43)
            },
            // CTR3: NH ↔ Thanh Khê — PendingSchoolSign (Provider approved, waiting for school to sign)
            new Contract
            {
                Id = CTR3, SchoolID = SCH3, ProviderID = PRV3,
                ContractName = "HĐ May áo thể dục NH - Thanh Khê 2025-2026",
                ContractNumber = $"HĐ-{now.Year}-G7H8I9",
                Status = "PendingSchoolSign", CreatedAt = now.AddDays(-5), ApprovedAt = now.AddDays(-2),
                ExpiresAt = now.AddYears(1)
            },
            // CTR4: PCT ↔ Sơn Trà — Pending (sent, waiting for Provider to approve)
            new Contract
            {
                Id = CTR4, SchoolID = SCH1, ProviderID = PRV2,
                ContractName = "HĐ May áo khoác PCT - Sơn Trà (chờ duyệt)",
                ContractNumber = $"HĐ-{now.Year}-J0K1L2",
                Status = "Pending", CreatedAt = now.AddDays(-3),
                ExpiresAt = now.AddYears(1)
            },
            // CTR5: TP ↔ Hoàng Gia — PendingProviderSign (school signed, waiting for provider1 to sign)
            new Contract
            {
                Id = CTR5, SchoolID = SCH2, ProviderID = PRV1,
                ContractName = "HĐ May đồng phục TP - Hoàng Gia 2025-2026",
                ContractNumber = $"HĐ-{now.Year}-M3N4O5",
                Status = "PendingProviderSign", CreatedAt = now.AddDays(-7), ApprovedAt = now.AddDays(-5),
                ExpiresAt = now.AddYears(1),
                SchoolSignature = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
                SchoolSignedAt = now.AddDays(-4)
            },
            // CTR6: PCT ↔ Hoàng Gia — PendingSchoolSign (provider approved, waiting for school1 to sign)
            new Contract
            {
                Id = CTR6, SchoolID = SCH1, ProviderID = PRV1,
                ContractName = "HĐ May áo dài PCT - Hoàng Gia 2025-2026 (v2)",
                ContractNumber = $"HĐ-{now.Year}-P6Q7R8",
                Status = "PendingSchoolSign", CreatedAt = now.AddDays(-4), ApprovedAt = now.AddDays(-2),
                ExpiresAt = now.AddYears(1)
            }
        );
        await db.SaveChangesAsync();

        // ── ContractItems (sample outfits attached to supplier agreements)
        db.Set<ContractItem>().AddRange(
            // CTR1: PCT ↔ Hoàng Gia — áo sơ mi + áo dài
            new ContractItem { Id = Guid.NewGuid(), ContractID = CTR1, OutfitID = OFT1, PricePerUnit = 0, MinQuantity = 0, MaxQuantity = 0 },
            new ContractItem { Id = Guid.NewGuid(), ContractID = CTR1, OutfitID = OFT4, PricePerUnit = 0, MinQuantity = 0, MaxQuantity = 0 },
            // CTR2: TP ↔ Sơn Trà — quần tây + áo khoác
            new ContractItem { Id = Guid.NewGuid(), ContractID = CTR2, OutfitID = OFT2, PricePerUnit = 0, MinQuantity = 0, MaxQuantity = 0 },
            new ContractItem { Id = Guid.NewGuid(), ContractID = CTR2, OutfitID = OFT5, PricePerUnit = 0, MinQuantity = 0, MaxQuantity = 0 },
            // CTR3: NH ↔ Thanh Khê — áo thể dục
            new ContractItem { Id = Guid.NewGuid(), ContractID = CTR3, OutfitID = OFT3, PricePerUnit = 0, MinQuantity = 0, MaxQuantity = 0 },
            // CTR4: Pending — áo khoác from different provider
            new ContractItem { Id = Guid.NewGuid(), ContractID = CTR4, OutfitID = OFT5, PricePerUnit = 0, MinQuantity = 0, MaxQuantity = 0 },
            // CTR5: TP ↔ Hoàng Gia — quần tây (PendingProviderSign — provider1 can sign directly)
            new ContractItem { Id = Guid.NewGuid(), ContractID = CTR5, OutfitID = OFT2, PricePerUnit = 0, MinQuantity = 0, MaxQuantity = 0 },
            // CTR6: PCT ↔ Hoàng Gia — áo dài (PendingSchoolSign — school1 can sign)
            new ContractItem { Id = Guid.NewGuid(), ContractID = CTR6, OutfitID = OFT4, PricePerUnit = 0, MinQuantity = 0, MaxQuantity = 0 }
        );
        await db.SaveChangesAsync();

        // ── Semester Publications (5 sample records across the 3 schools) ───
        db.Set<SemesterPublication>().AddRange(
            new SemesterPublication
            {
                Id = PUB1,
                SchoolID = SCH1,
                Semester = "HK1",
                AcademicYear = "2026-2027",
                StartDate = new DateTime(2026, 8, 1),
                EndDate = new DateTime(2026, 10, 31),
                Status = SemesterPublicationStatus.Active,
                Description = "Đợt công bố chính thức cho đồng phục học kỳ 1.",
                Rules = "Nhà cung cấp đã ký thỏa thuận cung ứng, phụ huynh đặt trực tiếp trên hệ thống.",
                CreatedAt = now.AddDays(-18),
                UpdatedAt = now.AddDays(-6)
            },
            new SemesterPublication
            {
                Id = PUB2,
                SchoolID = SCH2,
                Semester = "HK1",
                AcademicYear = "2026-2027",
                StartDate = new DateTime(2026, 8, 5),
                EndDate = new DateTime(2026, 11, 5),
                Status = SemesterPublicationStatus.Active,
                Description = "Đợt công bố HK1 cho đồng phục chính khóa và áo khoác.",
                Rules = "Ưu tiên nhà cung cấp đã có lịch sử giao hàng tốt.",
                CreatedAt = now.AddDays(-16),
                UpdatedAt = now.AddDays(-5)
            },
            new SemesterPublication
            {
                Id = PUB3,
                SchoolID = SCH3,
                Semester = "HK2",
                AcademicYear = "2025-2026",
                StartDate = new DateTime(2026, 1, 10),
                EndDate = new DateTime(2026, 3, 10),
                Status = SemesterPublicationStatus.Draft,
                Description = "Bản nháp công bố đồng phục thể dục cho học kỳ 2.",
                Rules = "Chỉ mở bán sau khi trường hoàn tất rà soát mẫu và nhà cung cấp.",
                CreatedAt = now.AddDays(-8),
                UpdatedAt = now.AddDays(-2)
            },
            new SemesterPublication
            {
                Id = PUB4,
                SchoolID = SCH1,
                Semester = "HK2",
                AcademicYear = "2025-2026",
                StartDate = new DateTime(2026, 1, 15),
                EndDate = new DateTime(2026, 4, 15),
                Status = SemesterPublicationStatus.Closed,
                Description = "Đợt công bố HK2 đã kết thúc để chuyển sang giai đoạn giao hàng.",
                Rules = "Không nhận thêm đơn mới sau ngày đóng công bố.",
                CreatedAt = now.AddDays(-40),
                UpdatedAt = now.AddDays(-12)
            },
            new SemesterPublication
            {
                Id = PUB5,
                SchoolID = SCH3,
                Semester = "HK1",
                AcademicYear = "2026-2027",
                StartDate = new DateTime(2026, 8, 12),
                EndDate = new DateTime(2026, 10, 20),
                Status = SemesterPublicationStatus.Active,
                Description = "Đợt công bố HK1 cho mẫu thể dục và áo khoác sự kiện.",
                Rules = "Nhà cung cấp chỉ cần xác nhận năng lực cung ứng; giá bán sẽ thiết lập theo từng đợt mở bán.",
                CreatedAt = now.AddDays(-11),
                UpdatedAt = now.AddDays(-4)
            }
        );
        await db.SaveChangesAsync();

        db.Set<SemesterPublicationOutfit>().AddRange(
            new SemesterPublicationOutfit { Id = Guid.NewGuid(), SemesterPublicationID = PUB1, OutfitID = OFT1, Notes = "Mẫu sơ mi chính khóa cho khối 10-12.", CreatedAt = now.AddDays(-18), UpdatedAt = now.AddDays(-18) },
            new SemesterPublicationOutfit { Id = Guid.NewGuid(), SemesterPublicationID = PUB1, OutfitID = OFT4, Notes = "Áo dài nữ dùng cho sự kiện và chào cờ.", CreatedAt = now.AddDays(-18), UpdatedAt = now.AddDays(-18) },
            new SemesterPublicationOutfit { Id = Guid.NewGuid(), SemesterPublicationID = PUB2, OutfitID = OFT2, Notes = "Quần tây đồng phục nam nữ.", CreatedAt = now.AddDays(-16), UpdatedAt = now.AddDays(-16) },
            new SemesterPublicationOutfit { Id = Guid.NewGuid(), SemesterPublicationID = PUB2, OutfitID = OFT5, Notes = "Áo khoác gió sử dụng theo mùa.", CreatedAt = now.AddDays(-16), UpdatedAt = now.AddDays(-16) },
            new SemesterPublicationOutfit { Id = Guid.NewGuid(), SemesterPublicationID = PUB3, OutfitID = OFT3, Notes = "Mẫu thể dục đang chờ xác nhận trước khi mở bán.", CreatedAt = now.AddDays(-8), UpdatedAt = now.AddDays(-8) },
            new SemesterPublicationOutfit { Id = Guid.NewGuid(), SemesterPublicationID = PUB4, OutfitID = OFT1, Notes = "Đợt HK2 đã hoàn tất, giữ lại để kiểm tra lịch sử.", CreatedAt = now.AddDays(-40), UpdatedAt = now.AddDays(-40) },
            new SemesterPublicationOutfit { Id = Guid.NewGuid(), SemesterPublicationID = PUB5, OutfitID = OFT3, Notes = "Mẫu thể dục đã được xác nhận cung ứng cho HK1.", CreatedAt = now.AddDays(-11), UpdatedAt = now.AddDays(-11) },
            new SemesterPublicationOutfit { Id = Guid.NewGuid(), SemesterPublicationID = PUB5, OutfitID = OFT5, Notes = "Áo khoác sự kiện dùng cho hoạt động ngoại khóa đầu năm.", CreatedAt = now.AddDays(-11), UpdatedAt = now.AddDays(-11) }
        );
        await db.SaveChangesAsync();

        db.Set<SemesterPublicationProvider>().AddRange(
            new SemesterPublicationProvider { Id = Guid.NewGuid(), SemesterPublicationID = PUB1, ProviderID = PRV1, ContractID = CTR1, Status = SemPublicationProviderStatus.Active, CreatedAt = now.AddDays(-18), UpdatedAt = now.AddDays(-18) },
            new SemesterPublicationProvider { Id = Guid.NewGuid(), SemesterPublicationID = PUB2, ProviderID = PRV2, ContractID = CTR2, Status = SemPublicationProviderStatus.Active, CreatedAt = now.AddDays(-16), UpdatedAt = now.AddDays(-16) },
            new SemesterPublicationProvider { Id = Guid.NewGuid(), SemesterPublicationID = PUB4, ProviderID = PRV1, ContractID = CTR1, Status = SemPublicationProviderStatus.Active, CreatedAt = now.AddDays(-40), UpdatedAt = now.AddDays(-20) },
            new SemesterPublicationProvider { Id = Guid.NewGuid(), SemesterPublicationID = PUB5, ProviderID = PRV3, ContractID = CTR3, Status = SemPublicationProviderStatus.Active, CreatedAt = now.AddDays(-11), UpdatedAt = now.AddDays(-4) },
            new SemesterPublicationProvider { Id = Guid.NewGuid(), SemesterPublicationID = PUB5, ProviderID = PRV2, ContractID = null, Status = SemPublicationProviderStatus.Suspended, SuspendReason = "Tạm dừng sau vòng đối soát năng lực đầu kỳ.", SuspendedAt = now.AddDays(-6), CreatedAt = now.AddDays(-10), UpdatedAt = now.AddDays(-6) }
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
            new ProductionBatch { Id = BATCH3, CampaignID = CAM3, ProviderID = PRV3, BatchName = "Lô SX NH - Áo thể dục", TotalQuantity = 80, CreatedDate = now.AddDays(-10), Status = ProductionBatchStatus.InProduction, DeliveryDeadline = new DateTime(2026,3,20), IsDeleted = false, ProcessedAt = now.AddDays(-7) },
            new ProductionBatch { Id = BATCH4, CampaignID = CAM4, ProviderID = PRV1, BatchName = "Lô SX PCT - Áo dài sự kiện", TotalQuantity = 60, CreatedDate = now.AddDays(-22), Status = ProductionBatchStatus.Completed, DeliveryDeadline = new DateTime(2026,4,5), IsDeleted = false, ProcessedAt = now.AddDays(-18), DeliveredQuantity = 60, DeliveryNote = "Đã hoàn tất sản xuất, chờ bàn giao đợt cuối." },
            new ProductionBatch { Id = BATCH5, CampaignID = CAM2, ProviderID = PRV2, BatchName = "Lô SX TP - Áo khoác đợt 2", TotalQuantity = 90, CreatedDate = now.AddDays(-26), Status = ProductionBatchStatus.Delivered, DeliveryDeadline = new DateTime(2026,3,28), IsDeleted = false, ProcessedAt = now.AddDays(-21), DeliveredQuantity = 90, DeliveryConfirmedAt = now.AddDays(-8), DeliveryNote = "Đã giao đủ và trường xác nhận hoàn tất." }
        );
        await db.SaveChangesAsync();

        // ── ProductionBatchItems ──────────────────────────────────────────────
        db.ProductionBatchItems.AddRange(
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH1, OutfitID = OFT1, Size = "S", Quantity = 50, UnitPrice = 125_000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH1, OutfitID = OFT1, Size = "M", Quantity = 65, UnitPrice = 125_000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH1, OutfitID = OFT1, Size = "L", Quantity = 35, UnitPrice = 125_000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH2, OutfitID = OFT2, Size = "M", Quantity = 55, UnitPrice = 130_000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH2, OutfitID = OFT2, Size = "L", Quantity = 45, UnitPrice = 130_000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH3, OutfitID = OFT3, Size = "M", Quantity = 80, UnitPrice = 75_000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH4, OutfitID = OFT4, Size = "M", Quantity = 30, UnitPrice = 245_000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH4, OutfitID = OFT4, Size = "L", Quantity = 30, UnitPrice = 245_000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH5, OutfitID = OFT5, Size = "M", Quantity = 40, UnitPrice = 165_000 },
            new ProductionBatchItem { Id = Guid.NewGuid(), BatchID = BATCH5, OutfitID = OFT5, Size = "L", Quantity = 50, UnitPrice = 165_000 }
        );
        await db.SaveChangesAsync();

        // ── Complaints ────────────────────────────────────────────────────────
        db.SupportTickets.AddRange(
            new SupportTicket { Id = Guid.NewGuid(), CampaignID = CAM1, BatchID = BATCH1, SchoolID = SCH1, ProviderID = PRV1, Title = "Áo sơ mi bị phai màu sau khi giặt", Description = "Một số áo lô đầu bị phai vàng sau 2 lần giặt máy", Status = SupportTicketStatus.Open, CreatedAt = now.AddDays(-3) },
            new SupportTicket { Id = Guid.NewGuid(), CampaignID = CAM1, BatchID = BATCH1, SchoolID = SCH1, ProviderID = PRV1, Title = "Thiếu hàng size M 3 sản phẩm", Description = "Đơn giao 65 cái size M nhưng chỉ nhận 62, thiếu 3", Status = SupportTicketStatus.InProgress, CreatedAt = now.AddDays(-5) },
            new SupportTicket { Id = Guid.NewGuid(), CampaignID = CAM2, BatchID = BATCH2, SchoolID = SCH2, ProviderID = PRV2, Title = "Giao hàng trễ hạn 10 ngày", Description = "Hạn giao 01/03 nhưng 11/03 vẫn chưa nhận được hàng", Status = SupportTicketStatus.Resolved, CreatedAt = now.AddDays(-12) },
            new SupportTicket { Id = Guid.NewGuid(), CampaignID = CAM3, BatchID = BATCH3, SchoolID = SCH3, ProviderID = PRV3, Title = "Đường may áo thể dục chưa đều", Description = "Một số áo ở lô đầu có đường may lệch tại cổ áo, cần rà soát lại toàn bộ lô.", Status = SupportTicketStatus.Open, CreatedAt = now.AddDays(-6) },
            new SupportTicket { Id = Guid.NewGuid(), CampaignID = CAM2, BatchID = BATCH2, SchoolID = SCH2, ProviderID = PRV2, Title = "Cần đối soát biên bản giao nhận", Description = "Trường cần đối chiếu lại số lượng đã giao trước khi khóa đợt phát đồng phục.", Status = SupportTicketStatus.InProgress, CreatedAt = now.AddDays(-9) }
        );
=======
>>>>>>> 2fc982e (refactor(be): align auth admin support and infrastructure updates)
        await db.SaveChangesAsync();
    }

    private static IEnumerable<Role> BuildRoles(DateTime now)
    {
        return
        [
            new Role
            {
                Id = RoleAdmin,
                RoleName = "Admin",
                Description = "System administrator.",
                IsSystemRole = true,
                CreatedAt = now
            },
            new Role
            {
                Id = RoleParent,
                RoleName = "Parent",
                Description = "Parent user linked to student records.",
                IsSystemRole = true,
                CreatedAt = now
            },
            new Role
            {
                Id = RoleSchool,
                RoleName = "School",
                Description = "School manager account.",
                IsSystemRole = true,
                CreatedAt = now
            },
            new Role
            {
                Id = RoleProvider,
                RoleName = "Provider",
                Description = "Uniform provider account.",
                IsSystemRole = true,
                CreatedAt = now
            },
            new Role
            {
                Id = RoleTeacher,
                RoleName = "HomeroomTeacher",
                Description = "Homeroom teacher account created for seeded classes.",
                IsSystemRole = true,
                CreatedAt = now
            }
        ];
    }

    private static SeedBundle BuildSeedBundle(DateTime now, string academicYear, string passwordHash)
    {
        var bundle = new SeedBundle();

        bundle.Users.Add(new User
        {
            Id = AdminUserId,
            FullName = "Nguyễn Hoàng Quản Trị",
            Email = "admin@vtos.com",
            PasswordHash = passwordHash,
            Phone = "0905000099",
            Avatar = "avatar.jpg",
            RoleID = RoleAdmin,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = now
        });

        var providerContexts = CreateProviders(bundle, now, passwordHash);
        var schoolContexts = CreateSchools(bundle, providerContexts, now, academicYear, passwordHash);

        CreateClassroomsAndTeachers(bundle, schoolContexts, now, academicYear, passwordHash);
        CreateStudentsAndParents(bundle, schoolContexts, now, academicYear, passwordHash);
        CreateTeacherReports(bundle, schoolContexts, now);
        CreateOrdersPaymentsAndFeedback(bundle, schoolContexts, now);
        UpdateWalletBalances(bundle);

        return bundle;
    }

    private static List<ProviderContext> CreateProviders(SeedBundle bundle, DateTime now, string passwordHash)
    {
        var blueprints = new[]
        {
            new ProviderBlueprint(
                "provider-hoanggia",
                "Công ty May Mặc Hoàng Gia",
                "Nguyễn Minh Tuấn",
                "provider1@vtos.com",
                "0905123456",
                "Lô C2 KCN Hòa Khánh, Liên Chiểu, Đà Nẵng",
                "0401987654",
                "Giám đốc điều hành",
                "VCB",
                "Vietcombank",
                "0491000234567",
                "CONG TY MAY MAC HOANG GIA"),
            new ProviderBlueprint(
                "provider-sontra",
                "Đồng Phục Sơn Trà",
                "Võ Thị Lan Anh",
                "provider2@vtos.com",
                "0935789012",
                "78 Ngô Quyền, Sơn Trà, Đà Nẵng",
                "0402456789",
                "Giám đốc kinh doanh",
                "TCB",
                "Techcombank",
                "19035678905678",
                "DONG PHUC SON TRA"),
            new ProviderBlueprint(
                "provider-lienchieu",
                "Xưởng May Liên Chiểu",
                "Phạm Đức Long",
                "provider3@vtos.com",
                "0769456789",
                "152 Nguyễn Lương Bằng, Liên Chiểu, Đà Nẵng",
                "0403123456",
                "Quản lý xưởng",
                "BIDV",
                "BIDV",
                "31410001234567",
                "XUONG MAY LIEN CHIEU"),
        };

        var contexts = new List<ProviderContext>();

        foreach (var blueprint in blueprints)
        {
            var providerId = StableGuid($"{blueprint.Key}:provider");
            var managerId = StableGuid($"{blueprint.Key}:manager");
            var walletId = StableGuid($"{blueprint.Key}:wallet");

            var provider = new Provider
            {
                Id = providerId,
                ProviderName = blueprint.ProviderName,
                ContactPersonName = blueprint.ContactPersonName,
                Phone = blueprint.Phone,
                Email = blueprint.Email,
                Address = blueprint.Address,
                TaxCode = blueprint.TaxCode,
                RepresentativeTitle = blueprint.RepresentativeTitle,
                Status = ProviderStatus.Active,
                VerificationStatus = VerificationStatus.Approved,
                VerificationDocumentUrl = $"https://media.vtos.homes/seeds/providers/{blueprint.Key}/verify.pdf",
                IsDeleted = false
            };

            var manager = new User
            {
                Id = managerId,
                FullName = blueprint.ContactPersonName,
                Email = blueprint.Email,
                PasswordHash = passwordHash,
                Phone = blueprint.Phone,
                Avatar = "avatar.jpg",
                RoleID = RoleProvider,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now
            };

            var wallet = new Wallet
            {
                Id = walletId,
                OwnerID = providerId,
                OwnerType = WalletOwnerType.Provider,
                Balance = 0m,
                BankCode = blueprint.BankCode,
                BankName = blueprint.BankName,
                BankAccountNumber = blueprint.BankAccountNumber,
                BankAccountName = blueprint.BankAccountName,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            bundle.Providers.Add(provider);
            bundle.Users.Add(manager);
            bundle.Wallets.Add(wallet);
            bundle.ProviderManagers.Add(new ProviderManager
            {
                Id = StableGuid($"{blueprint.Key}:provider-manager"),
                UserID = managerId,
                ProviderID = providerId
            });

            contexts.Add(new ProviderContext
            {
                Blueprint = blueprint,
                ProviderId = providerId,
                ManagerUserId = managerId,
                WalletId = walletId
            });
        }

        return contexts;
    }

    private static List<SchoolContext> CreateSchools(
        SeedBundle bundle,
        IReadOnlyList<ProviderContext> providerContexts,
        DateTime now,
        string academicYear,
        string passwordHash)
    {
        var blueprints = new[]
        {
            new SchoolBlueprint(
                "school-thpt",
                "Trường THPT Phan Châu Trinh",
                "THPT",
                "154 Lê Lợi, Hải Châu, Đà Nẵng",
                "school1@vtos.com",
                "02363822367",
                "Nguyễn Thị Thanh Hà",
                "Hiệu trưởng",
                10,
                12,
                8,
                18,
                24),
            new SchoolBlueprint(
                "school-thcs",
                "Trường THCS Nguyễn Huệ",
                "THCS",
                "62 Nguyễn Chí Thanh, Hải Châu, Đà Nẵng",
                "school2@vtos.com",
                "02363823456",
                "Trần Văn Minh",
                "Hiệu trưởng",
                6,
                9,
                4,
                16,
                22),
            new SchoolBlueprint(
                "school-tieuhoc",
                "Trường Tiểu học Lê Quý Đôn",
                "Tiểu học",
                "85 Hải Phòng, Thanh Khê, Đà Nẵng",
                "school3@vtos.com",
                "02363778999",
                "Lê Thị Bích Ngọc",
                "Hiệu trưởng",
                1,
                5,
                4,
                14,
                20),
        };

        var contexts = new List<SchoolContext>();

        for (var index = 0; index < blueprints.Length; index++)
        {
            var blueprint = blueprints[index];
            var schoolId = StableGuid($"{blueprint.Key}:school");
            var managerId = StableGuid($"{blueprint.Key}:manager");
            var walletId = StableGuid($"{blueprint.Key}:wallet");
            var sizeChartId = StableGuid($"{blueprint.Key}:sizechart");

            var school = new School
            {
                Id = schoolId,
                SchoolName = blueprint.SchoolName,
                Level = blueprint.Level,
                LogoURL = $"https://media.vtos.homes/seeds/schools/{blueprint.Key}/logo.png",
                ContactInfo = $"{{\"email\":\"{blueprint.Email}\",\"phone\":\"{blueprint.Phone}\",\"address\":\"{blueprint.Address}\"}}",
                Address = blueprint.Address,
                Phone = blueprint.Phone,
                RepresentativeName = blueprint.RepresentativeName,
                RepresentativeTitle = blueprint.RepresentativeTitle,
                TaxCode = GenerateTaxCode($"{blueprint.Key}:tax"),
                Status = SchoolStatus.Active,
                VerificationStatus = VerificationStatus.Approved,
                VerificationDocumentUrl = $"https://media.vtos.homes/seeds/schools/{blueprint.Key}/verify.pdf",
                IsDeleted = false,
                CreatedAt = now
            };

            var manager = new User
            {
                Id = managerId,
                FullName = blueprint.RepresentativeName,
                Email = blueprint.Email,
                PasswordHash = passwordHash,
                Phone = NormalizePhone(blueprint.Phone),
                Avatar = "avatar.jpg",
                RoleID = RoleSchool,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now
            };

            var wallet = new Wallet
            {
                Id = walletId,
                OwnerID = schoolId,
                OwnerType = WalletOwnerType.School,
                Balance = 0m,
                BankCode = index switch { 0 => "VCB", 1 => "TCB", _ => "BIDV" },
                BankName = index switch { 0 => "Vietcombank", 1 => "Techcombank", _ => "BIDV" },
                BankAccountNumber = index switch
                {
                    0 => "0491000111222",
                    1 => "19035678123456",
                    _ => "31410007654321"
                },
                BankAccountName = RemoveDiacritics(blueprint.SchoolName).ToUpperInvariant(),
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            var sizeChart = new SizeChart
            {
                Id = sizeChartId,
                ChartName = $"Bảng size {blueprint.SchoolName}",
                Description = $"Bảng size chuẩn cho {blueprint.Level.ToLowerInvariant()} dùng trong luồng đặt may trực tiếp.",
                Unit = "cm",
                CreatedAt = now
            };

            bundle.Schools.Add(school);
            bundle.Users.Add(manager);
            bundle.Wallets.Add(wallet);
            bundle.SizeCharts.Add(sizeChart);
            bundle.SchoolManagers.Add(new SchoolManager
            {
                Id = StableGuid($"{blueprint.Key}:school-manager"),
                UserID = managerId,
                SchoolID = schoolId
            });

            CreateSizeChartDetails(bundle, blueprint, sizeChartId, now);

            var primaryProvider = providerContexts[index % providerContexts.Count];
            var secondaryProvider = providerContexts[(index + 1) % providerContexts.Count];

            var context = new SchoolContext
            {
                Blueprint = blueprint,
                SchoolId = schoolId,
                ManagerUserId = managerId,
                WalletId = walletId,
                SizeChartId = sizeChartId,
                PrimaryProvider = primaryProvider,
                SecondaryProvider = secondaryProvider
            };

            CreateOutfitsForSchool(bundle, context, now);
            CreateContractsForSchool(bundle, context, now);
            CreateSemesterPublications(bundle, context, now, academicYear);

            contexts.Add(context);
        }

        return contexts;
    }

    private static void CreateSizeChartDetails(SeedBundle bundle, SchoolBlueprint blueprint, Guid sizeChartId, DateTime now)
    {
        var sizes = blueprint.Level == "Tiểu học"
            ? new[] { "XS", "S", "M", "L" }
            : new[] { "S", "M", "L", "XL" };

        for (var index = 0; index < sizes.Length; index++)
        {
            var detailId = StableGuid($"{sizeChartId}:size:{sizes[index]}");
            bundle.SizeChartDetails.Add(new SizeChartDetail
            {
                Id = detailId,
                SizeChartID = sizeChartId,
                SizeLabel = sizes[index],
                CreatedAt = now
            });

            var heightMin = blueprint.Level == "Tiểu học" ? 110 + (index * 8) : 145 + (index * 6);
            var heightMax = heightMin + (blueprint.Level == "Tiểu học" ? 10 : 7);
            var chestMin = blueprint.Level == "Tiểu học" ? 54 + (index * 6) : 74 + (index * 5);
            var chestMax = chestMin + 6;
            var waistMin = blueprint.Level == "Tiểu học" ? 50 + (index * 5) : 62 + (index * 4);
            var waistMax = waistMin + 6;

            bundle.SizeChartMeasurements.AddRange(new[]
            {
                new SizeChartMeasurement
                {
                    Id = StableGuid($"{detailId}:height"),
                    SizeChartDetailId = detailId,
                    FieldKey = "height",
                    DisplayName = "Chiều cao",
                    Unit = "cm",
                    MinCm = heightMin,
                    MaxCm = heightMax,
                    CreatedAt = now
                },
                new SizeChartMeasurement
                {
                    Id = StableGuid($"{detailId}:chest"),
                    SizeChartDetailId = detailId,
                    FieldKey = "chest",
                    DisplayName = "Vòng ngực",
                    Unit = "cm",
                    MinCm = chestMin,
                    MaxCm = chestMax,
                    CreatedAt = now
                },
                new SizeChartMeasurement
                {
                    Id = StableGuid($"{detailId}:waist"),
                    SizeChartDetailId = detailId,
                    FieldKey = "waist",
                    DisplayName = "Vòng eo",
                    Unit = "cm",
                    MinCm = waistMin,
                    MaxCm = waistMax,
                    CreatedAt = now
                }
            });
        }
    }

    private static void CreateOutfitsForSchool(SeedBundle bundle, SchoolContext context, DateTime now)
    {
        var outfitBlueprints = GetOutfitBlueprints(context.Blueprint);

        for (var index = 0; index < outfitBlueprints.Length; index++)
        {
            var blueprint = outfitBlueprints[index];
            var provider = index < 2 ? context.PrimaryProvider : context.SecondaryProvider;
            var outfitId = StableGuid($"{context.Blueprint.Key}:outfit:{blueprint.Code}");

            var outfit = new Outfit
            {
                Id = outfitId,
                SchoolID = context.SchoolId,
                OutfitName = blueprint.Name,
                Description = blueprint.Description,
                MaterialType = blueprint.MaterialType,
                Price = blueprint.BasePrice,
                OutfitType = blueprint.OutfitType,
                MainImageURL = $"https://media.vtos.homes/seeds/outfits/{context.Blueprint.Key}/{blueprint.Code}.png",
                SizeChartID = context.SizeChartId,
                IsAvailable = true,
                IsCustomizable = false,
                IsDeleted = false,
                CreatedAt = now
            };

            bundle.Outfits.Add(outfit);

            var sizes = context.Blueprint.Level == "Tiểu học"
                ? new[] { "XS", "S", "M", "L" }
                : new[] { "S", "M", "L", "XL" };

            for (var sizeIndex = 0; sizeIndex < sizes.Length; sizeIndex++)
            {
                bundle.ProductVariants.Add(new ProductVariant
                {
                    Id = StableGuid($"{outfitId}:variant:{sizes[sizeIndex]}"),
                    OutfitID = outfitId,
                    Size = sizes[sizeIndex],
                    StockQuantity = 200 + (sizeIndex * 25),
                    Price = blueprint.BasePrice + (sizeIndex == sizes.Length - 1 ? 10000 : 0),
                    SKUCode = $"{context.Blueprint.Key.ToUpperInvariant()}-{blueprint.Code.ToUpperInvariant()}-{sizes[sizeIndex]}",
                    ColorVariant = blueprint.Color,
                    MaterialType = blueprint.MaterialType,
                    VariantImageURL = outfit.MainImageURL,
                    IsDeleted = false
                });
            }

            context.Outfits.Add(new OutfitContext
            {
                OutfitId = outfitId,
                ProviderId = provider.ProviderId,
                OutfitName = blueprint.Name,
                BasePrice = blueprint.BasePrice
            });
        }
    }

    private static void CreateContractsForSchool(SeedBundle bundle, SchoolContext context, DateTime now)
    {
        var groupedOutfits = context.Outfits.GroupBy(x => x.ProviderId).ToDictionary(g => g.Key, g => g.ToList());
        var activePrimaryId = StableGuid($"{context.Blueprint.Key}:contract:primary");
        var activeSecondaryId = StableGuid($"{context.Blueprint.Key}:contract:secondary");
        var pendingId = StableGuid($"{context.Blueprint.Key}:contract:pending");

        var activePrimary = new Contract
        {
            Id = activePrimaryId,
            SchoolID = context.SchoolId,
            ProviderID = context.PrimaryProvider.ProviderId,
            ContractName = $"HĐ cung ứng {context.Blueprint.Level} - {context.Blueprint.SchoolName} / {context.PrimaryProvider.Blueprint.ProviderName}",
            ContractNumber = $"HD-{GetCompactCode(context.Blueprint.Key)}-P1",
            Status = "Active",
            CreatedAt = now.AddDays(-90),
            ApprovedAt = now.AddDays(-82),
            ExpiresAt = now.AddMonths(12),
            SchoolSignature = TinySignature,
            ProviderSignature = TinySignature,
            SchoolSignedAt = now.AddDays(-81),
            ProviderSignedAt = now.AddDays(-80),
            ContractPdfUrl = $"/contracts/{activePrimaryId}.pdf"
        };

        var activeSecondary = new Contract
        {
            Id = activeSecondaryId,
            SchoolID = context.SchoolId,
            ProviderID = context.SecondaryProvider.ProviderId,
            ContractName = $"HĐ thể thao & phụ kiện - {context.Blueprint.SchoolName} / {context.SecondaryProvider.Blueprint.ProviderName}",
            ContractNumber = $"HD-{GetCompactCode(context.Blueprint.Key)}-P2",
            Status = "InUse",
            CreatedAt = now.AddDays(-70),
            ApprovedAt = now.AddDays(-62),
            ExpiresAt = now.AddMonths(10),
            SchoolSignature = TinySignature,
            ProviderSignature = TinySignature,
            SchoolSignedAt = now.AddDays(-61),
            ProviderSignedAt = now.AddDays(-60),
            ContractPdfUrl = $"/contracts/{activeSecondaryId}.pdf"
        };

        var pending = new Contract
        {
            Id = pendingId,
            SchoolID = context.SchoolId,
            ProviderID = context.SecondaryProvider.ProviderId,
            ContractName = $"HĐ mở rộng danh mục phụ trợ - {context.Blueprint.SchoolName}",
            ContractNumber = $"HD-{GetCompactCode(context.Blueprint.Key)}-P3",
            Status = "PendingProviderSign",
            CreatedAt = now.AddDays(-14),
            ApprovedAt = now.AddDays(-9),
            ExpiresAt = now.AddMonths(8),
            SchoolSignature = TinySignature,
            SchoolSignedAt = now.AddDays(-8)
        };

        bundle.Contracts.AddRange([activePrimary, activeSecondary, pending]);

        context.ActivePrimaryContractId = activePrimaryId;
        context.ActiveSecondaryContractId = activeSecondaryId;

        foreach (var outfit in groupedOutfits[context.PrimaryProvider.ProviderId])
        {
            var contractPrice = Math.Round(outfit.BasePrice * 0.68m, 0);
            context.ProviderCostByOutfitId[outfit.OutfitId] = contractPrice;
            bundle.ContractItems.Add(new ContractItem
            {
                Id = StableGuid($"{activePrimaryId}:item:{outfit.OutfitId}"),
                ContractID = activePrimaryId,
                OutfitID = outfit.OutfitId,
                PricePerUnit = contractPrice,
                MinQuantity = 80,
                MaxQuantity = 1200
            });
        }

        foreach (var outfit in groupedOutfits[context.SecondaryProvider.ProviderId])
        {
            var contractPrice = Math.Round(outfit.BasePrice * 0.7m, 0);
            context.ProviderCostByOutfitId[outfit.OutfitId] = contractPrice;
            bundle.ContractItems.Add(new ContractItem
            {
                Id = StableGuid($"{activeSecondaryId}:item:{outfit.OutfitId}"),
                ContractID = activeSecondaryId,
                OutfitID = outfit.OutfitId,
                PricePerUnit = contractPrice,
                MinQuantity = 60,
                MaxQuantity = 800
            });

            bundle.ContractItems.Add(new ContractItem
            {
                Id = StableGuid($"{pendingId}:item:{outfit.OutfitId}"),
                ContractID = pendingId,
                OutfitID = outfit.OutfitId,
                PricePerUnit = Math.Round(outfit.BasePrice * 0.73m, 0),
                MinQuantity = 40,
                MaxQuantity = 500
            });
        }
    }

    private static void CreateSemesterPublications(SeedBundle bundle, SchoolContext context, DateTime now, string academicYear)
    {
        var closedId = StableGuid($"{context.Blueprint.Key}:publication:closed");
        var activeId = StableGuid($"{context.Blueprint.Key}:publication:active");
        var draftId = StableGuid($"{context.Blueprint.Key}:publication:draft");

        var closed = new SemesterPublication
        {
            Id = closedId,
            SchoolID = context.SchoolId,
            Semester = "HK1",
            AcademicYear = academicYear,
            StartDate = new DateTime(now.Year - 1, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(now.Year, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            Status = SemesterPublicationStatus.Closed,
            Description = $"Đợt chốt danh mục học kỳ 1 cho {context.Blueprint.SchoolName}.",
            Rules = "Danh mục đã chốt, chỉ dùng để tra cứu lịch sử đơn hàng.",
            CreatedAt = now.AddDays(-180),
            UpdatedAt = now.AddDays(-110)
        };

        var active = new SemesterPublication
        {
            Id = activeId,
            SchoolID = context.SchoolId,
            Semester = "HK2",
            AcademicYear = academicYear,
            StartDate = now.AddDays(-35),
            EndDate = now.AddDays(40),
            Status = SemesterPublicationStatus.Active,
            Description = $"Danh mục đặt trực tiếp đang mở cho phụ huynh của {context.Blueprint.SchoolName}.",
            Rules = "Phụ huynh đặt trực tiếp theo lớp, giáo viên theo dõi tiến độ phủ đơn.",
            CreatedAt = now.AddDays(-45),
            UpdatedAt = now.AddDays(-10)
        };

        var draft = new SemesterPublication
        {
            Id = draftId,
            SchoolID = context.SchoolId,
            Semester = "Hè",
            AcademicYear = academicYear,
            StartDate = now.AddDays(55),
            EndDate = now.AddDays(95),
            Status = SemesterPublicationStatus.Draft,
            Description = $"Bản nháp danh mục hè cho {context.Blueprint.SchoolName}.",
            Rules = "Chưa phát hành. Chờ trường hoàn tất chọn nhà cung cấp.",
            CreatedAt = now.AddDays(-6),
            UpdatedAt = now.AddDays(-2)
        };

        bundle.SemesterPublications.AddRange([closed, active, draft]);

        context.ClosedPublicationId = closedId;
        context.ActivePublicationId = activeId;
        context.DraftPublicationId = draftId;

        foreach (var outfit in context.Outfits)
        {
            bundle.SemesterPublicationOutfits.Add(new SemesterPublicationOutfit
            {
                Id = StableGuid($"{activeId}:outfit:{outfit.OutfitId}"),
                SemesterPublicationID = activeId,
                OutfitID = outfit.OutfitId,
                Notes = "Danh mục chính đang bán.",
                CreatedAt = now.AddDays(-20)
            });

            if (outfit.ProviderId == context.PrimaryProvider.ProviderId)
            {
                bundle.SemesterPublicationOutfits.Add(new SemesterPublicationOutfit
                {
                    Id = StableGuid($"{closedId}:outfit:{outfit.OutfitId}"),
                    SemesterPublicationID = closedId,
                    OutfitID = outfit.OutfitId,
                    Notes = "Dùng cho danh mục lịch sử học kỳ 1.",
                    CreatedAt = now.AddDays(-160)
                });
            }

            if (outfit.ProviderId == context.PrimaryProvider.ProviderId)
            {
                bundle.SemesterPublicationOutfits.Add(new SemesterPublicationOutfit
                {
                    Id = StableGuid($"{draftId}:outfit:{outfit.OutfitId}"),
                    SemesterPublicationID = draftId,
                    OutfitID = outfit.OutfitId,
                    Notes = "Ứng viên cho danh mục hè.",
                    CreatedAt = now.AddDays(-3)
                });
            }
        }

        bundle.SemesterPublicationProviders.AddRange(new[]
        {
            new SemesterPublicationProvider
            {
                Id = StableGuid($"{activeId}:provider:{context.PrimaryProvider.ProviderId}"),
                SemesterPublicationID = activeId,
                ProviderID = context.PrimaryProvider.ProviderId,
                ContractID = context.ActivePrimaryContractId,
                Status = SemPublicationProviderStatus.Active,
                CreatedAt = now.AddDays(-25)
            },
            new SemesterPublicationProvider
            {
                Id = StableGuid($"{activeId}:provider:{context.SecondaryProvider.ProviderId}"),
                SemesterPublicationID = activeId,
                ProviderID = context.SecondaryProvider.ProviderId,
                ContractID = context.ActiveSecondaryContractId,
                Status = SemPublicationProviderStatus.Active,
                CreatedAt = now.AddDays(-25)
            },
            new SemesterPublicationProvider
            {
                Id = StableGuid($"{closedId}:provider:{context.PrimaryProvider.ProviderId}"),
                SemesterPublicationID = closedId,
                ProviderID = context.PrimaryProvider.ProviderId,
                ContractID = context.ActivePrimaryContractId,
                Status = SemPublicationProviderStatus.Active,
                CreatedAt = now.AddDays(-170)
            },
            new SemesterPublicationProvider
            {
                Id = StableGuid($"{draftId}:provider:{context.PrimaryProvider.ProviderId}"),
                SemesterPublicationID = draftId,
                ProviderID = context.PrimaryProvider.ProviderId,
                ContractID = context.ActivePrimaryContractId,
                Status = SemPublicationProviderStatus.Active,
                CreatedAt = now.AddDays(-4)
            },
            new SemesterPublicationProvider
            {
                Id = StableGuid($"{draftId}:provider:{context.SecondaryProvider.ProviderId}"),
                SemesterPublicationID = draftId,
                ProviderID = context.SecondaryProvider.ProviderId,
                ContractID = context.ActiveSecondaryContractId,
                Status = SemPublicationProviderStatus.Suspended,
                SuspendedAt = now.AddDays(-1),
                SuspendReason = "Chưa chốt giá danh mục hè.",
                CreatedAt = now.AddDays(-4)
            }
        });
    }
    private static void CreateClassroomsAndTeachers(
        SeedBundle bundle,
        IReadOnlyList<SchoolContext> schoolContexts,
        DateTime now,
        string academicYear,
        string passwordHash)
    {
        foreach (var school in schoolContexts)
        {
            var classOrdinal = 0;
            for (var grade = school.Blueprint.StartGrade; grade <= school.Blueprint.EndGrade; grade++)
            {
                for (var section = 1; section <= school.Blueprint.ClassesPerGrade; section++)
                {
                    var classCode = $"{grade}A{section}";
                    var teacherId = StableGuid($"{school.Blueprint.Key}:teacher:{classCode}");
                    var classId = StableGuid($"{school.Blueprint.Key}:class:{classCode}");
                    var scenario = ResolveScenario(classOrdinal);
                    classOrdinal++;

                    var teacherName = BuildAdultName($"{school.Blueprint.Key}:teacher:{classCode}", true);
                    var teacherEmail = $"teacher.{school.Blueprint.Key}.{grade}a{section}@vtos.com";

                    bundle.Users.Add(new User
                    {
                        Id = teacherId,
                        FullName = teacherName,
                        Email = teacherEmail,
                        PasswordHash = passwordHash,
                        Phone = GeneratePhone($"{school.Blueprint.Key}:teacher:{classCode}", "091"),
                        Avatar = "avatar.jpg",
                        RoleID = RoleTeacher,
                        IsActive = true,
                        IsDeleted = false,
                        CreatedAt = now
                    });

                    bundle.ClassGroups.Add(new ClassGroup
                    {
                        Id = classId,
                        SchoolID = school.SchoolId,
                        ClassName = classCode,
                        Grade = grade.ToString(),
                        AcademicYear = academicYear,
                        HomeroomTeacherID = teacherId,
                        CreatedAt = now.AddDays(-35)
                    });

                    school.Classes.Add(new ClassContext
                    {
                        ClassId = classId,
                        ClassName = classCode,
                        Grade = grade,
                        TeacherUserId = teacherId,
                        TeacherName = teacherName,
                        TeacherEmail = teacherEmail,
                        Scenario = scenario
                    });
                }
            }
        }
    }

    private static void CreateStudentsAndParents(
        SeedBundle bundle,
        IReadOnlyList<SchoolContext> schoolContexts,
        DateTime now,
        string academicYear,
        string passwordHash)
    {
        var households = new Dictionary<string, ParentContext>();
        var parentSequence = 0;

        foreach (var school in schoolContexts)
        {
            foreach (var classContext in school.Classes)
            {
                var studentCount = StableInt(
                    $"{school.Blueprint.Key}:{classContext.ClassName}:count",
                    school.Blueprint.MinStudents,
                    school.Blueprint.MaxStudents);

                var householdKeysInClass = new List<string>();

                for (var studentIndex = 1; studentIndex <= studentCount; studentIndex++)
                {
                    var studentKey = $"{school.Blueprint.Key}:{classContext.ClassName}:student:{studentIndex}";
                    var isMale = StableInt($"{studentKey}:gender", 0, 99) % 2 == 0;
                    var shouldLinkParent = StableInt($"{studentKey}:link", 0, 99) < GetLinkThreshold(classContext.Scenario);
                    var hasMeasurements = StableInt($"{studentKey}:measurement", 0, 99) < GetMeasurementThreshold(classContext.Scenario);

                    var childId = StableGuid($"{studentKey}:child");
                    var birthMonth = StableInt($"{studentKey}:month", 1, 12);
                    var birthDay = StableInt($"{studentKey}:day", 1, DateTime.DaysInMonth(now.Year - (classContext.Grade + 5), birthMonth));
                    var birthYear = now.Year - (classContext.Grade + 5);
                    var dob = new DateTime(birthYear, birthMonth, birthDay, 0, 0, 0, DateTimeKind.Utc);
                    var age = Math.Max(now.Year - birthYear, 6);

                    ParentContext? parent = null;
                    var parentPhone = GeneratePhone($"{studentKey}:phone", "090");

                    if (shouldLinkParent)
                    {
                        var householdKey = BuildHouseholdKey(
                            studentKey,
                            studentIndex,
                            householdKeysInClass,
                            school.Blueprint.Key,
                            classContext.ClassName);

                        householdKeysInClass.Add(householdKey);

                        if (!households.TryGetValue(householdKey, out parent))
                        {
                            parentSequence++;
                            parent = CreateParentContext(householdKey, parentSequence, childId, isMale, passwordHash, now);
                            households[householdKey] = parent;
                            bundle.Users.Add(parent.User);
                            bundle.ParentProfiles.Add(parent.Profile);
                        }

                        parentPhone = parent.User.Phone ?? parentPhone;
                    }

                    var studentName = BuildStudentName(studentKey, isMale);
                    var studentCode = $"{GetCompactCode(school.Blueprint.Key)}-{classContext.ClassName}-{studentIndex:00}";
                    var height = hasMeasurements
                        ? ResolveHeightForGrade(classContext.Grade, school.Blueprint.Level, studentKey)
                        : 0;
                    var weight = hasMeasurements
                        ? ResolveWeightForGrade(classContext.Grade, school.Blueprint.Level, studentKey)
                        : 0;

                    var child = new ChildProfile
                    {
                        Id = childId,
                        ParentUserID = parent?.User.Id,
                        ParentPhone = parentPhone,
                        FullName = studentName,
                        Age = age,
                        Grade = $"Lớp {classContext.ClassName}",
                        Gender = isMale ? Gender.Male : Gender.Female,
                        SchoolID = school.SchoolId,
                        ClassGroupID = classContext.ClassId,
                        IsDeleted = false,
                        DOB = dob,
                        Avatar = "avatar.jpg",
                        HeightCm = height,
                        WeightKg = weight
                    };

                    var import = new StudentDataImport
                    {
                        Id = StableGuid($"{studentKey}:import"),
                        SchoolID = school.SchoolId,
                        StudentCode = studentCode,
                        FullName = studentName,
                        Class = classContext.ClassName,
                        ParentPhone = parentPhone,
                        DateOfBirth = dob,
                        Gender = isMale ? "Male" : "Female",
                        HomeroomTeacherName = classContext.TeacherName,
                        HomeroomTeacherEmail = classContext.TeacherEmail,
                        IsRegistered = parent != null,
                        MatchedChildID = childId,
                        CreatedAt = now.AddDays(-45),
                        UpdatedAt = now.AddDays(-12)
                    };

                    bundle.ChildProfiles.Add(child);
                    bundle.StudentDataImports.Add(import);

                    classContext.Students.Add(new StudentContext
                    {
                        ChildId = childId,
                        Parent = parent,
                        SchoolId = school.SchoolId,
                        ClassId = classContext.ClassId,
                        ClassName = classContext.ClassName,
                        StudentName = studentName,
                        ParentPhone = parentPhone,
                        ParentLinked = parent != null,
                        HasMeasurements = hasMeasurements
                    });
                }
            }
        }
    }

    private static void CreateTeacherReports(SeedBundle bundle, IReadOnlyList<SchoolContext> schoolContexts, DateTime now)
    {
        foreach (var school in schoolContexts)
        {
            for (var index = 0; index < school.Classes.Count; index++)
            {
                var classContext = school.Classes[index];
                var reportId = StableGuid($"{classContext.ClassId}:report:primary");
                var submittedAt = now.AddDays(-(index % 12 + 2));

                var (reportType, status, title, content, reviewNote) = classContext.Scenario switch
                {
                    ClassScenario.ParentGap => (
                        TeacherReportType.OrderCoverage,
                        TeacherReportStatus.Submitted,
                        $"Nhắc phụ huynh lớp {classContext.ClassName} hoàn tất liên kết",
                        "Tỷ lệ phụ huynh liên kết còn thấp, cần hỗ trợ xác minh số điện thoại và hướng dẫn phụ huynh vào danh mục HK2.",
                        (string?)null),
                    ClassScenario.MeasurementGap => (
                        TeacherReportType.QualityIssue,
                        TeacherReportStatus.Submitted,
                        $"Thiếu số đo ở lớp {classContext.ClassName}",
                        "Nhiều học sinh chưa đủ chiều cao hoặc cân nặng để gợi ý size, cần nhắc cập nhật lại hồ sơ trước đợt chốt đơn.",
                        (string?)null),
                    ClassScenario.OrderGap => (
                        TeacherReportType.OrderCoverage,
                        TeacherReportStatus.Reviewed,
                        $"Theo dõi tiến độ đặt may lớp {classContext.ClassName}",
                        "Danh sách phụ huynh đã liên kết nhưng chưa phát sinh đơn đang được tổng hợp để nhắc theo từng hộ.",
                        "Nhà trường đã tiếp nhận, ưu tiên nhắc nhóm chưa đặt trước hạn chốt."),
                    _ => (
                        TeacherReportType.General,
                        TeacherReportStatus.Reviewed,
                        $"Tổng hợp tình hình lớp {classContext.ClassName}",
                        "Lớp vận hành ổn định, tiến độ liên kết phụ huynh và độ phủ đơn hàng đang nằm trong ngưỡng an toàn.",
                        "Đã ghi nhận, tiếp tục theo dõi định kỳ.")
                };

                bundle.TeacherReports.Add(new TeacherReport
                {
                    Id = reportId,
                    ClassGroupId = classContext.ClassId,
                    TeacherUserId = classContext.TeacherUserId,
                    ReportType = reportType,
                    Title = title,
                    Content = content,
                    Status = status,
                    SubmittedAt = submittedAt,
                    ReviewedAt = status == TeacherReportStatus.Reviewed ? submittedAt.AddDays(2) : null,
                    ReviewNote = reviewNote
                });

                if (index % 5 == 0)
                {
                    bundle.TeacherReports.Add(new TeacherReport
                    {
                        Id = StableGuid($"{classContext.ClassId}:report:followup"),
                        ClassGroupId = classContext.ClassId,
                        TeacherUserId = classContext.TeacherUserId,
                        ReportType = TeacherReportType.General,
                        Title = $"Cập nhật bổ sung lớp {classContext.ClassName}",
                        Content = "Giáo viên cập nhật thêm danh sách phụ huynh mới liên kết và đề xuất nhắc nhóm chưa hoàn thiện hồ sơ số đo.",
                        Status = TeacherReportStatus.Submitted,
                        SubmittedAt = submittedAt.AddDays(1)
                    });
                }
            }
        }
    }

    private static void CreateOrdersPaymentsAndFeedback(SeedBundle bundle, IReadOnlyList<SchoolContext> schoolContexts, DateTime now)
    {
        var statusCycle = new[]
        {
            OrderStatus.Pending,
            OrderStatus.Paid,
            OrderStatus.Accepted,
            OrderStatus.Confirmed,
            OrderStatus.Processed,
            OrderStatus.InProduction,
            OrderStatus.ReadyToShip,
            OrderStatus.Shipped,
            OrderStatus.Delivered,
            OrderStatus.Cancelled,
            OrderStatus.Refunded,
            OrderStatus.Delivered,
            OrderStatus.Shipped,
            OrderStatus.Accepted
        };

        var shippingCompanies = new[] { "Giao Hàng Nhanh", "Viettel Post", "J&T Express" };
        var feedbackComments = new[]
        {
            "Đồng phục đúng size, giao nhanh hơn dự kiến.",
            "Vải ổn, đường may chắc chắn, phụ huynh hài lòng.",
            "Cần cải thiện bao bì nhưng chất lượng sản phẩm tốt.",
            "Màu sắc và form áo đúng như công bố trong danh mục."
        };

        var orderOrdinal = 0;

        foreach (var school in schoolContexts)
        {
            foreach (var classContext in school.Classes)
            {
                foreach (var student in classContext.Students.Where(x => x.ParentLinked))
                {
                    if (!ShouldCreateOrder(classContext.Scenario, student.ChildId))
                        continue;

                    var status = statusCycle[orderOrdinal % statusCycle.Length];
                    orderOrdinal++;

                    var providerId = ResolveProviderIdForOrder(school, student.ChildId, status);
                    var publicationId = status == OrderStatus.Delivered || status == OrderStatus.Refunded
                        ? school.ClosedPublicationId
                        : school.ActivePublicationId;
                    var providerOutfits = school.Outfits.Where(x => x.ProviderId == providerId).ToList();
                    if (providerOutfits.Count == 0)
                        providerOutfits = school.Outfits;

                    var itemCount = providerOutfits.Count == 1
                        ? 1
                        : StableInt($"{student.ChildId}:order-items:{status}", 1, 2);

                    var chosenOutfits = providerOutfits
                        .OrderBy(x => StableGuid($"{student.ChildId}:{status}:{x.OutfitId}"))
                        .Take(itemCount)
                        .ToList();

                    var orderId = StableGuid($"{student.ChildId}:order:{status}:{orderOrdinal}");
                    var orderDate = ResolveOrderDate(now, status, orderOrdinal);
                    var shippingAddress = BuildShippingAddress(student.ChildId, school.Blueprint);
                    var recipientName = student.Parent!.User.FullName;
                    var recipientPhone = student.Parent.User.Phone;
                    var shippingCompany = status is OrderStatus.ReadyToShip or OrderStatus.Shipped or OrderStatus.Delivered
                        ? Pick($"{orderId}:shipper", shippingCompanies)
                        : null;
                    var trackingCode = shippingCompany != null
                        ? $"VTOS-{GetCompactCode(school.Blueprint.Key)}-{orderOrdinal:0000}"
                        : null;

                    decimal totalAmount = 0m;
                    decimal providerPayout = 0m;

                    var orderItems = new List<OrderItem>();
                    for (var itemIndex = 0; itemIndex < chosenOutfits.Count; itemIndex++)
                    {
                        var outfit = chosenOutfits[itemIndex];
                        var variant = bundle.ProductVariants
                            .Where(x => x.OutfitID == outfit.OutfitId)
                            .OrderBy(x => SizeDistance(x.Size, student.HasMeasurements))
                            .ThenBy(x => x.Size)
                            .First();

                        var quantity = StableInt($"{orderId}:qty:{itemIndex}", 1, itemIndex == 0 ? 2 : 1);
                        var itemTotal = variant.Price * quantity;
                        totalAmount += itemTotal;
                        providerPayout += contextPrice(school.ProviderCostByOutfitId, outfit.OutfitId) * quantity;

                        orderItems.Add(new OrderItem
                        {
                            Id = StableGuid($"{orderId}:item:{itemIndex}"),
                            OrderID = orderId,
                            ProductVariantID = variant.Id,
                            Quantity = quantity,
                            UnitPrice = variant.Price,
                            SizeOrdered = variant.Size,
                            IsCustomOrder = false,
                            CreatedAt = orderDate
                        });
                    }

                    var order = new Order
                    {
                        Id = orderId,
                        ChildProfileID = student.ChildId,
                        ProviderID = providerId,
                        SemesterPublicationID = publicationId,
                        OrderDate = orderDate,
                        OrderStatus = status,
                        TotalAmount = totalAmount,
                        ShippingAddress = shippingAddress,
                        DeliveryMethod = "HomeDelivery",
                        TrackingCode = trackingCode,
                        ShippingCompany = shippingCompany,
                        RecipientName = recipientName,
                        RecipientPhone = recipientPhone,
                        CancelReason = status == OrderStatus.Cancelled
                            ? "Phụ huynh đổi nhu cầu trước khi thanh toán."
                            : status == OrderStatus.Refunded
                                ? "Đã hoàn tiền vì phụ huynh phản hồi lỗi may."
                                : null,
                        IsProviderPaid = status == OrderStatus.Delivered,
                        CreatedAt = orderDate,
                        UpdatedAt = status == OrderStatus.Pending ? null : orderDate.AddDays(1)
                    };

                    bundle.Orders.Add(order);
                    bundle.OrderItems.AddRange(orderItems);

                    CreatePaymentTransactions(
                        bundle,
                        school,
                        providerId,
                        order,
                        status,
                        totalAmount,
                        providerPayout,
                        orderDate,
                        recipientName);

                    if (status == OrderStatus.Delivered)
                    {
                        var feedbackItem = orderItems[0];
                        bundle.Feedbacks.Add(new Feedback
                        {
                            Id = StableGuid($"{orderId}:feedback"),
                            UserID = student.Parent.User.Id,
                            OrderItemID = feedbackItem.Id,
                            Rating = StableInt($"{orderId}:rating", 4, 5),
                            Comment = Pick($"{orderId}:comment", feedbackComments),
                            Timestamp = orderDate.AddDays(10),
                            ModerationStatus = ModerationStatus.Approved,
                            CreatedAt = orderDate.AddDays(10)
                        });
                    }
                }
            }
        }

        static decimal contextPrice(Dictionary<Guid, decimal> providerCostByOutfitId, Guid outfitId)
            => providerCostByOutfitId.TryGetValue(outfitId, out var value) ? value : 0m;
    }

    private static void CreatePaymentTransactions(
        SeedBundle bundle,
        SchoolContext school,
        Guid providerId,
        Order order,
        OrderStatus status,
        decimal totalAmount,
        decimal providerPayout,
        DateTime orderDate,
        string recipientName)
    {
        var providerWalletId = providerId == school.PrimaryProvider.ProviderId
            ? school.PrimaryProvider.WalletId
            : school.SecondaryProvider.WalletId;

        if (status == OrderStatus.Pending)
        {
            bundle.PaymentTransactions.Add(new PaymentTransaction
            {
                Id = StableGuid($"{order.Id}:payment:pending"),
                OrderID = order.Id,
                WalletID = school.WalletId,
                PaymentLinkId = $"payos-{order.Id:N}",
                TransactionType = TransactionType.OrderPayment,
                GatewayType = PaymentGatewayType.PayOS,
                TransactionStatus = PaymentStatus.Pending,
                Amount = totalAmount,
                TransactionTimestamp = orderDate,
                Description = $"{recipientName} đã tạo link thanh toán chờ xử lý.",
                CreatedAt = orderDate
            });
            return;
        }

        if (status != OrderStatus.Cancelled)
        {
            bundle.PaymentTransactions.Add(new PaymentTransaction
            {
                Id = StableGuid($"{order.Id}:payment:completed"),
                OrderID = order.Id,
                WalletID = school.WalletId,
                PaymentLinkId = $"payos-{order.Id:N}",
                TransactionType = TransactionType.OrderPayment,
                GatewayType = PaymentGatewayType.PayOS,
                TransactionStatus = PaymentStatus.Completed,
                Amount = totalAmount,
                TransactionTimestamp = orderDate.AddHours(2),
                Description = $"{recipientName} thanh toán đơn hàng qua PayOS.",
                CreatedAt = orderDate.AddHours(2)
            });
        }

        if (status == OrderStatus.Refunded)
        {
            bundle.PaymentTransactions.Add(new PaymentTransaction
            {
                Id = StableGuid($"{order.Id}:payment:refund"),
                OrderID = order.Id,
                WalletID = school.WalletId,
                TransactionType = TransactionType.Refund,
                GatewayType = PaymentGatewayType.Other,
                TransactionStatus = PaymentStatus.Completed,
                Amount = totalAmount,
                TransactionTimestamp = orderDate.AddDays(2),
                Description = $"Hoàn tiền cho phụ huynh đơn {order.Id:N}.",
                CreatedAt = orderDate.AddDays(2)
            });
        }

        if (status == OrderStatus.Delivered && providerPayout > 0)
        {
            bundle.PaymentTransactions.Add(new PaymentTransaction
            {
                Id = StableGuid($"{order.Id}:payment:provider-school"),
                OrderID = order.Id,
                WalletID = school.WalletId,
                TransactionType = TransactionType.ProviderPayment,
                GatewayType = PaymentGatewayType.Other,
                TransactionStatus = PaymentStatus.Completed,
                Amount = providerPayout,
                TransactionTimestamp = orderDate.AddDays(9),
                Description = $"Trường chuyển thanh toán cho nhà cung cấp đơn {order.Id:N}.",
                CreatedAt = orderDate.AddDays(9)
            });

            bundle.PaymentTransactions.Add(new PaymentTransaction
            {
                Id = StableGuid($"{order.Id}:payment:provider-receive"),
                OrderID = order.Id,
                WalletID = providerWalletId,
                TransactionType = TransactionType.ProviderPayment,
                GatewayType = PaymentGatewayType.Other,
                TransactionStatus = PaymentStatus.Completed,
                Amount = providerPayout,
                TransactionTimestamp = orderDate.AddDays(9),
                Description = $"Nhà cung cấp nhận thanh toán cho đơn {order.Id:N}.",
                CreatedAt = orderDate.AddDays(9)
            });
        }
    }

    private static void UpdateWalletBalances(SeedBundle bundle)
    {
        var walletLookup = bundle.Wallets.ToDictionary(x => x.Id);

        foreach (var wallet in bundle.Wallets)
            wallet.Balance = 0m;

        foreach (var tx in bundle.PaymentTransactions.Where(x => x.TransactionStatus == PaymentStatus.Completed && x.WalletID.HasValue))
        {
            var wallet = walletLookup[tx.WalletID!.Value];
            if (wallet.OwnerType == WalletOwnerType.School)
            {
                wallet.Balance += tx.TransactionType switch
                {
                    TransactionType.OrderPayment => tx.Amount,
                    TransactionType.Refund => -tx.Amount,
                    TransactionType.ProviderPayment => -tx.Amount,
                    _ => 0m
                };
            }
            else if (wallet.OwnerType == WalletOwnerType.Provider)
            {
                wallet.Balance += tx.TransactionType == TransactionType.ProviderPayment ? tx.Amount : 0m;
            }
        }

        foreach (var wallet in bundle.Wallets)
            wallet.UpdatedAt = DateTime.UtcNow;
    }

    private static ParentContext CreateParentContext(
        string householdKey,
        int parentSequence,
        Guid seedChildId,
        bool childIsMale,
        string passwordHash,
        DateTime now)
    {
        var parentId = StableGuid($"{householdKey}:parent-user");
        var parentIsFemale = StableInt($"{householdKey}:gender", 0, 99) < 55;
        var parentName = BuildAdultName($"{householdKey}:name", parentIsFemale);
        var phone = GeneratePhone($"{householdKey}:phone", parentIsFemale ? "090" : "093");
        var birthYear = parentIsFemale ? 1985 : 1983;
        var dob = new DateTime(
            birthYear + StableInt($"{householdKey}:dob-year", -3, 4),
            StableInt($"{householdKey}:dob-month", 1, 12),
            StableInt($"{householdKey}:dob-day", 1, 28),
            0,
            0,
            0,
            DateTimeKind.Utc);

        var user = new User
        {
            Id = parentId,
            FullName = parentName,
            Email = $"parent{parentSequence:D4}@vtos.com",
            PasswordHash = passwordHash,
            Phone = phone,
            Avatar = "avatar.jpg",
            RoleID = RoleParent,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = now
        };

        var profile = new ParentProfile
        {
            Id = StableGuid($"{householdKey}:parent-profile"),
            UserID = parentId,
            DOB = dob,
            Gender = parentIsFemale ? Gender.Female : Gender.Male
        };

        return new ParentContext
        {
            User = user,
            Profile = profile,
            SourceChildId = seedChildId,
            PrefersBoysUniform = childIsMale
        };
    }

    private static string BuildHouseholdKey(
        string studentKey,
        int studentIndex,
        IReadOnlyList<string> householdKeysInClass,
        string schoolKey,
        string className)
    {
        if (studentIndex % 9 == 0 && householdKeysInClass.Count > 0)
            return householdKeysInClass[^1];

        return $"{schoolKey}:{className}:household:{studentIndex}";
    }

    private static bool ShouldCreateOrder(ClassScenario scenario, Guid childId)
    {
        var chance = StableInt($"{childId}:order-chance", 0, 99);
        return chance < scenario switch
        {
            ClassScenario.Healthy => 62,
            ClassScenario.ParentGap => 36,
            ClassScenario.MeasurementGap => 44,
            ClassScenario.OrderGap => 18,
            _ => 42
        };
    }

    private static Guid ResolveProviderIdForOrder(SchoolContext school, Guid childId, OrderStatus status)
    {
        if (status == OrderStatus.Refunded)
            return school.PrimaryProvider.ProviderId;

        return StableInt($"{childId}:provider-pick:{status}", 0, 99) < 68
            ? school.PrimaryProvider.ProviderId
            : school.SecondaryProvider.ProviderId;
    }

    private static DateTime ResolveOrderDate(DateTime now, OrderStatus status, int ordinal)
    {
        var daysAgo = status switch
        {
            OrderStatus.Pending => 1,
            OrderStatus.Paid => 3,
            OrderStatus.Accepted => 5,
            OrderStatus.Confirmed => 6,
            OrderStatus.Processed => 8,
            OrderStatus.InProduction => 10,
            OrderStatus.ReadyToShip => 12,
            OrderStatus.Shipped => 16,
            OrderStatus.Delivered => 25,
            OrderStatus.Cancelled => 4,
            OrderStatus.Refunded => 30,
            _ => 7
        };

        return now.AddDays(-(daysAgo + (ordinal % 3)));
    }

    private static int GetLinkThreshold(ClassScenario scenario)
    {
        return scenario switch
        {
            ClassScenario.Healthy => 92,
            ClassScenario.ParentGap => 60,
            ClassScenario.MeasurementGap => 84,
            ClassScenario.OrderGap => 88,
            _ => 77
        };
    }

    private static int GetMeasurementThreshold(ClassScenario scenario)
    {
        return scenario switch
        {
            ClassScenario.Healthy => 92,
            ClassScenario.ParentGap => 86,
            ClassScenario.MeasurementGap => 58,
            ClassScenario.OrderGap => 88,
            _ => 78
        };
    }

    private static ClassScenario ResolveScenario(int classOrdinal)
    {
        return classOrdinal switch
        {
            0 => ClassScenario.Healthy,
            1 => ClassScenario.ParentGap,
            2 => ClassScenario.MeasurementGap,
            3 => ClassScenario.OrderGap,
            _ when classOrdinal % 6 == 0 => ClassScenario.OrderGap,
            _ => ClassScenario.Mixed
        };
    }

    private static OutfitBlueprint[] GetOutfitBlueprints(SchoolBlueprint blueprint)
    {
        if (blueprint.Level == "THPT")
        {
            return
            [
                new OutfitBlueprint("shirt", $"Áo sơ mi trắng {blueprint.SchoolName}", "Áo sơ mi tay dài dùng cho đồng phục chính khóa.", "Kate Mỹ", "Trắng", 185000m, OutfitType.Uniform),
                new OutfitBlueprint("bottom", $"Quần tây / chân váy {blueprint.SchoolName}", "Danh mục quần hoặc váy tiêu chuẩn cho học sinh trung học.", "Tuytsi", "Xanh đen", 210000m, OutfitType.Uniform),
                new OutfitBlueprint("jacket", $"Áo khoác đồng phục {blueprint.SchoolName}", "Áo khoác gió đồng phục cho ca sáng và hoạt động ngoại khóa.", "Microfiber", "Xanh navy", 320000m, OutfitType.Uniform)
            ];
        }

        if (blueprint.Level == "THCS")
        {
            return
            [
                new OutfitBlueprint("shirt", $"Áo sơ mi đồng phục {blueprint.SchoolName}", "Áo sơ mi chính khóa dành cho học sinh THCS.", "Kate Silk", "Trắng", 165000m, OutfitType.Uniform),
                new OutfitBlueprint("bottom", $"Quần kaki đồng phục {blueprint.SchoolName}", "Quần kaki xanh đậm dùng cho học sinh THCS.", "Kaki co giãn", "Xanh đậm", 175000m, OutfitType.Uniform),
                new OutfitBlueprint("sports", $"Bộ thể dục {blueprint.SchoolName}", "Bộ thể dục mặc trong tiết học vận động.", "Poly lạnh", "Xanh biển", 195000m, OutfitType.Sportswear)
            ];
        }

        return
        [
            new OutfitBlueprint("polo", $"Áo polo đồng phục {blueprint.SchoolName}", "Áo polo cổ viền cho học sinh tiểu học.", "Cotton cá sấu", "Trắng kem", 145000m, OutfitType.Uniform),
            new OutfitBlueprint("short", $"Quần short / chân váy {blueprint.SchoolName}", "Mẫu quần short hoặc chân váy đồng phục tiểu học.", "Kaki mềm", "Xanh than", 135000m, OutfitType.Uniform),
            new OutfitBlueprint("sports", $"Áo thể dục {blueprint.SchoolName}", "Áo thể dục nhẹ, dễ vận động cho học sinh tiểu học.", "Poly cotton", "Cam nhạt", 155000m, OutfitType.Sportswear)
        ];
    }

    private static string BuildShippingAddress(Guid childId, SchoolBlueprint school)
    {
        var wards = school.Level switch
        {
            "THPT" => new[] { "Hải Châu 1", "Hải Châu 2", "Thanh Bình" },
            "THCS" => new[] { "Phước Ninh", "Bình Hiên", "Thuận Phước" },
            _ => new[] { "Thanh Khê Đông", "Thanh Khê Tây", "Xuân Hà" }
        };

        var streetNumbers = StableInt($"{childId}:street-number", 12, 210);
        var streets = new[] { "Lê Duẩn", "Hoàng Diệu", "Nguyễn Văn Linh", "Trần Phú", "Điện Biên Phủ" };
        return $"{streetNumbers} {Pick($"{childId}:street", streets)}, {Pick($"{childId}:ward", wards)}, Đà Nẵng";
    }

    private static int ResolveHeightForGrade(int grade, string level, string key)
    {
        var baseline = level switch
        {
            "THPT" => 152 + ((grade - 10) * 4),
            "THCS" => 134 + ((grade - 6) * 5),
            _ => 112 + ((grade - 1) * 5)
        };

        return baseline + StableInt($"{key}:height-offset", 0, 11);
    }

    private static float ResolveWeightForGrade(int grade, string level, string key)
    {
        var baseline = level switch
        {
            "THPT" => 42 + ((grade - 10) * 4),
            "THCS" => 30 + ((grade - 6) * 4),
            _ => 19 + ((grade - 1) * 3)
        };

        return baseline + StableInt($"{key}:weight-offset", 0, 8);
    }

    private static string BuildAdultName(string key, bool female)
    {
        var familyNames = new[] { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Võ", "Đặng", "Bùi", "Phan" };
        var middleMale = new[] { "Văn", "Đức", "Minh", "Quang", "Gia", "Hữu" };
        var givenMale = new[] { "Long", "Khang", "Tuấn", "Khánh", "Phúc", "Hải", "Phong", "Nam" };
        var middleFemale = new[] { "Thị", "Ngọc", "Thanh", "Kim", "Bảo", "Thu" };
        var givenFemale = new[] { "Hà", "Anh", "Linh", "Trang", "Mai", "Ngân", "Vy", "Thảo" };

        return $"{Pick($"{key}:family", familyNames)} {Pick($"{key}:middle", female ? middleFemale : middleMale)} {Pick($"{key}:given", female ? givenFemale : givenMale)}";
    }

    private static string BuildStudentName(string key, bool male)
    {
        var familyNames = new[] { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Võ", "Bùi", "Đặng", "Phan" };
        var middleMale = new[] { "Gia", "Minh", "Quốc", "Tuấn", "Hữu", "Anh", "Đức" };
        var givenMale = new[] { "Khôi", "Khang", "Bảo", "Huy", "Duy", "Phúc", "Nhật", "An" };
        var middleFemale = new[] { "Ngọc", "Bảo", "Thanh", "Khánh", "Gia", "Thảo", "Mỹ" };
        var givenFemale = new[] { "Anh", "Linh", "Trân", "Vy", "My", "Nhi", "Hà", "Mai" };

        return $"{Pick($"{key}:family", familyNames)} {Pick($"{key}:middle", male ? middleMale : middleFemale)} {Pick($"{key}:given", male ? givenMale : givenFemale)}";
    }

    private static int SizeDistance(string size, bool hasMeasurements)
    {
        if (!hasMeasurements)
        {
            return size switch
            {
                "M" => 0,
                "S" => 1,
                "L" => 2,
                "XS" => 2,
                _ => 3
            };
        }

        return size switch
        {
            "S" => 1,
            "M" => 0,
            "L" => 1,
            "XL" => 2,
            "XS" => 2,
            _ => 3
        };
    }

    private static string GenerateTaxCode(string key)
    {
        var prefix = StableInt($"{key}:tax", 10000000, 99999999);
        return $"040{prefix}";
    }

    private static string GeneratePhone(string key, string prefix)
    {
        var tail = StableInt($"{key}:phone-tail", 1000000, 9999999);
        return $"{prefix}{tail}";
    }

    private static string NormalizePhone(string value)
    {
        return new string(value.Where(char.IsDigit).ToArray());
    }

    private static string GetCompactCode(string value)
    {
        return RemoveDiacritics(value)
            .Replace("school-", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("provider-", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)
            .ToUpperInvariant();
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC)
            .Replace('đ', 'd')
            .Replace('Đ', 'D');
    }

    private static string Pick(string key, string[] values)
    {
        return values[StableInt(key, 0, values.Length - 1)];
    }

    private static int StableInt(string key, int minInclusive, int maxInclusive)
    {
        if (maxInclusive <= minInclusive)
            return minInclusive;

        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(key));
        var raw = BitConverter.ToUInt32(bytes, 0);
        var range = (uint)(maxInclusive - minInclusive + 1);
        return (int)(raw % range) + minInclusive;
    }

    private static Guid StableGuid(string key)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(key));
        return new Guid(bytes);
    }

    private static string GetAcademicYear(DateTime now)
    {
        return now.Month >= 8
            ? $"{now.Year}-{now.Year + 1}"
            : $"{now.Year - 1}-{now.Year}";
    }

    private sealed class SeedBundle
    {
        public List<User> Users { get; } = new();
        public List<School> Schools { get; } = new();
        public List<Provider> Providers { get; } = new();
        public List<Wallet> Wallets { get; } = new();
        public List<ParentProfile> ParentProfiles { get; } = new();
        public List<SchoolManager> SchoolManagers { get; } = new();
        public List<ProviderManager> ProviderManagers { get; } = new();
        public List<SizeChart> SizeCharts { get; } = new();
        public List<SizeChartDetail> SizeChartDetails { get; } = new();
        public List<SizeChartMeasurement> SizeChartMeasurements { get; } = new();
        public List<Outfit> Outfits { get; } = new();
        public List<ProductVariant> ProductVariants { get; } = new();
        public List<Contract> Contracts { get; } = new();
        public List<ContractItem> ContractItems { get; } = new();
        public List<ClassGroup> ClassGroups { get; } = new();
        public List<ChildProfile> ChildProfiles { get; } = new();
        public List<StudentDataImport> StudentDataImports { get; } = new();
        public List<SemesterPublication> SemesterPublications { get; } = new();
        public List<SemesterPublicationOutfit> SemesterPublicationOutfits { get; } = new();
        public List<SemesterPublicationProvider> SemesterPublicationProviders { get; } = new();
        public List<Order> Orders { get; } = new();
        public List<OrderItem> OrderItems { get; } = new();
        public List<PaymentTransaction> PaymentTransactions { get; } = new();
        public List<Feedback> Feedbacks { get; } = new();
        public List<TeacherReport> TeacherReports { get; } = new();
    }

    private sealed class ProviderContext
    {
        public ProviderBlueprint Blueprint { get; init; } = null!;
        public Guid ProviderId { get; init; }
        public Guid ManagerUserId { get; init; }
        public Guid WalletId { get; init; }
    }

    private sealed class SchoolContext
    {
        public SchoolBlueprint Blueprint { get; init; } = null!;
        public Guid SchoolId { get; init; }
        public Guid ManagerUserId { get; init; }
        public Guid WalletId { get; init; }
        public Guid SizeChartId { get; init; }
        public ProviderContext PrimaryProvider { get; init; } = null!;
        public ProviderContext SecondaryProvider { get; init; } = null!;
        public Guid ActivePrimaryContractId { get; set; }
        public Guid ActiveSecondaryContractId { get; set; }
        public Guid ClosedPublicationId { get; set; }
        public Guid ActivePublicationId { get; set; }
        public Guid DraftPublicationId { get; set; }
        public List<OutfitContext> Outfits { get; } = new();
        public List<ClassContext> Classes { get; } = new();
        public Dictionary<Guid, decimal> ProviderCostByOutfitId { get; } = new();
    }

    private sealed class OutfitContext
    {
        public Guid OutfitId { get; init; }
        public Guid ProviderId { get; init; }
        public string OutfitName { get; init; } = string.Empty;
        public decimal BasePrice { get; init; }
    }

    private sealed class ClassContext
    {
        public Guid ClassId { get; init; }
        public string ClassName { get; init; } = string.Empty;
        public int Grade { get; init; }
        public Guid TeacherUserId { get; init; }
        public string TeacherName { get; init; } = string.Empty;
        public string TeacherEmail { get; init; } = string.Empty;
        public ClassScenario Scenario { get; init; }
        public List<StudentContext> Students { get; } = new();
    }

    private sealed class StudentContext
    {
        public Guid ChildId { get; init; }
        public ParentContext? Parent { get; init; }
        public Guid SchoolId { get; init; }
        public Guid ClassId { get; init; }
        public string ClassName { get; init; } = string.Empty;
        public string StudentName { get; init; } = string.Empty;
        public string ParentPhone { get; init; } = string.Empty;
        public bool ParentLinked { get; init; }
        public bool HasMeasurements { get; init; }
    }

    private sealed class ParentContext
    {
        public User User { get; init; } = null!;
        public ParentProfile Profile { get; init; } = null!;
        public Guid SourceChildId { get; init; }
        public bool PrefersBoysUniform { get; init; }
    }

    private sealed record SchoolBlueprint(
        string Key,
        string SchoolName,
        string Level,
        string Address,
        string Email,
        string Phone,
        string RepresentativeName,
        string RepresentativeTitle,
        int StartGrade,
        int EndGrade,
        int ClassesPerGrade,
        int MinStudents,
        int MaxStudents);

    private sealed record ProviderBlueprint(
        string Key,
        string ProviderName,
        string ContactPersonName,
        string Email,
        string Phone,
        string Address,
        string TaxCode,
        string RepresentativeTitle,
        string BankCode,
        string BankName,
        string BankAccountNumber,
        string BankAccountName);

    private sealed record OutfitBlueprint(
        string Code,
        string Name,
        string Description,
        string MaterialType,
        string Color,
        decimal BasePrice,
        OutfitType OutfitType);

    private enum ClassScenario
    {
        Healthy,
        ParentGap,
        MeasurementGap,
        OrderGap,
        Mixed
    }
}

