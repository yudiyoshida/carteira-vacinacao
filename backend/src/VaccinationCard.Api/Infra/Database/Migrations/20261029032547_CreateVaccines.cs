using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaccinationCard.Api.Infra.Database.Migrations
{
    /// <inheritdoc />
    public partial class CreateVaccines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:unaccent", ",,");

            migrationBuilder.CreateTable(
                name: "vaccines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    primary_doses = table.Column<short>(type: "smallint", nullable: false),
                    booster_doses = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vaccines", x => x.id);
                    table.CheckConstraint("ck_vaccines_booster_doses", "booster_doses BETWEEN 0 AND 5");
                    table.CheckConstraint("ck_vaccines_primary_doses", "primary_doses BETWEEN 1 AND 10");
                });

            migrationBuilder.CreateIndex(
                name: "ux_vaccines_normalized_name",
                table: "vaccines",
                column: "normalized_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vaccines");
        }
    }
}
