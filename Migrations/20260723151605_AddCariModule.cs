using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GelirGiderPanel.Migrations
{
    /// <inheritdoc />
    public partial class AddCariModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CariAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OpeningBalance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CariAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CariTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CariAccountId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DocumentNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LinkedTransactionId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DebitAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreditAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CariTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CariTransactions_CariAccounts_CariAccountId",
                        column: x => x.CariAccountId,
                        principalTable: "CariAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "ID",
                keyValue: 1,
                column: "Password",
                value: "$2a$11$hYKZs.ekZNcF.ci6lINOceRAU/pTuwF8Q4e6NUSjHQyc//7BxD.CW");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "ID",
                keyValue: 2,
                column: "Password",
                value: "$2a$11$3PeYPvXd/dJ3m6XJOewKb.dJgP1z.JitWc7ZpMLnCp3NMp8cpjUiG");

            migrationBuilder.CreateIndex(
                name: "IX_CariAccounts_Name",
                table: "CariAccounts",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_CariTransactions_CariAccountId",
                table: "CariTransactions",
                column: "CariAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CariTransactions_Date",
                table: "CariTransactions",
                column: "Date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CariTransactions");

            migrationBuilder.DropTable(
                name: "CariAccounts");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "ID",
                keyValue: 1,
                column: "Password",
                value: "$2a$11$uaJhxwPkE/QHukPwinvc4uOLRYWfBkrsoH04Qxa24esUvkqDb2pqS");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "ID",
                keyValue: 2,
                column: "Password",
                value: "$2a$11$gWyBuJikbH.fvcbJe.c6be/GVBB6Mb5WVK08axXgj7S0L0RPx.tfC");
        }
    }
}
