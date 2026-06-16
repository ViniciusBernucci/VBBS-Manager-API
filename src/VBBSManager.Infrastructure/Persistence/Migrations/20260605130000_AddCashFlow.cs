using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VBBSManager.Infrastructure.Persistence.Migrations;

[Migration("20260605130000_AddCashFlow")]
public partial class AddCashFlow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CashFlowConfigs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                InitialYear = table.Column<int>(type: "integer", nullable: false),
                InitialMonth = table.Column<int>(type: "integer", nullable: false),
                InitialBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CashFlowConfigs", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "CashFlowTransactions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                Date = table.Column<DateOnly>(type: "date", nullable: false),
                Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                Category = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CashFlowTransactions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CashFlowConfigs_TenantId",
            table: "CashFlowConfigs",
            column: "TenantId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CashFlowTransactions_TenantId_Date",
            table: "CashFlowTransactions",
            columns: new[] { "TenantId", "Date" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CashFlowTransactions");
        migrationBuilder.DropTable(name: "CashFlowConfigs");
    }
}
