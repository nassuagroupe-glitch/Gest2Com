using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gest2Com.Models
{
    /// <summary>
    /// Article du catalogue et du stock.
    /// </summary>
    public class Produit
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire")]
        public string Nom { get; set; } = string.Empty;

        public string Reference { get; set; } = string.Empty;
        public string Categorie { get; set; } = string.Empty;

        [Column(TypeName = "decimal(12,2)")]
        public decimal PrixAchat { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Le prix de vente doit être supérieur à zéro")]
        [Column(TypeName = "decimal(12,2)")]
        public decimal PrixVente { get; set; }

        public int QuantiteStock { get; set; }
        public int SeuilAlerte { get; set; } = 5;

        public bool EstEnAlerte => QuantiteStock <= SeuilAlerte;

        public List<LigneVente> LignesVente { get; set; } = new();
    }
}
