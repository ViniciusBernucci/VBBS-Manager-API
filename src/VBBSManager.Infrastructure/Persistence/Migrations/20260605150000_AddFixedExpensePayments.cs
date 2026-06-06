using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VBBSManager.Infrastructure.Persistence.Migrations;

public partial class AddFixedExpensePayments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FixedExpensePayments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                FixedExpenseId = table.Column<Guid>(type: "uuid", nullable: false),
                Year = table.Column<int>(type: "integer", nullable: false),
                Month = table.Column<int>(type: "integer", nullable: false),
                CashFlowTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FixedExpensePayments", x => x.Id);

                table.ForeignKey(
                    name: "FK_FixedExpensePayments_FixedExpenses_FixedExpenseId",
                    column: x => x.FixedExpenseId,
                    principalTable: "FixedExpenses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);

                table.ForeignKey(
                    name: "FK_FixedExpensePayments_CashFlowTransactions_CashFlowTransactionId",
                    column: x => x.CashFlowTransactionId,
                    principalTable: "CashFlowTransactions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_FixedExpensePayments_FixedExpenseId",
            table: "FixedExpensePayments",
            column: "FixedExpenseId");

        migrationBuilder.CreateIndex(
            name: "IX_FixedExpensePayments_CashFlowTransactionId",
            table: "FixedExpensePayments",
            column: "CashFlowTransactionId");

        migrationBuilder.CreateIndex(
            name: "IX_FixedExpensePayments_TenantId_FixedExpenseId_Year_Month",
            table: "FixedExpensePayments",
            columns: new[] { "TenantId", "FixedExpenseId", "Year", "Month" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "FixedExpensePayments");
    }
}
