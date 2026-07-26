using Microsoft.AspNetCore.Mvc;
using Gest2Com.Filters;
using Gest2Com.Models.ViewModels;
using Gest2Com.Repositories;

namespace Gest2Com.Controllers
{
    /// <summary>
    /// Controller du tableau de bord : synthèse du jour (CA, crédits en cours,
    /// alertes de stock). Reçoit la requête, interroge les Repositories (Model),
    /// renvoie une Vue avec les données.
    /// </summary>
    [RequireConnexion]
    public class HomeController : Controller
    {
        private readonly ProduitRepository _produitRepository;
        private readonly VenteRepository _venteRepository;
        private readonly ClientRepository _clientRepository;

        public HomeController(ProduitRepository produitRepository, VenteRepository venteRepository,
            ClientRepository clientRepository)
        {
            _produitRepository = produitRepository;
            _venteRepository = venteRepository;
            _clientRepository = clientRepository;
        }

        public async Task<IActionResult> Index()
        {
            var aujourdHui = DateTime.Today;
            var debutMois = new DateTime(aujourdHui.Year, aujourdHui.Month, 1);

            var clientsAvecCredit = await _clientRepository.AvecCreditEnCoursAsync();

            var modele = new TableauBordViewModel
            {
                ChiffreAffairesJour = await _venteRepository.ChiffreAffairesPeriodeAsync(aujourdHui, aujourdHui.AddDays(1).AddSeconds(-1)),
                ChiffreAffairesMois = await _venteRepository.ChiffreAffairesPeriodeAsync(debutMois, aujourdHui.AddDays(1).AddSeconds(-1)),
                TotalCreditsEnCours = clientsAvecCredit.Sum(c => c.SoldeCredit),
                ProduitsEnAlerte = await _produitRepository.EnAlerteAsync(),
                DernieresVentes = (await _venteRepository.ListerToutesAsync()).Take(10).ToList()
            };

            return View(modele);
        }
    }
}
