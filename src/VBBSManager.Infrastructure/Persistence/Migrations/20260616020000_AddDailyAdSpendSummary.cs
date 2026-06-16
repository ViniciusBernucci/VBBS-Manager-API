using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VBBSManager.Infrastructure.Persistence.Migrations;

public partial class AddDailyAdSpendSummary : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DailyAdSpendSummaries",
            columns: table => new
            {
                Id              = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId        = table.Column<Guid>(type: "uuid", nullable: false),
                Date            = table.Column<DateOnly>(type: "date", nullable: false),
                TotalSpend      = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                TotalImpressions = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                TotalClicks     = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                TotalConversions = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                TotalRevenue    = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                LastSyncedAt    = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedAt       = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt       = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DailyAdSpendSummaries", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DailyAdSpendSummaries_TenantId_Date",
            table: "DailyAdSpendSummaries",
            columns: ["TenantId", "Date"],
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "DailyAdSpendSummaries");
    }
}
