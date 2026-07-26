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
        public IActionResult Login() => View();

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
    }
}
