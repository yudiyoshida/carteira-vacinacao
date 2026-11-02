using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaccinationCard.Api.Infra.Database.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeVaccineNamesWithoutAccents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The same value as VaccineDao.Normalize for the names already stored, which the registration trims.
            migrationBuilder.Sql("UPDATE vaccines SET normalized_name = lower(unaccent(name));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE vaccines SET normalized_name = lower(name);");
        }
    }
}
