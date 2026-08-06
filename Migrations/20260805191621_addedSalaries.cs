using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GelirGiderPanel.Migrations
{
    /// <inheritdoc />
    public partial class addedSalaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Salaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Salaries", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "ID",
                keyValue: 1,
                column: "Password",
                value: "$2a$11$aBdXT3vN5cg8y479WLs54.TEolsex3bXukEczU3/t/YA3qCW9MVKW");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "ID",
                keyValue: 2,
                column: "Password",
                value: "$2a$11$F533/P0XAgWeSdCkThM9NO3FpouJJS/RaXNnc7GiNfN1MP37RNcR.");

            migrationBuilder.CreateIndex(
                name: "IX_Salaries_Name",
                table: "Salaries",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Salaries");

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
        }
    }
}
