using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class DiagnosePending : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "IdentityRole",
                keyColumn: "Id",
                keyValue: "07094013-e51f-47ff-af6e-9257ad663277",
                column: "ConcurrencyStamp",
                value: "af3e1ef0-8ab3-4839-9c77-8913db0e8349");

            migrationBuilder.UpdateData(
                table: "IdentityRole",
                keyColumn: "Id",
                keyValue: "7f7b6805-0ec9-4dff-a539-0251c8efcefc",
                column: "ConcurrencyStamp",
                value: "5708d3f2-edeb-42ca-b80e-1e995371a8c7");

            migrationBuilder.UpdateData(
                table: "IdentityRole",
                keyColumn: "Id",
                keyValue: "bf4f4642-3251-40cf-98b1-aeff812658b1",
                column: "ConcurrencyStamp",
                value: "dc4683f9-9247-4127-b57d-c952d85c3f74");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "IdentityRole",
                keyColumn: "Id",
                keyValue: "07094013-e51f-47ff-af6e-9257ad663277",
                column: "ConcurrencyStamp",
                value: "8c448de7-b1ca-42d8-b8d2-1f953e26b8a4");

            migrationBuilder.UpdateData(
                table: "IdentityRole",
                keyColumn: "Id",
                keyValue: "7f7b6805-0ec9-4dff-a539-0251c8efcefc",
                column: "ConcurrencyStamp",
                value: "9a9686a9-aa2c-4aa0-9197-d4440a31a01a");

            migrationBuilder.UpdateData(
                table: "IdentityRole",
                keyColumn: "Id",
                keyValue: "bf4f4642-3251-40cf-98b1-aeff812658b1",
                column: "ConcurrencyStamp",
                value: "39994b62-d8cd-455d-9abc-fd4384a47cf0");
        }
    }
}
