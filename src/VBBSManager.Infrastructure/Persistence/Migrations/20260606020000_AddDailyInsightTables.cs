using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VBBSManager.Infrastructure.Persistence.Migrations;

[Migration("20260606020000_AddDailyInsightTables")]
public partial class AddDailyInsightTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Remove a tabela de insights agregados anterior
        migrationBuilder.DropTable(name: "MetaCampaignInsights");

        // Nível campanha — granularidade diária
        migrationBuilder.CreateTable(
            name: "MetaCampaignDailyInsights",
            columns: table => new
            {
                Id           = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId     = table.Column<Guid>(type: "uuid", nullable: false),
                Date         = table.Column<DateOnly>(type: "date", nullable: false),
                CampaignId   = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                CampaignName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Impressions  = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                Clicks       = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                Reach        = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                Spend        = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                Cpc          = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                Cpm          = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                Ctr          = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                Frequency    = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                Conversions  = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_MetaCampaignDailyInsights", x => x.Id));

        migrationBuilder.CreateIndex("IX_MetaCampaignDailyInsights_TenantId_CampaignId_Date",
            "MetaCampaignDailyInsights", new[] { "TenantId", "CampaignId", "Date" }, unique: true);
        migrationBuilder.CreateIndex("IX_MetaCampaignDailyInsights_TenantId_Date",
            "MetaCampaignDailyInsights", new[] { "TenantId", "Date" });

        // Nível conjunto de anúncios — granularidade diária
        migrationBuilder.CreateTable(
            name: "MetaAdSetDailyInsights",
            columns: table => new
            {
                Id           = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId     = table.Column<Guid>(type: "uuid", nullable: false),
                Date         = table.Column<DateOnly>(type: "date", nullable: false),
                CampaignId   = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                CampaignName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                AdSetId      = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                AdSetName    = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Impressions  = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                Clicks       = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                Reach        = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                Spend        = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                Cpc          = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                Cpm          = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                Ctr          = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                Frequency    = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                Conversions  = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_MetaAdSetDailyInsights", x => x.Id));

        migrationBuilder.CreateIndex("IX_MetaAdSetDailyInsights_TenantId_AdSetId_Date",
            "MetaAdSetDailyInsights", new[] { "TenantId", "AdSetId", "Date" }, unique: true);
        migrationBuilder.CreateIndex("IX_MetaAdSetDailyInsights_TenantId_Date",
            "MetaAdSetDailyInsights", new[] { "TenantId", "Date" });

        // Nível anúncio — granularidade diária
        migrationBuilder.CreateTable(
            name: "MetaAdDailyInsights",
            columns: table => new
            {
                Id           = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId     = table.Column<Guid>(type: "uuid", nullable: false),
                Date         = table.Column<DateOnly>(type: "date", nullable: false),
                CampaignId   = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                CampaignName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                AdSetId      = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                AdSetName    = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                AdId         = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                AdName       = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Impressions  = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                Clicks       = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                Reach        = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                Spend        = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                Cpc          = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                Cpm          = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                Ctr          = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                Frequency    = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                Conversions  = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_MetaAdDailyInsights", x => x.Id));

        migrationBuilder.CreateIndex("IX_MetaAdDailyInsights_TenantId_AdId_Date",
            "MetaAdDailyInsights", new[] { "TenantId", "AdId", "Date" }, unique: true);
        migrationBuilder.CreateIndex("IX_MetaAdDailyInsights_TenantId_Date",
            "MetaAdDailyInsights", new[] { "TenantId", "Date" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "MetaCampaignDailyInsights");
        migrationBuilder.DropTable(name: "MetaAdSetDailyInsights");
        migrationBuilder.DropTable(name: "MetaAdDailyInsights");
    }
}
