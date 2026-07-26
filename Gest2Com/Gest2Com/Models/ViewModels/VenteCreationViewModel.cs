using System.ComponentModel.DataAnnotations;
using Gest2Com.Models;

namespace Gest2Com.Models.ViewModels
{
    /// <summary>
    /// ViewModel utilisé pour l'écran de création d'une vente :
    /// regroupe les données du formulaire + les listes déroulantes nécessaires.
    /// </summary>
    public class VenteCreationViewModel
    {
        public int? ClientId { get; set; }

        [Required]
        public string TypeVente { get; set; } = Vente.TYPE_ESPECES;

        public decimal MontantVerseImmediatement { get; set; }

        public List<LigneVenteInput> Lignes { get; set; } = new();

        public List<Produit> ProduitsDisponibles { get; set; } = new();
        public List<Client> ClientsDisponibles { get; set; } = new();
    }

    public class LigneVenteInput
    {
        public int ProduitId { get; set; }
        public int Quantite { get; set; }
    }
}
