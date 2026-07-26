using Microsoft.AspNetCore.Mvc;
using Gest2Com.Filters;
using Gest2Com.Repositories;

namespace Gest2Com.Controllers
{
    /// <summary>
    /// Gestion des comptes utilisateurs et de leurs rôles. Réservé aux administrateurs.
    /// Pas d'ASP.NET Identity : contrôle d'accès basé sur la session (cf. AccountController).
    /// </summary>
    [RequireRole("admin")]
    public class UtilisateursController : Controller
    {
        private const string ROLE_ADMIN = "admin";
        private static readonly string[] ROLES_VALIDES = { "admin", "gerant", "vendeur" };

        private readonly AuthRepository _repository;
        public UtilisateursController(AuthRepository repository) => _repository = repository;

        public async Task<IActionResult> Index()
        {
            var utilisateurs = await _repository.ListerTousAsync();
            return View(utilisateurs);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string nomUtilisateur, string motDePasse, string nomComplet, string role)
        {
            if (string.IsNullOrWhiteSpace(nomUtilisateur) || string.IsNullOrWhiteSpace(motDePasse))
                ModelState.AddModelError("", "Le nom d'utilisateur et le mot de passe sont obligatoires");
            else if (await _repository.NomUtilisateurExisteAsync(nomUtilisateur))
                ModelState.AddModelError("", $"Le nom d'utilisateur \"{nomUtilisateur}\" est déjà pris");

            if (!ROLES_VALIDES.Contains(role))
                ModelState.AddModelError("", "Rôle invalide");

            if (!ModelState.IsValid) return View();

            await _repository.CreerUtilisateurAsync(nomUtilisateur, motDePasse, nomComplet, role);
            TempData["Succes"] = $"Utilisateur \"{nomUtilisateur}\" créé avec succès";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var utilisateur = await _repository.ParIdAsync(id);
            if (utilisateur == null) return NotFound();
            return View(utilisateur);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string nomComplet, string role)
        {
            var cible = await _repository.ParIdAsync(id);
            if (cible == null) return NotFound();

            if (!ROLES_VALIDES.Contains(role))
            {
                ModelState.AddModelError("", "Rôle invalide");
                return View(cible);
            }

            if (cible.Role == ROLE_ADMIN && role != ROLE_ADMIN && await _repository.NombreAdminsAsync() <= 1)
            {
                TempData["Erreur"] = "Impossible de retirer le rôle admin : c'est le dernier administrateur";
                return RedirectToAction(nameof(Index));
            }

            await _repository.ModifierRoleAsync(id, nomComplet, role);

            // Si l'admin modifie son propre compte, synchronise la session en cours
            if (HttpContext.Session.GetInt32("UtilisateurId") == id)
            {
                HttpContext.Session.SetString("UtilisateurNom", nomComplet);
                HttpContext.Session.SetString("UtilisateurRole", role);
            }

            TempData["Succes"] = "Utilisateur mis à jour";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (HttpContext.Session.GetInt32("UtilisateurId") == id)
            {
                TempData["Erreur"] = "Vous ne pouvez pas supprimer votre propre compte";
                return RedirectToAction(nameof(Index));
            }

            var cible = await _repository.ParIdAsync(id);
            if (cible == null) return NotFound();

            if (cible.Role == ROLE_ADMIN && await _repository.NombreAdminsAsync() <= 1)
            {
                TempData["Erreur"] = "Impossible de supprimer le dernier administrateur";
                return RedirectToAction(nameof(Index));
            }

            await _repository.SupprimerAsync(id);
            TempData["Succes"] = "Utilisateur supprimé";
            return RedirectToAction(nameof(Index));
        }
    }
}
