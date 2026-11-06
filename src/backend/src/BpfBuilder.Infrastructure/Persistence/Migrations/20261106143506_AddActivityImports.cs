using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BpfBuilder.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityImports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityImports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FiscalYearId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ImportedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityImports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityImports_FiscalYears_FiscalYearId",
                        column: x => x.FiscalYearId,
                        principalTable: "FiscalYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportedCosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ActivityImportId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedCosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedCosts_ActivityImports_ActivityImportId",
                        column: x => x.ActivityImportId,
                        principalTable: "ActivityImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportedLearners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LearnerCode = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SessionCode = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    HoursAttended = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    ActivityImportId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedLearners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedLearners_ActivityImports_ActivityImportId",
                        column: x => x.ActivityImportId,
                        principalTable: "ActivityImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportedRevenue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FormLine = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    SessionCode = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Label = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ActivityImportId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedRevenue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedRevenue_ActivityImports_ActivityImportId",
                        column: x => x.ActivityImportId,
                        principalTable: "ActivityImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportedSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PlannedHours = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    DeliveryMode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    DistanceLearning = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    TrainingGoal = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    RncpLevel = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    NsfCode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    ActivityImportId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedSessions_ActivityImports_ActivityImportId",
                        column: x => x.ActivityImportId,
                        principalTable: "ActivityImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ImportedTrainers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TrainerCode = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SessionCode = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    HoursDelivered = table.Column<decimal>(type: "TEXT", precision: 14, scale: 2, nullable: false),
                    ActivityImportId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportedTrainers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImportedTrainers_ActivityImports_ActivityImportId",
                        column: x => x.ActivityImportId,
                        principalTable: "ActivityImports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityImports_FiscalYearId",
                table: "ActivityImports",
                column: "FiscalYearId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportedCosts_ActivityImportId",
                table: "ImportedCosts",
                column: "ActivityImportId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedLearners_ActivityImportId",
                table: "ImportedLearners",
                column: "ActivityImportId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedRevenue_ActivityImportId",
                table: "ImportedRevenue",
                column: "ActivityImportId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedSessions_ActivityImportId",
                table: "ImportedSessions",
                column: "ActivityImportId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedTrainers_ActivityImportId",
                table: "ImportedTrainers",
                column: "ActivityImportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportedCosts");

            migrationBuilder.DropTable(
                name: "ImportedLearners");

            migrationBuilder.DropTable(
                name: "ImportedRevenue");

            migrationBuilder.DropTable(
                name: "ImportedSessions");

            migrationBuilder.DropTable(
                name: "ImportedTrainers");

            migrationBuilder.DropTable(
                name: "ActivityImports");
        }
    }
}
