using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GelirGiderPanel.Migrations
{
    /// <inheritdoc />
    public partial class logs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoginLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    LoginTime = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginLogs", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "ID",
                keyValue: 1,
                column: "Password",
                value: "$2a$11$kHA2YVlA2LQPpLw4oOQiAuQ.k9Ki1UqH2UGPAKtg5Ce/Sr6Ouw5YW");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "ID",
                keyValue: 2,
                column: "Password",
                value: "$2a$11$E9qe9e0WG9CPu1A7o27EaOYGgL2CoxgV/iOLoHx59rzZGr1RMi0ei");

            migrationBuilder.CreateIndex(
                name: "IX_LoginLogs_LoginTime",
                table: "LoginLogs",
                column: "LoginTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginLogs");

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
        }
    }
}
