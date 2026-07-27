using Microsoft.AspNetCore.Mvc;
using Gest2Com.Filters;
using Gest2Com.Models;
using Gest2Com.Models.ViewModels;
using Gest2Com.Repositories;
using Gest2Com.Services;

namespace Gest2Com.Controllers
{
    [RequireConnexion]
    public class ClientsController : Controller
    {
        private readonly ClientRepository _repository;
        private readonly VenteRepository _venteRepository;
        private readonly RelanceCreditService _relanceService;
        private readonly IWhatsAppSender _whatsAppSender;

        public ClientsController(
            ClientRepository repository,
            VenteRepository venteRepository,
            RelanceCreditService relanceService,
            IWhatsAppSender whatsAppSender)
        {
            _repository = repository;
            _venteRepository = venteRepository;
            _relanceService = relanceService;
            _whatsAppSender = whatsAppSender;
        }

        public async Task<IActionResult> Index(string? q)
        {
            var clients = await _repository.ListerTousAsync(q);
            ViewData["Recherche"] = q;
            return View(clients);
        }

        [HttpGet]
        [RequireRole("admin", "gerant")]
        public IActionResult Create() => View(new Client());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("admin", "gerant")]
        public async Task<IActionResult> Create(Client client)
        {
            if (!ModelState.IsValid) return View(client);

            await _repository.AjouterAsync(client);
            TempData["Succes"] = $"Client \"{client.Nom}\" créé avec succès";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [RequireRole("admin", "gerant")]
        public async Task<IActionResult> Edit(int id)
        {
            var client = await _repository.ParIdAsync(id);
            if (client == null) return NotFound();
            return View(client);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("admin", "gerant")]
        public async Task<IActionResult> Edit(int id, Client client)
        {
            if (id != client.Id) return BadRequest();
            if (!ModelState.IsValid) return View(client);

            await _repository.ModifierAsync(client);
            TempData["Succes"] = "Client mis à jour";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("admin", "gerant")]
        public async Task<IActionResult> Delete(int id)
        {
            var supprime = await _repository.SupprimerAsync(id);
            if (!supprime)
            {
                TempData["Erreur"] = "Impossible de supprimer ce client : il a des ventes enregistrées à son nom.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Succes"] = "Client supprimé";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Credits()
        {
            var clients = await _repository.AvecCreditEnCoursAsync();
            var ventesEnCours = await _venteRepository.ListerCreditsEnCoursAsync();
            var compteParClient = ventesEnCours
                .Where(v => v.ClientId != null)
                .GroupBy(v => v.ClientId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            var modele = clients.Select(c => new ClientCreditViewModel
            {
                ClientId = c.Id,
                Nom = c.Nom,
                Telephone = c.Telephone,
                SoldeCredit = c.SoldeCredit,
                LimiteCredit = c.LimiteCredit,
                NombreVentesEnCours = compteParClient.GetValueOrDefault(c.Id)
            }).ToList();

            return View(modele);
        }

        /// <summary>
        /// Liste des clients dont au moins une vente à crédit non soldée dépasse
        /// le délai indiqué, avec un message et un lien WhatsApp prêts à envoyer.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Relances(int jours = RelanceCreditService.JOURS_RETARD_PAR_DEFAUT)
        {
            var relances = await _relanceService.ObtenirRelancesEligiblesAsync(jours);
            ViewData["JoursSeuil"] = jours;
            return View(relances);
        }

        /// <summary>
        /// Envoie la relance WhatsApp au client via Twilio et enregistre la date de
        /// relance (uniquement en cas de succès), pour garder trace des relances déjà envoyées.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnvoyerRelance(int clientId)
        {
            var client = await _repository.ParIdAsync(clientId);
            if (client == null) return NotFound();

            var venteLaPlusAncienne = (await _venteRepository.ListerCreditsEnCoursAsync())
                .Where(v => v.ClientId == clientId)
                .OrderBy(v => v.DateVente)
                .FirstOrDefault();
            var joursDeRetard = venteLaPlusAncienne != null
                ? (DateTime.Today - venteLaPlusAncienne.DateVente).Days
                : 0;

            var message = RelanceCreditService.ConstruireMessage(client.Nom, client.SoldeCredit, joursDeRetard);
            var (succes, erreur) = await _whatsAppSender.EnvoyerAsync(client.Telephone, message);

            if (!succes)
            {
                TempData["Erreur"] = $"Échec de l'envoi WhatsApp à {client.Nom} : {erreur}";
                return RedirectToAction(nameof(Relances));
            }

            await _repository.EnregistrerRelanceAsync(clientId);
            TempData["Succes"] = $"Relance envoyée à {client.Nom}";
            return RedirectToAction(nameof(Relances));
        }
    }
}
