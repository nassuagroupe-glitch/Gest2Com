using System.ComponentModel.DataAnnotations.Schema;

namespace Gest2Com.Models
{
    /// <summary>
    /// Une ligne (un produit + quantité) au sein d'une vente.
    /// </summary>
    public class LigneVente
    {
        public int Id { get; set; }

        public int VenteId { get; set; }
        public Vente? Vente { get; set; }

        public int ProduitId { get; set; }
        public Produit? Produit { get; set; }

        public string NomProduit { get; set; } = string.Empty;   // copie figée au moment de la vente
        public int Quantite { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal PrixUnitaire { get; set; }

        public decimal SousTotal() => Quantite * PrixUnitaire;
    }
}
