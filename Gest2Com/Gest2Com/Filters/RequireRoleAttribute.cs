using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Gest2Com.Filters
{
    /// <summary>
    /// Exige une session active ET un rôle parmi ceux autorisés. Redirige vers le login
    /// si non connecté (même comportement que RequireConnexionAttribute), ou vers le
    /// tableau de bord avec un message d'erreur si le rôle connecté n'est pas autorisé.
    /// </summary>
    public class RequireRoleAttribute : ActionFilterAttribute
    {
        private readonly string[] _rolesAutorises;

        public RequireRoleAttribute(params string[] rolesAutorises) => _rolesAutorises = rolesAutorises;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;

            if (session.GetInt32("UtilisateurId") == null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            var role = session.GetString("UtilisateurRole");
            if (role == null || !_rolesAutorises.Contains(role))
            {
                if (context.Controller is Controller controller)
                    controller.TempData["Erreur"] = "Accès réservé aux rôles : " + string.Join(", ", _rolesAutorises);

                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
        }
    }
}
