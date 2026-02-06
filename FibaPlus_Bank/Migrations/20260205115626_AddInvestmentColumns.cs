using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FibaPlus_Bank.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestmentColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Symbol",
                table: "InvestmentTransactions");

            migrationBuilder.RenameColumn(
                name: "InvestmentTransactionId",
                table: "InvestmentTransactions",
                newName: "Id");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionType",
                table: "InvestmentTransactions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "InvestmentType",
                table: "InvestmentTransactions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "InstrumentCode",
                table: "InvestmentTransactions",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "CurrencyCode",
                table: "Investments",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true,
                defaultValue: "TRY",
                oldClrType: typeof(string),
                oldType: "nvarchar(5)",
                oldMaxLength: 5,
                oldDefaultValue: "TRY");

            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "Investments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Investments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_Investments_AccountId",
                table: "Investments",
                column: "AccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Investments_Accounts_AccountId",
                table: "Investments",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Investments_Accounts_AccountId",
                table: "Investments");

            migrationBuilder.DropIndex(
                name: "IX_Investments_AccountId",
                table: "Investments");

            migrationBuilder.DropColumn(
                name: "InstrumentCode",
                table: "InvestmentTransactions");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Investments");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Investments");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "InvestmentTransactions",
                newName: "InvestmentTransactionId");

            migrationBuilder.AlterColumn<string>(
                name: "TransactionType",
                table: "InvestmentTransactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InvestmentType",
                table: "InvestmentTransactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Symbol",
                table: "InvestmentTransactions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "CurrencyCode",
                table: "Investments",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "TRY",
                oldClrType: typeof(string),
                oldType: "nvarchar(5)",
                oldMaxLength: 5,
                oldNullable: true,
                oldDefaultValue: "TRY");
        }
    }
}
