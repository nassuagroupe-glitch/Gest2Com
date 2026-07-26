namespace Gest2Com.Models
{
    /// <summary>
    /// Historique des mouvements de stock (entrée / sortie), pour la traçabilité.
    /// </summary>
    public class MouvementStock
    {
        public const string TYPE_ENTREE = "Entree";
        public const string TYPE_SORTIE = "Sortie";

        public int Id { get; set; }
        public int ProduitId { get; set; }
        public Produit? Produit { get; set; }

        public string Type { get; set; } = TYPE_ENTREE;
        public int Quantite { get; set; }
        public string Motif { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;
    }
}
