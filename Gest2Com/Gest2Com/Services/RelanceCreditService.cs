using Gest2Com.Models.ViewModels;
using Gest2Com.Repositories;
using Gest2Com.Utils;

namespace Gest2Com.Services
{
    /// <summary>
    /// Détermine quels clients ont un crédit en retard et doivent être relancés,
    /// et prépare le message de relance associé.
    /// </summary>
    public class RelanceCreditService
    {
        public const int JOURS_RETARD_PAR_DEFAUT = 30;
        public const int JOURS_COOLDOWN_AUTO = 7;

        private readonly VenteRepository _venteRepository;

        public RelanceCreditService(VenteRepository venteRepository)
        {
            _venteRepository = venteRepository;
        }

        public async Task<List<ClientRelanceViewModel>> ObtenirRelancesEligiblesAsync(int joursSeuil = JOURS_RETARD_PAR_DEFAUT)
        {
            var aujourdHui = DateTime.Today;
            var ventesEnCours = await _venteRepository.ListerCreditsEnCoursAsync();

            var relances = ventesEnCours
                .Where(v => v.Client != null)
                .GroupBy(v => v.Client!)
                .Select(g => new ClientRelanceViewModel
                {
                    ClientId = g.Key.Id,
                    Nom = g.Key.Nom,
                    Telephone = g.Key.Telephone,
                    SoldeCredit = g.Key.SoldeCredit,
                    LimiteCredit = g.Key.LimiteCredit,
                    DateCreditLaPlusAncien = g.Min(v => v.DateVente),
                    NombreVentesEnCours = g.Count(),
                    JoursDeRetard = (aujourdHui - g.Min(v => v.DateVente)).Days,
                    DateDerniereRelance = g.Key.DateDerniereRelance
                })
                .Where(m => m.JoursDeRetard >= joursSeuil)
                .OrderByDescending(m => m.JoursDeRetard)
                .ToList();

            foreach (var relance in relances)
            {
                relance.Message = ConstruireMessage(relance.Nom, relance.SoldeCredit, relance.JoursDeRetard);
                relance.LienWhatsApp = ConstruireLienWhatsApp(relance.Telephone, relance.Message);
            }

            return relances;
        }

        /// <summary>Éligible à une relance automatique si jamais relancé, ou pas depuis JOURS_COOLDOWN_AUTO jours.</summary>
        public static bool EstEligibleEnvoiAuto(ClientRelanceViewModel relance) =>
            relance.DateDerniereRelance == null || (DateTime.Now - relance.DateDerniereRelance.Value).Days >= JOURS_COOLDOWN_AUTO;

        public static string ConstruireMessage(string nom, decimal soldeCredit, int joursDeRetard) =>
            $"Bonjour {nom}, ceci est un rappel concernant votre solde de " +
            $"{soldeCredit:N0} F chez nous, en attente depuis {joursDeRetard} jour(s). " +
            "Merci de passer régulariser votre compte dès que possible. Cordialement.";

        /// <summary>Lien wa.me de secours (envoi manuel) si l'envoi automatique Twilio échoue.</summary>
        public static string? ConstruireLienWhatsApp(string telephone, string message)
        {
            var telephoneE164 = Telephone.VersE164(telephone);
            return telephoneE164 == null
                ? null
                : $"https://wa.me/{telephoneE164.TrimStart('+')}?text={Uri.EscapeDataString(message)}";
        }
    }
}
