using Gest2Com.Models;

namespace Gest2Com.Models.ViewModels
{
    public class TableauBordViewModel
    {
        public decimal ChiffreAffairesJour { get; set; }
        public decimal ChiffreAffairesMois { get; set; }
        public decimal TotalCreditsEnCours { get; set; }
        public List<Produit> ProduitsEnAlerte { get; set; } = new();
        public List<Vente> DernieresVentes { get; set; } = new();
    }
}
