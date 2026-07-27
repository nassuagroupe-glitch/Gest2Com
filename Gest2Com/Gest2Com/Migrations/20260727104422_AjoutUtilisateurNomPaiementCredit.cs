using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gest2Com.Migrations
{
    /// <inheritdoc />
    public partial class AjoutUtilisateurNomPaiementCredit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UtilisateurNom",
                table: "PaiementsCredit",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UtilisateurNom",
                table: "PaiementsCredit");
        }
    }
}
