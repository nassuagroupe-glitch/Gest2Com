using System.ComponentModel.DataAnnotations.Schema;

namespace Gest2Com.Models
{
    /// <summary>
    /// Versement effectué par un client pour rembourser une vente à crédit.
    /// Une vente à crédit peut recevoir plusieurs paiements successifs.
    /// </summary>
    public class PaiementCredit
    {
        public int Id { get; set; }

        public int VenteId { get; set; }
        public Vente? Vente { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal Montant { get; set; }

        public string ModePaiement { get; set; } = "Espèces";
        public DateTime DatePaiement { get; set; } = DateTime.Now;
        public string Notes { get; set; } = string.Empty;

        /// <summary>Utilisateur ayant encaissé ce versement (traçabilité de caisse).</summary>
        public string UtilisateurNom { get; set; } = string.Empty;
    }
}
