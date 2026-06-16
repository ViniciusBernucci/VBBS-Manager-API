using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VBBSManager.Infrastructure.Persistence.Migrations;

[Migration("20260612010000_AddRevenueToInsights")]
public partial class AddRevenueToInsights : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "Revenue",
            table: "MetaCampaignDailyInsights",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "Revenue",
            table: "MetaAdSetDailyInsights",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.AddColumn<decimal>(
            name: "Revenue",
            table: "MetaAdDailyInsights",
            type: "numeric(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Revenue", table: "MetaCampaignDailyInsights");
        migrationBuilder.DropColumn(name: "Revenue", table: "MetaAdSetDailyInsights");
        migrationBuilder.DropColumn(name: "Revenue", table: "MetaAdDailyInsights");
    }
}
