using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VBBSManager.Infrastructure.Persistence.Migrations;

[Migration("20260606010000_AddMetaCampaignInsights")]
public partial class AddMetaCampaignInsights : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MetaCampaignInsights",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                CampaignId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                CampaignName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Impressions = table.Column<long>(type: "bigint", nullable: false),
                Clicks = table.Column<long>(type: "bigint", nullable: false),
                Reach = table.Column<long>(type: "bigint", nullable: false),
                Spend = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                Cpc = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                Cpm = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                Ctr = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
                Conversions = table.Column<int>(type: "integer", nullable: false),
                DateStart = table.Column<DateOnly>(type: "date", nullable: false),
                DateStop = table.Column<DateOnly>(type: "date", nullable: false),
                LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MetaCampaignInsights", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MetaCampaignInsights_TenantId_CampaignId_DateStart_DateStop",
            table: "MetaCampaignInsights",
            columns: new[] { "TenantId", "CampaignId", "DateStart", "DateStop" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_MetaCampaignInsights_TenantId_DateStart",
            table: "MetaCampaignInsights",
            columns: new[] { "TenantId", "DateStart" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "MetaCampaignInsights");
    }
}
