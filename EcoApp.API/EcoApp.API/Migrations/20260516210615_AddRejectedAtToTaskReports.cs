using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoApp.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRejectedAtToTaskReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "TaskReports",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "TaskReports");
        }
    }
}
