using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaccinationCard.Api.Infra.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "version",
                table: "people",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "version",
                table: "people");
        }
    }
}
