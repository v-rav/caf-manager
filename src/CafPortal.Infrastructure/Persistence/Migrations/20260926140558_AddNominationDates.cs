using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafPortal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNominationDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "ActualEndDate",
                table: "Nominations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ActualStartDate",
                table: "Nominations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ApprovalDate",
                table: "Nominations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "NominatedDate",
                table: "Nominations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PlannedEndDate",
                table: "Nominations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PlannedStartDate",
                table: "Nominations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalDays",
                table: "Nominations",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualEndDate",
                table: "Nominations");

            migrationBuilder.DropColumn(
                name: "ActualStartDate",
                table: "Nominations");

            migrationBuilder.DropColumn(
                name: "ApprovalDate",
                table: "Nominations");

            migrationBuilder.DropColumn(
                name: "NominatedDate",
                table: "Nominations");

            migrationBuilder.DropColumn(
                name: "PlannedEndDate",
                table: "Nominations");

            migrationBuilder.DropColumn(
                name: "PlannedStartDate",
                table: "Nominations");

            migrationBuilder.DropColumn(
                name: "TotalDays",
                table: "Nominations");
        }
    }
}
