using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gest2Com.Migrations
{
    /// <inheritdoc />
    public partial class AjoutImageProduit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageNomFichier",
                table: "Produits",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageNomFichier",
                table: "Produits");
        }
    }
}
