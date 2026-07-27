namespace Gest2Com.Models.ViewModels
{
    /// <summary>
    /// Client ayant un solde de crédit en cours, avec le nombre de ventes à
    /// crédit non soldées associées (pour lien direct vers son historique).
    /// </summary>
    public class ClientCreditViewModel
    {
        public int ClientId { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public decimal SoldeCredit { get; set; }
        public decimal LimiteCredit { get; set; }
        public int NombreVentesEnCours { get; set; }
    }
}
