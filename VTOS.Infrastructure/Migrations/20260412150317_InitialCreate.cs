using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VTOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Category",
                columns: table => new
                {
                    CategoryID = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Category", x => x.CategoryID);
                });

            migrationBuilder.CreateTable(
                name: "EmailVerification",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OTPCode = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Purpose = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Registration")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailVerification", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                columns: table => new
                {
                    RoleID = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsSystemRole = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.RoleID);
                });

            migrationBuilder.CreateTable(
                name: "SizeChart",
                columns: table => new
                {
                    SizeChartID = table.Column<Guid>(type: "uuid", nullable: false),
                    ChartName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "cm"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SizeChart", x => x.SizeChartID);
                });

            migrationBuilder.CreateTable(
                name: "Wallets",
                columns: table => new
                {
                    WalletID = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerID = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerType = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BankCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BankName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    BankAccountNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BankAccountName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wallets", x => x.WalletID);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    UserID = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Avatar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RoleID = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PasswordResetToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PasswordResetTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GoogleId = table.Column<string>(type: "text", nullable: true),
                    AuthProvider = table.Column<string>(type: "text", nullable: false),
                    IsTwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorSecret = table.Column<string>(type: "text", nullable: true),
                    RecoveryCodes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.UserID);
                    table.ForeignKey(
                        name: "FK_User_Role_RoleID",
                        column: x => x.RoleID,
                        principalTable: "Role",
                        principalColumn: "RoleID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SizeChartDetail",
                columns: table => new
                {
                    DetailID = table.Column<Guid>(type: "uuid", nullable: false),
                    SizeChartID = table.Column<Guid>(type: "uuid", nullable: false),
                    SizeLabel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChestMin = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    ChestMax = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    WaistMin = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    WaistMax = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    HipMin = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    HipMax = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    HeightMin = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    HeightMax = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    OtherMeasurements = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SizeChartDetail", x => x.DetailID);
                    table.ForeignKey(
                        name: "FK_SizeChartDetail_SizeChart_SizeChartID",
                        column: x => x.SizeChartID,
                        principalTable: "SizeChart",
                        principalColumn: "SizeChartID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Provider",
                columns: table => new
                {
                    ProviderID = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContactPersonName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TaxCode = table.Column<string>(type: "text", nullable: true),
                    RepresentativeTitle = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    VerificationStatus = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    VerificationDocumentUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Provider", x => x.ProviderID);
                    table.ForeignKey(
                        name: "FK_Provider_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "WalletID");
                });

            migrationBuilder.CreateTable(
                name: "School",
                columns: table => new
                {
                    SchoolID = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    LogoURL = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ContactInfo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Address = table.Column<string>(type: "text", nullable: true),
                    TaxCode = table.Column<string>(type: "text", nullable: true),
                    RepresentativeName = table.Column<string>(type: "text", nullable: true),
                    RepresentativeTitle = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true),
                    CatalogID = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    VerificationStatus = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    VerificationDocumentUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_School", x => x.SchoolID);
                    table.ForeignKey(
                        name: "FK_School_Wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "Wallets",
                        principalColumn: "WalletID");
                });

            migrationBuilder.CreateTable(
                name: "WalletWithdrawalRequest",
                columns: table => new
                {
                    WithdrawalID = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletID = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AdminNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletWithdrawalRequest", x => x.WithdrawalID);
                    table.ForeignKey(
                        name: "FK_WalletWithdrawalRequest_Wallets_WalletID",
                        column: x => x.WalletID,
                        principalTable: "Wallets",
                        principalColumn: "WalletID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContactPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ContactPersonName = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProcessedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountRequests_User_CreatedUserId",
                        column: x => x.CreatedUserId,
                        principalTable: "User",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_AccountRequests_User_ProcessedByUserId",
                        column: x => x.ProcessedByUserId,
                        principalTable: "User",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    ChatMessageID = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelType = table.Column<int>(type: "integer", nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MessageType = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ImageUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ProposalStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ProposalOutfitName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.ChatMessageID);
                    table.ForeignKey(
                        name: "FK_ChatMessages_User_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InAppNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "text", nullable: true),
                    ActionUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InAppNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InAppNotifications_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NotificationType = table.Column<int>(type: "integer", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationLogs_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParentBankAccount",
                columns: table => new
                {
                    BankAccountID = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentUserID = table.Column<Guid>(type: "uuid", nullable: false),
                    BankName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    BankCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AccountNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AccountHolderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentBankAccount", x => x.BankAccountID);
                    table.ForeignKey(
                        name: "FK_ParentBankAccount_User_ParentUserID",
                        column: x => x.ParentUserID,
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ParentProfile",
                columns: table => new
                {
                    ParentProfileID = table.Column<Guid>(type: "uuid", nullable: false),
                    UserID = table.Column<Guid>(type: "uuid", nullable: false),
                    DOB = table.Column<DateTime>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentProfile", x => x.ParentProfileID);
                    table.ForeignKey(
                        name: "FK_ParentProfile_User_UserID",
                        column: x => x.UserID,
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProviderManager",
                columns: table => new
                {
                    ProviderManagerID = table.Column<Guid>(type: "uuid", nullable: false),
                    UserID = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderID = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderManager", x => x.ProviderManagerID);
                    table.ForeignKey(
                        name: "FK_ProviderManager_Provider_ProviderID",
                        column: x => x.ProviderID,
                        principalTable: "Provider",
                        principalColumn: "ProviderID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProviderManager_User_UserID",
                        column: x => x.UserID,
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Campaign",
                columns: table => new
                {
                    CampaignID = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolID = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaign", x => x.CampaignID);
                    table.ForeignKey(
                        name: "FK_Campaign_School_SchoolID",
                        column: x => x.SchoolID,
                        principalTable: "School",
                        principalColumn: "SchoolID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Children",
                columns: table => new
                {
                    ChildID = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentUserID = table.Column<Guid>(type: "uuid", nullable: true),
                    ParentPhone = table.Column<string>(type: "text", nullable: true),
                    FullName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Age = table.Column<int>(type: "integer", nullable: false),
                    Grade = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SchoolID = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DOB = table.Column<DateTime>(type: "date", nullable: true),
                    Avatar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    HeightCm = table.Column<int>(type: "integer", nullable: false),
                    WeightKg = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Children", x => x.ChildID);
                    table.ForeignKey(
                        name: "FK_Children_School_SchoolID",
                        column: x => x.SchoolID,
                        principalTable: "School",
                        principalColumn: "SchoolID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Children_User_ParentUserID",
                        column: x => x.ParentUserID,
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Contract",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolID = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderID = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    ContractNumber = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SchoolSignature = table.Column<string>(type: "text", nullable: true),
                    SchoolSignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProviderSignature = table.Column<string>(type: "text", nullable: true),
                    ProviderSignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SigningOTPCode = table.Column<string>(type: "text", nullable: true),
                    SigningOTPExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SigningOTPFor = table.Column<string>(type: "text", nullable: true),
                    ContractPdfUrl = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contract", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contract_Provider_ProviderID",
                        column: x => x.ProviderID,
                        principalTable: "Provider",
                        principalColumn: "ProviderID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contract_School_SchoolID",
                        column: x => x.SchoolID,
                        principalTable: "School",
                        principalColumn: "SchoolID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImportBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolID = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    TotalRows = table.Column<int>(type: "integer", nullable: false),
                    SuccessCount = table.Column<int>(type: "integer", nullable: false),
                    SkippedCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportBatches_School_SchoolID",
                        column: x => x.SchoolID,
                        principalTable: "School",
                        principalColumn: "SchoolID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Outfit",
                columns: table => new
                {
                    OutfitID = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolID = table.Column<Guid>(type: "uuid", nullable: false),
                    OutfitName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OutfitType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MainImageURL = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SizeChartID = table.Column<Guid>(type: "uuid", nullable: true),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    IsCustomizable = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outfit", x => x.OutfitID);
                    table.ForeignKey(
                        name: "FK_Outfit_School_SchoolID",
                        column: x => x.SchoolID,
                        principalTable: "School",
                        principalColumn: "SchoolID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Outfit_SizeChart_SizeChartID",
                        column: x => x.SizeChartID,
                        principalTable: "SizeChart",
                        principalColumn: "SizeChartID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SchoolManager",
                columns: table => new
                {
                    SchoolManagerID = table.Column<Guid>(type: "uuid", nullable: false),
                    UserID = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolID = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolManager", x => x.SchoolManagerID);
                    table.ForeignKey(
                        name: "FK_SchoolManager_School_SchoolID",
                        column: x => x.SchoolID,
                        principalTable: "School",
                        principalColumn: "SchoolID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolManager_User_UserID",
                        column: x => x.UserID,
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionBatch",
                columns: table => new
                {
                    BatchID = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignID = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderID = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TotalQuantity = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", maxLength: 20, nullable: false),
                    DeliveryDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeliveredQuantity = table.Column<int>(type: "integer", nullable: false),
                    DeliveryConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveryNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionBatch", x => x.BatchID);
                    table.ForeignKey(
                        name: "FK_ProductionBatch_Campaign_CampaignID",
                        column: x => x.CampaignID,
                        principalTable: "Campaign",
                        principalColumn: "CampaignID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionBatch_Provider_ProviderID",
                        column: x => x.ProviderID,
                        principalTable: "Provider",
                        principalColumn: "ProviderID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BodygramScanLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomScanId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BodygramScanId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OrganizationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodygramScanLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BodygramScanLogs_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "ChildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BodygramScanRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildId = table.Column<Guid>(type: "uuid", nullable: false),
                    BodygramScanId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CustomScanId = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScannedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUnix = table.Column<long>(type: "bigint", nullable: false),
                    HeightCm = table.Column<int>(type: "integer", nullable: false),
                    WeightKg = table.Column<float>(type: "real", nullable: false),
                    AvatarUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AvatarFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AvatarType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RawInputJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawMeasurementsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WaistToHipRatio = table.Column<double>(type: "double precision", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodygramScanRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BodygramScanRecords_Children_ChildId",
                        column: x => x.ChildId,
                        principalTable: "Children",
                        principalColumn: "ChildID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Order",
                columns: table => new
                {
                    OrderID = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildProfileID = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OrderStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ShippingAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CampaignID = table.Column<Guid>(type: "uuid", nullable: true),
                    DeliveryMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CancelReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsProviderPaid = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Order", x => x.OrderID);
                    table.ForeignKey(
                        name: "FK_Order_Campaign_CampaignID",
                        column: x => x.CampaignID,
                        principalTable: "Campaign",
                        principalColumn: "CampaignID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Order_Children_ChildProfileID",
                        column: x => x.ChildProfileID,
                        principalTable: "Children",
                        principalColumn: "ChildID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentDataImport",
                columns: table => new
                {
                    ImportID = table.Column<Guid>(type: "uuid", nullable: false),
                    SchoolID = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FullName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Class = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ParentPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Gender = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IsRegistered = table.Column<bool>(type: "boolean", nullable: false),
                    MatchedChildID = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentDataImport", x => x.ImportID);
                    table.ForeignKey(
                        name: "FK_StudentDataImport_Children_MatchedChildID",
                        column: x => x.MatchedChildID,
                        principalTable: "Children",
                        principalColumn: "ChildID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StudentDataImport_School_SchoolID",
                        column: x => x.SchoolID,
                        principalTable: "School",
                        principalColumn: "SchoolID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CampaignOutfit",
                columns: table => new
                {
                    CampaignOutfitID = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignID = table.Column<Guid>(type: "uuid", nullable: false),
                    OutfitID = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderID = table.Column<Guid>(type: "uuid", nullable: true),
                    ContractID = table.Column<Guid>(type: "uuid", nullable: true),
                    CampaignPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MaxQuantity = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignOutfit", x => x.CampaignOutfitID);
                    table.ForeignKey(
                        name: "FK_CampaignOutfit_Campaign_CampaignID",
                        column: x => x.CampaignID,
                        principalTable: "Campaign",
                        principalColumn: "CampaignID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CampaignOutfit_Contract_ContractID",
                        column: x => x.ContractID,
                        principalTable: "Contract",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CampaignOutfit_Outfit_OutfitID",
                        column: x => x.OutfitID,
                        principalTable: "Outfit",
                        principalColumn: "OutfitID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CampaignOutfit_Provider_ProviderID",
                        column: x => x.ProviderID,
                        principalTable: "Provider",
                        principalColumn: "ProviderID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContractItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractID = table.Column<Guid>(type: "uuid", nullable: false),
                    OutfitID = table.Column<Guid>(type: "uuid", nullable: false),
                    PricePerUnit = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MinQuantity = table.Column<int>(type: "integer", nullable: false),
                    MaxQuantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractItem_Contract_ContractID",
                        column: x => x.ContractID,
                        principalTable: "Contract",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContractItem_Outfit_OutfitID",
                        column: x => x.OutfitID,
                        principalTable: "Outfit",
                        principalColumn: "OutfitID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OutfitCategory",
                columns: table => new
                {
                    OutfitID = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryID = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutfitCategory", x => new { x.OutfitID, x.CategoryID });
                    table.ForeignKey(
                        name: "FK_OutfitCategory_Category_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "Category",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OutfitCategory_Outfit_OutfitID",
                        column: x => x.OutfitID,
                        principalTable: "Outfit",
                        principalColumn: "OutfitID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutfitRecommendation",
                columns: table => new
                {
                    RecommendationID = table.Column<Guid>(type: "uuid", nullable: false),
                    UserID = table.Column<Guid>(type: "uuid", nullable: false),
                    OutfitID = table.Column<Guid>(type: "uuid", nullable: false),
                    RecommendationScore = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    RuleConfigID = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutfitRecommendation", x => x.RecommendationID);
                    table.ForeignKey(
                        name: "FK_OutfitRecommendation_Outfit_OutfitID",
                        column: x => x.OutfitID,
                        principalTable: "Outfit",
                        principalColumn: "OutfitID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OutfitRecommendation_User_UserID",
                        column: x => x.UserID,
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductVariant",
                columns: table => new
                {
                    ProductVariantID = table.Column<Guid>(type: "uuid", nullable: false),
                    OutfitID = table.Column<Guid>(type: "uuid", nullable: false),
                    Size = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ColorVariant = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    MaterialType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StockQuantity = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SKUCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VariantImageURL = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariant", x => x.ProductVariantID);
                    table.ForeignKey(
                        name: "FK_ProductVariant_Outfit_OutfitID",
                        column: x => x.OutfitID,
                        principalTable: "Outfit",
                        principalColumn: "OutfitID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TryOnHistory",
                columns: table => new
                {
                    TryOnID = table.Column<Guid>(type: "uuid", nullable: false),
                    GuestSessionID = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UserID = table.Column<Guid>(type: "uuid", nullable: true),
                    ChildID = table.Column<Guid>(type: "uuid", nullable: true),
                    OutfitID = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedPhotoURL = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ResultPhotoURL = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TryOnTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AlignmentAdjustment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SourcePlatform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TryOnHistory", x => x.TryOnID);
                    table.ForeignKey(
                        name: "FK_TryOnHistory_Children_ChildID",
                        column: x => x.ChildID,
                        principalTable: "Children",
                        principalColumn: "ChildID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TryOnHistory_Outfit_OutfitID",
                        column: x => x.OutfitID,
                        principalTable: "Outfit",
                        principalColumn: "OutfitID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TryOnHistory_User_UserID",
                        column: x => x.UserID,
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Complaints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignID = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchID = table.Column<Guid>(type: "uuid", nullable: true),
                    SchoolID = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderID = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Response = table.Column<string>(type: "text", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProofImageUrls = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Complaints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Complaints_Campaign_CampaignID",
                        column: x => x.CampaignID,
                        principalTable: "Campaign",
                        principalColumn: "CampaignID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Complaints_ProductionBatch_BatchID",
                        column: x => x.BatchID,
                        principalTable: "ProductionBatch",
                        principalColumn: "BatchID");
                    table.ForeignKey(
                        name: "FK_Complaints_Provider_ProviderID",
                        column: x => x.ProviderID,
                        principalTable: "Provider",
                        principalColumn: "ProviderID");
                    table.ForeignKey(
                        name: "FK_Complaints_School_SchoolID",
                        column: x => x.SchoolID,
                        principalTable: "School",
                        principalColumn: "SchoolID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeliveryRecord",
                columns: table => new
                {
                    DeliveryRecordID = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchID = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcceptedQuantity = table.Column<int>(type: "integer", nullable: true),
                    DefectiveQuantity = table.Column<int>(type: "integer", nullable: true),
                    DefectNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryRecord", x => x.DeliveryRecordID);
                    table.ForeignKey(
                        name: "FK_DeliveryRecord_ProductionBatch_BatchID",
                        column: x => x.BatchID,
                        principalTable: "ProductionBatch",
                        principalColumn: "BatchID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DistributionSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchID = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Method = table.Column<string>(type: "text", nullable: false),
                    TimeSlot = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistributionSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DistributionSchedules_ProductionBatch_BatchID",
                        column: x => x.BatchID,
                        principalTable: "ProductionBatch",
                        principalColumn: "BatchID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionBatchItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchID = table.Column<Guid>(type: "uuid", nullable: false),
                    OutfitID = table.Column<Guid>(type: "uuid", nullable: false),
                    Size = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionBatchItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionBatchItems_Outfit_OutfitID",
                        column: x => x.OutfitID,
                        principalTable: "Outfit",
                        principalColumn: "OutfitID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductionBatchItems_ProductionBatch_BatchID",
                        column: x => x.BatchID,
                        principalTable: "ProductionBatch",
                        principalColumn: "BatchID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BodygramMeasurementRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScanRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BodygramMeasurementRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BodygramMeasurementRecords_BodygramScanRecords_ScanRecordId",
                        column: x => x.ScanRecordId,
                        principalTable: "BodygramScanRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DistributionRecord",
                columns: table => new
                {
                    DistributionRecordID = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchID = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderID = table.Column<Guid>(type: "uuid", nullable: false),
                    DistributedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ShippingCompany = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TrackingCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProofImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistributionRecord", x => x.DistributionRecordID);
                    table.ForeignKey(
                        name: "FK_DistributionRecord_Order_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Order",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DistributionRecord_ProductionBatch_BatchID",
                        column: x => x.BatchID,
                        principalTable: "ProductionBatch",
                        principalColumn: "BatchID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Invoice",
                columns: table => new
                {
                    InvoiceID = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderID = table.Column<Guid>(type: "uuid", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InvoiceDataURL = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoice", x => x.InvoiceID);
                    table.ForeignKey(
                        name: "FK_Invoice_Order_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Order",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransaction",
                columns: table => new
                {
                    PaymentID = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderID = table.Column<Guid>(type: "uuid", nullable: true),
                    WalletID = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentLinkId = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TransactionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GatewayType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TransactionStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TransactionTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TransactionLog = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransaction", x => x.PaymentID);
                    table.ForeignKey(
                        name: "FK_PaymentTransaction_Order_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Order",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentTransaction_Wallets_WalletID",
                        column: x => x.WalletID,
                        principalTable: "Wallets",
                        principalColumn: "WalletID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItem",
                columns: table => new
                {
                    OrderItemID = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderID = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductVariantID = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SizeOrdered = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsCustomOrder = table.Column<bool>(type: "boolean", nullable: false),
                    CustomMeasurements = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItem", x => x.OrderItemID);
                    table.ForeignKey(
                        name: "FK_OrderItem_Order_OrderID",
                        column: x => x.OrderID,
                        principalTable: "Order",
                        principalColumn: "OrderID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItem_ProductVariant_ProductVariantID",
                        column: x => x.ProductVariantID,
                        principalTable: "ProductVariant",
                        principalColumn: "ProductVariantID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AIFitAnalysis",
                columns: table => new
                {
                    AnalysisID = table.Column<Guid>(type: "uuid", nullable: false),
                    TryOnID = table.Column<Guid>(type: "uuid", nullable: false),
                    DetectedBodyProportions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SuggestedSize = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FitScore = table.Column<int>(type: "integer", nullable: true),
                    AlgorithmVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIFitAnalysis", x => x.AnalysisID);
                    table.ForeignKey(
                        name: "FK_AIFitAnalysis_TryOnHistory_TryOnID",
                        column: x => x.TryOnID,
                        principalTable: "TryOnHistory",
                        principalColumn: "TryOnID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Refund",
                columns: table => new
                {
                    RefundID = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentID = table.Column<Guid>(type: "uuid", nullable: false),
                    RefundAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RefundStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisputeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refund", x => x.RefundID);
                    table.ForeignKey(
                        name: "FK_Refund_PaymentTransaction_PaymentID",
                        column: x => x.PaymentID,
                        principalTable: "PaymentTransaction",
                        principalColumn: "PaymentID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Feedback",
                columns: table => new
                {
                    FeedbackID = table.Column<Guid>(type: "uuid", nullable: false),
                    UserID = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemID = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModerationStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProductVariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedback", x => x.FeedbackID);
                    table.ForeignKey(
                        name: "FK_Feedback_Campaign_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaign",
                        principalColumn: "CampaignID");
                    table.ForeignKey(
                        name: "FK_Feedback_OrderItem_OrderItemID",
                        column: x => x.OrderItemID,
                        principalTable: "OrderItem",
                        principalColumn: "OrderItemID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Feedback_ProductVariant_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariant",
                        principalColumn: "ProductVariantID");
                    table.ForeignKey(
                        name: "FK_Feedback_User_UserID",
                        column: x => x.UserID,
                        principalTable: "User",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountRequests_CreatedAt",
                table: "AccountRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRequests_CreatedUserId",
                table: "AccountRequests",
                column: "CreatedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRequests_ProcessedByUserId",
                table: "AccountRequests",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRequests_Status",
                table: "AccountRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRequests_Type",
                table: "AccountRequests",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_AIFitAnalysis_TryOnID",
                table: "AIFitAnalysis",
                column: "TryOnID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BodygramMeasurementRecords_ScanRecordId_Name",
                table: "BodygramMeasurementRecords",
                columns: new[] { "ScanRecordId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_BodygramScanLogs_ChildId",
                table: "BodygramScanLogs",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_BodygramScanRecords_BodygramScanId",
                table: "BodygramScanRecords",
                column: "BodygramScanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BodygramScanRecords_ChildId",
                table: "BodygramScanRecords",
                column: "ChildId");

            migrationBuilder.CreateIndex(
                name: "IX_BodygramScanRecords_CustomScanId",
                table: "BodygramScanRecords",
                column: "CustomScanId");

            migrationBuilder.CreateIndex(
                name: "IX_Campaign_SchoolID",
                table: "Campaign",
                column: "SchoolID");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignOutfit_CampaignID",
                table: "CampaignOutfit",
                column: "CampaignID");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignOutfit_ContractID",
                table: "CampaignOutfit",
                column: "ContractID");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignOutfit_OutfitID",
                table: "CampaignOutfit",
                column: "OutfitID");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignOutfit_ProviderID",
                table: "CampaignOutfit",
                column: "ProviderID");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_ChannelType_ChannelId_SentAt",
                table: "ChatMessages",
                columns: new[] { "ChannelType", "ChannelId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_SenderUserId",
                table: "ChatMessages",
                column: "SenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Children_IsDeleted",
                table: "Children",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Children_ParentUserID",
                table: "Children",
                column: "ParentUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Children_SchoolID",
                table: "Children",
                column: "SchoolID");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_BatchID",
                table: "Complaints",
                column: "BatchID");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_CampaignID",
                table: "Complaints",
                column: "CampaignID");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_ProviderID",
                table: "Complaints",
                column: "ProviderID");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_SchoolID",
                table: "Complaints",
                column: "SchoolID");

            migrationBuilder.CreateIndex(
                name: "IX_Contract_ProviderID",
                table: "Contract",
                column: "ProviderID");

            migrationBuilder.CreateIndex(
                name: "IX_Contract_SchoolID",
                table: "Contract",
                column: "SchoolID");

            migrationBuilder.CreateIndex(
                name: "IX_ContractItem_ContractID",
                table: "ContractItem",
                column: "ContractID");

            migrationBuilder.CreateIndex(
                name: "IX_ContractItem_OutfitID",
                table: "ContractItem",
                column: "OutfitID");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryRecord_BatchID",
                table: "DeliveryRecord",
                column: "BatchID");

            migrationBuilder.CreateIndex(
                name: "IX_DistributionRecord_BatchID",
                table: "DistributionRecord",
                column: "BatchID");

            migrationBuilder.CreateIndex(
                name: "IX_DistributionRecord_OrderID",
                table: "DistributionRecord",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_DistributionSchedules_BatchID",
                table: "DistributionSchedules",
                column: "BatchID");

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerification_Email",
                table: "EmailVerification",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerification_ExpiresAt",
                table: "EmailVerification",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_CampaignId",
                table: "Feedback",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_OrderItemID",
                table: "Feedback",
                column: "OrderItemID");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_ProductVariantId",
                table: "Feedback",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_UserID",
                table: "Feedback",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_ImportBatches_SchoolID",
                table: "ImportBatches",
                column: "SchoolID");

            migrationBuilder.CreateIndex(
                name: "IX_InAppNotifications_UserId",
                table: "InAppNotifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoice_OrderID",
                table: "Invoice",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationLog_User_Type_Ref",
                table: "NotificationLogs",
                columns: new[] { "UserId", "NotificationType", "ReferenceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Order_CampaignID",
                table: "Order",
                column: "CampaignID");

            migrationBuilder.CreateIndex(
                name: "IX_Order_ChildProfileID",
                table: "Order",
                column: "ChildProfileID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_OrderID",
                table: "OrderItem",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItem_ProductVariantID",
                table: "OrderItem",
                column: "ProductVariantID");

            migrationBuilder.CreateIndex(
                name: "IX_Outfit_SchoolID",
                table: "Outfit",
                column: "SchoolID");

            migrationBuilder.CreateIndex(
                name: "IX_Outfit_SizeChartID",
                table: "Outfit",
                column: "SizeChartID");

            migrationBuilder.CreateIndex(
                name: "IX_OutfitCategory_CategoryID",
                table: "OutfitCategory",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_OutfitRecommendation_OutfitID",
                table: "OutfitRecommendation",
                column: "OutfitID");

            migrationBuilder.CreateIndex(
                name: "IX_OutfitRecommendation_UserID",
                table: "OutfitRecommendation",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_ParentBankAccount_ParentUserID",
                table: "ParentBankAccount",
                column: "ParentUserID");

            migrationBuilder.CreateIndex(
                name: "IX_ParentProfile_UserID",
                table: "ParentProfile",
                column: "UserID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_OrderID",
                table: "PaymentTransaction",
                column: "OrderID");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_WalletID",
                table: "PaymentTransaction",
                column: "WalletID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatch_CampaignID",
                table: "ProductionBatch",
                column: "CampaignID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatch_IsDeleted",
                table: "ProductionBatch",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatch_ProviderID",
                table: "ProductionBatch",
                column: "ProviderID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatchItems_BatchID",
                table: "ProductionBatchItems",
                column: "BatchID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionBatchItems_OutfitID",
                table: "ProductionBatchItems",
                column: "OutfitID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariant_OutfitID",
                table: "ProductVariant",
                column: "OutfitID");

            migrationBuilder.CreateIndex(
                name: "IX_Provider_IsDeleted",
                table: "Provider",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Provider_WalletId",
                table: "Provider",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderManager_ProviderID",
                table: "ProviderManager",
                column: "ProviderID");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderManager_UserID",
                table: "ProviderManager",
                column: "UserID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Refund_PaymentID",
                table: "Refund",
                column: "PaymentID");

            migrationBuilder.CreateIndex(
                name: "IX_Role_RoleName",
                table: "Role",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_School_WalletId",
                table: "School",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolManager_SchoolID",
                table: "SchoolManager",
                column: "SchoolID");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolManager_UserID",
                table: "SchoolManager",
                column: "UserID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SizeChartDetail_SizeChartID",
                table: "SizeChartDetail",
                column: "SizeChartID");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDataImport_MatchedChildID",
                table: "StudentDataImport",
                column: "MatchedChildID");

            migrationBuilder.CreateIndex(
                name: "IX_StudentDataImport_SchoolID",
                table: "StudentDataImport",
                column: "SchoolID");

            migrationBuilder.CreateIndex(
                name: "IX_TryOnHistory_ChildID",
                table: "TryOnHistory",
                column: "ChildID");

            migrationBuilder.CreateIndex(
                name: "IX_TryOnHistory_OutfitID",
                table: "TryOnHistory",
                column: "OutfitID");

            migrationBuilder.CreateIndex(
                name: "IX_TryOnHistory_UserID",
                table: "TryOnHistory",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_User_Email",
                table: "User",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_IsDeleted",
                table: "User",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_User_RoleID",
                table: "User",
                column: "RoleID");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_OwnerID_OwnerType",
                table: "Wallets",
                columns: new[] { "OwnerID", "OwnerType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletWithdrawalRequest_WalletID",
                table: "WalletWithdrawalRequest",
                column: "WalletID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountRequests");

            migrationBuilder.DropTable(
                name: "AIFitAnalysis");

            migrationBuilder.DropTable(
                name: "BodygramMeasurementRecords");

            migrationBuilder.DropTable(
                name: "BodygramScanLogs");

            migrationBuilder.DropTable(
                name: "CampaignOutfit");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "Complaints");

            migrationBuilder.DropTable(
                name: "ContractItem");

            migrationBuilder.DropTable(
                name: "DeliveryRecord");

            migrationBuilder.DropTable(
                name: "DistributionRecord");

            migrationBuilder.DropTable(
                name: "DistributionSchedules");

            migrationBuilder.DropTable(
                name: "EmailVerification");

            migrationBuilder.DropTable(
                name: "Feedback");

            migrationBuilder.DropTable(
                name: "ImportBatches");

            migrationBuilder.DropTable(
                name: "InAppNotifications");

            migrationBuilder.DropTable(
                name: "Invoice");

            migrationBuilder.DropTable(
                name: "NotificationLogs");

            migrationBuilder.DropTable(
                name: "OutfitCategory");

            migrationBuilder.DropTable(
                name: "OutfitRecommendation");

            migrationBuilder.DropTable(
                name: "ParentBankAccount");

            migrationBuilder.DropTable(
                name: "ParentProfile");

            migrationBuilder.DropTable(
                name: "ProductionBatchItems");

            migrationBuilder.DropTable(
                name: "ProviderManager");

            migrationBuilder.DropTable(
                name: "Refund");

            migrationBuilder.DropTable(
                name: "SchoolManager");

            migrationBuilder.DropTable(
                name: "SizeChartDetail");

            migrationBuilder.DropTable(
                name: "StudentDataImport");

            migrationBuilder.DropTable(
                name: "WalletWithdrawalRequest");

            migrationBuilder.DropTable(
                name: "TryOnHistory");

            migrationBuilder.DropTable(
                name: "BodygramScanRecords");

            migrationBuilder.DropTable(
                name: "Contract");

            migrationBuilder.DropTable(
                name: "OrderItem");

            migrationBuilder.DropTable(
                name: "Category");

            migrationBuilder.DropTable(
                name: "ProductionBatch");

            migrationBuilder.DropTable(
                name: "PaymentTransaction");

            migrationBuilder.DropTable(
                name: "ProductVariant");

            migrationBuilder.DropTable(
                name: "Provider");

            migrationBuilder.DropTable(
                name: "Order");

            migrationBuilder.DropTable(
                name: "Outfit");

            migrationBuilder.DropTable(
                name: "Campaign");

            migrationBuilder.DropTable(
                name: "Children");

            migrationBuilder.DropTable(
                name: "SizeChart");

            migrationBuilder.DropTable(
                name: "School");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Wallets");

            migrationBuilder.DropTable(
                name: "Role");
        }
    }
}
