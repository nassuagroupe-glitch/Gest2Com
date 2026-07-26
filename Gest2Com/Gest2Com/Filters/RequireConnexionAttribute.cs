using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Gest2Com.Filters
{
    /// <summary>
    /// Exige une session active (utilisateur connecté). Pas d'ASP.NET Identity dans ce
    /// projet : l'auth est locale et suivie via la session (cf. AccountController), donc
    /// ce filtre remplace [Authorize] pour rester cohérent avec cette approche.
    /// </summary>
    public class RequireConnexionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.Session.GetInt32("UtilisateurId") == null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }
        }
    }
}
