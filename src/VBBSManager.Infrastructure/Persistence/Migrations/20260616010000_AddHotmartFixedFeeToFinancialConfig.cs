using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VBBSManager.Infrastructure.Persistence.Migrations;

[Migration("20260616010000_AddHotmartFixedFeeToFinancialConfig")]
public partial class AddHotmartFixedFeeToFinancialConfig : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(
            name: "HotmartFixedFeePerTransaction",
            table: "FinancialConfigs",
            type: "numeric(10,4)",
            precision: 10,
            scale: 4,
            nullable: false,
            defaultValue: 0.54m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "HotmartFixedFeePerTransaction",
            table: "FinancialConfigs");
    }
}
