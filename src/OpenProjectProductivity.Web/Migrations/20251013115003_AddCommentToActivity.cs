using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenProjectProductivity.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentToActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Activities");

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FromStatus",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToStatus",
                table: "Activities",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Comment",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "FromStatus",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "ToStatus",
                table: "Activities");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Activities",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
