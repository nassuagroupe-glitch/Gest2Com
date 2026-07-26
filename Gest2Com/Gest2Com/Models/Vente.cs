using System.ComponentModel.DataAnnotations.Schema;

namespace Gest2Com.Models
{
    /// <summary>
    /// Une vente, en espèces ou à crédit. Collection de LigneVente.
    /// </summary>
    public class Vente
    {
        public const string TYPE_ESPECES = "Especes";
        public const string TYPE_CREDIT = "Credit";

        public const string STATUT_PAYEE = "Payee";
        public const string STATUT_PARTIELLE = "PartiellementPayee";
        public const string STATUT_IMPAYEE = "Impayee";

        public int Id { get; set; }
        public string Numero { get; set; } = string.Empty;

        public int? ClientId { get; set; }
        public Client? Client { get; set; }

        public string TypeVente { get; set; } = TYPE_ESPECES;

        [Column(TypeName = "decimal(12,2)")]
        public decimal MontantTotal { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal MontantPaye { get; set; }

        public string Statut { get; set; } = STATUT_PAYEE;
        public string VendeurNom { get; set; } = string.Empty;
        public DateTime DateVente { get; set; } = DateTime.Now;

        public List<LigneVente> Lignes { get; set; } = new();
        public List<PaiementCredit> Paiements { get; set; } = new();

        [NotMapped]
        public decimal MontantRestant => MontantTotal - MontantPaye;

        public decimal CalculerTotal() => Lignes.Sum(l => l.SousTotal());
    }
}
