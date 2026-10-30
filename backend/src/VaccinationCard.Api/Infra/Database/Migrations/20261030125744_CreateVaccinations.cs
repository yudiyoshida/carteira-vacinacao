using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VaccinationCard.Api.Infra.Database.Migrations
{
    /// <inheritdoc />
    public partial class CreateVaccinations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vaccinations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vaccine_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dose_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    dose_number = table.Column<short>(type: "smallint", nullable: false),
                    applied_on = table.Column<DateOnly>(type: "date", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vaccinations", x => x.id);
                    table.ForeignKey(
                        name: "fk_vaccinations_people_person_id",
                        column: x => x.person_id,
                        principalTable: "people",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_vaccinations_vaccines_vaccine_id",
                        column: x => x.vaccine_id,
                        principalTable: "vaccines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_vaccinations_vaccine_id",
                table: "vaccinations",
                column: "vaccine_id");

            migrationBuilder.CreateIndex(
                name: "ux_vaccinations_person_vaccine_dose",
                table: "vaccinations",
                columns: new[] { "person_id", "vaccine_id", "dose_type", "dose_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vaccinations");
        }
    }
}
