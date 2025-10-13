using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenProjectProductivity.Web.Migrations
{
    /// <inheritdoc />
    public partial class FixActivityUserRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activities_Users_UserId1",
                table: "Activities");

            migrationBuilder.DropIndex(
                name: "IX_Activities_UserId1",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Activities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId1",
                table: "Activities",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Activities_UserId1",
                table: "Activities",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Activities_Users_UserId1",
                table: "Activities",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
