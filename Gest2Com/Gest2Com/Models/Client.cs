using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gest2Com.Models
{
    /// <summary>
    /// Client de la boutique. Le suivi du crédit se fait via SoldeCredit :
    /// montant total actuellement dû par ce client sur ses ventes à crédit.
    /// </summary>
    public class Client
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire")]
        public string Nom { get; set; } = string.Empty;

        public string Telephone { get; set; } = string.Empty;
        public string Adresse { get; set; } = string.Empty;

        [Column(TypeName = "decimal(12,2)")]
        public decimal LimiteCredit { get; set; } = 0;

        [Column(TypeName = "decimal(12,2)")]
        public decimal SoldeCredit { get; set; } = 0;   // ce que le client doit actuellement

        public bool PeutEmprunter(decimal montant) => SoldeCredit + montant <= LimiteCredit;

        /// <summary>Date de la dernière relance de crédit envoyée à ce client (WhatsApp).</summary>
        public DateTime? DateDerniereRelance { get; set; }

        public List<Vente> Ventes { get; set; } = new();
    }
}
