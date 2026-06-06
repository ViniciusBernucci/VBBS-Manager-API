using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VBBSManager.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Tenants",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                Slug = table.Column<string>(type: "text", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tenants", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                PasswordHash = table.Column<string>(type: "text", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
                table.ForeignKey(
                    name: "FK_Users_Tenants_TenantId",
                    column: x => x.TenantId,
                    principalTable: "Tenants",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                Token = table.Column<string>(type: "text", nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_RefreshTokens_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Integrations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Provider = table.Column<int>(type: "integer", nullable: false),
                CredentialsEncrypted = table.Column<string>(type: "text", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Integrations", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Alerts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Type = table.Column<int>(type: "integer", nullable: false),
                Severity = table.Column<int>(type: "integer", nullable: false),
                Title = table.Column<string>(type: "text", nullable: false),
                Message = table.Column<string>(type: "text", nullable: false),
                IsRead = table.Column<bool>(type: "boolean", nullable: false),
                IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Metadata = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Alerts", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "PlanningGoals",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                TargetValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                CurrentValue = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                Category = table.Column<string>(type: "text", nullable: false),
                ComparisonType = table.Column<string>(type: "text", nullable: false),
                ActionIfFailed = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                SortOrder = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PlanningGoals", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "FinancialConfigs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                MonthlyGrossRevenue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                MonthlyAdSpend = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                HotmartFeePercent = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                InstallmentFeePercent = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                InstallmentSalesPercent = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                FederalTaxPercent = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                RefundRatePercent = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                MetaAdsTaxPercent = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                AccountingCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                InvoicingCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                ManychatCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                HotmartPlayerCost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FinancialConfigs", x => x.Id);
            });

        // Indexes
        migrationBuilder.CreateIndex(
            name: "IX_Users_TenantId_Email",
            table: "Users",
            columns: new[] { "TenantId", "Email" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_UserId",
            table: "RefreshTokens",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_PlanningGoals_TenantId_Key",
            table: "PlanningGoals",
            columns: new[] { "TenantId", "Key" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PlanningGoals_TenantId",
            table: "PlanningGoals",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_FinancialConfigs_TenantId",
            table: "FinancialConfigs",
            column: "TenantId",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "FinancialConfigs");
        migrationBuilder.DropTable(name: "PlanningGoals");
        migrationBuilder.DropTable(name: "Alerts");
        migrationBuilder.DropTable(name: "Integrations");
        migrationBuilder.DropTable(name: "RefreshTokens");
        migrationBuilder.DropTable(name: "Users");
        migrationBuilder.DropTable(name: "Tenants");
    }
}
