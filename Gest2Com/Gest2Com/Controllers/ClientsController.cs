using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Gest2Com.Filters;
using Gest2Com.Models;
using Gest2Com.Models.ViewModels;
using Gest2Com.Repositories;

namespace Gest2Com.Controllers
{
    [RequireConnexion]
    public class ClientsController : Controller
    {
        private const int JOURS_RETARD_PAR_DEFAUT = 30;

        private readonly ClientRepository _repository;
        private readonly VenteRepository _venteRepository;

        public ClientsController(ClientRepository repository, VenteRepository venteRepository)
        {
            _repository = repository;
            _venteRepository = venteRepository;
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
            return View(clients);
        }

        /// <summary>
        /// Liste des clients dont au moins une vente à crédit non soldée dépasse
        /// le délai indiqué, avec un message et un lien WhatsApp prêts à envoyer.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Relances(int jours = JOURS_RETARD_PAR_DEFAUT)
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
                    JoursDeRetard = (aujourdHui - g.Min(v => v.DateVente)).Days
                })
                .Where(m => m.JoursDeRetard >= jours)
                .OrderByDescending(m => m.JoursDeRetard)
                .ToList();

            foreach (var relance in relances)
            {
                relance.Message = $"Bonjour {relance.Nom}, ceci est un rappel concernant votre solde de " +
                    $"{relance.SoldeCredit:N0} F chez nous, en attente depuis {relance.JoursDeRetard} jour(s). " +
                    "Merci de passer régulariser votre compte dès que possible. Cordialement.";

                var telephoneNettoye = Regex.Replace(relance.Telephone, "[^0-9]", "");
                relance.LienWhatsApp = string.IsNullOrEmpty(telephoneNettoye)
                    ? null
                    : $"https://wa.me/{telephoneNettoye}?text={Uri.EscapeDataString(relance.Message)}";
            }

            ViewData["JoursSeuil"] = jours;
            return View(relances);
        }
    }
}
