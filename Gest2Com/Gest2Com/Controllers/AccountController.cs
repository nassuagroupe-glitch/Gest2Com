using Microsoft.AspNetCore.Mvc;
using Gest2Com.Repositories;

namespace Gest2Com.Controllers
{
    /// <summary>
    /// Controller d'authentification : session simple (pas d'ASP.NET Identity)
    /// pour rester cohérent avec l'auth locale utilisée dans le reste du projet.
    /// </summary>
    public class AccountController : Controller
    {
        private readonly AuthRepository _repository;
        public AccountController(AuthRepository repository) => _repository = repository;

        [HttpGet]
        public async Task<IActionResult> Login()
        {
            if (HttpContext.Session.GetInt32("UtilisateurId") != null)
                return RedirectToAction("Index", "Home");

            if (await _repository.NombreUtilisateursAsync() == 0)
                return RedirectToAction(nameof(PremierAdmin));

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string nomUtilisateur, string motDePasse)
        {
            var utilisateur = await _repository.ConnexionAsync(nomUtilisateur, motDePasse);
            if (utilisateur == null)
            {
                ViewBag.Erreur = "Identifiants incorrects";
                return View();
            }

            HttpContext.Session.SetInt32("UtilisateurId", utilisateur.Id);
            HttpContext.Session.SetString("UtilisateurNom", utilisateur.NomComplet);
            HttpContext.Session.SetString("UtilisateurRole", utilisateur.Role);

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Deconnexion()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Assistant de premier démarrage : tant qu'aucun utilisateur n'existe en base
        /// (nouvelle installation), permet de créer le tout premier compte, admin.
        /// Se désactive de lui-même dès qu'un compte existe.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> PremierAdmin()
        {
            if (await _repository.NombreUtilisateursAsync() > 0)
                return RedirectToAction(nameof(Login));

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PremierAdmin(string nomUtilisateur, string motDePasse, string confirmationMotDePasse, string nomComplet)
        {
            if (await _repository.NombreUtilisateursAsync() > 0)
                return RedirectToAction(nameof(Login));

            if (string.IsNullOrWhiteSpace(nomUtilisateur) || string.IsNullOrWhiteSpace(motDePasse) || string.IsNullOrWhiteSpace(nomComplet))
                ModelState.AddModelError("", "Tous les champs sont obligatoires");
            else if (motDePasse != confirmationMotDePasse)
                ModelState.AddModelError("", "Les mots de passe ne correspondent pas");

            if (!ModelState.IsValid) return View();

            var id = await _repository.CreerUtilisateurAsync(nomUtilisateur, motDePasse, nomComplet, "admin");

            HttpContext.Session.SetInt32("UtilisateurId", id);
            HttpContext.Session.SetString("UtilisateurNom", nomComplet);
            HttpContext.Session.SetString("UtilisateurRole", "admin");

            TempData["Succes"] = "Compte administrateur créé avec succès";
            return RedirectToAction("Index", "Home");
        }
    }
}
