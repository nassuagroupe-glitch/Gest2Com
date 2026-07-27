using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gest2Com.Migrations
{
    /// <inheritdoc />
    public partial class AjoutDateDerniereRelanceClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateDerniereRelance",
                table: "Clients",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateDerniereRelance",
                table: "Clients");
        }
    }
}
