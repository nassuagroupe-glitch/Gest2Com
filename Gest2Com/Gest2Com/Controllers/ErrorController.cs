using Microsoft.AspNetCore.Mvc;

namespace Gest2Com.Controllers
{
    /// <summary>
    /// Page d'erreur générique (cible de app.UseExceptionHandler). Séparée de HomeController
    /// pour rester accessible même sans session active (pas de [RequireConnexion] ici).
    /// </summary>
    public class ErrorController : Controller
    {
        [Route("/Home/Error")]
        public IActionResult Error() => View();
    }
}
