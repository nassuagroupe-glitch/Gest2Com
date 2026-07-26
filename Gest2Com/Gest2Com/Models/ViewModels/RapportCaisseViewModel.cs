namespace Gest2Com.Models.ViewModels
{
    /// <summary>
    /// Rapport de caisse sur une période : distingue le chiffre d'affaires généré
    /// (ventes créées dans la période) de l'encaissement réel (cash effectivement
    /// entré en caisse, y compris les versements sur des ventes à crédit plus anciennes).
    /// </summary>
    public class RapportCaisseViewModel
    {
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }

        public int NombreVentesEspeces { get; set; }
        public decimal CAEspeces { get; set; }

        public int NombreVentesCredit { get; set; }
        public decimal CACredit { get; set; }

        public decimal CATotal => CAEspeces + CACredit;
        public int NombreVentesTotal => NombreVentesEspeces + NombreVentesCredit;

        public decimal EncaisseEspeces { get; set; }
        public decimal EncaisseVersementsCredit { get; set; }
        public decimal EncaisseTotal => EncaisseEspeces + EncaisseVersementsCredit;

        public List<Vente> Ventes { get; set; } = new();
    }
}
