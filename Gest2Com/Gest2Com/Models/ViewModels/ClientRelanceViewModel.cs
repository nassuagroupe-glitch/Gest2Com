namespace Gest2Com.Models.ViewModels
{
    /// <summary>
    /// Client dont au moins une vente à crédit non soldée dépasse le délai de
    /// relance configuré. Porte le message et le lien WhatsApp prêts à l'emploi
    /// pour que le gérant relance le client en un clic.
    /// </summary>
    public class ClientRelanceViewModel
    {
        public int ClientId { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public decimal SoldeCredit { get; set; }
        public decimal LimiteCredit { get; set; }
        public DateTime DateCreditLaPlusAncien { get; set; }
        public int NombreVentesEnCours { get; set; }
        public int JoursDeRetard { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? LienWhatsApp { get; set; }
    }
}
