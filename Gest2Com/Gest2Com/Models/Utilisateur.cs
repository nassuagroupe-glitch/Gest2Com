namespace Gest2Com.Models
{
    /// <summary>
    /// Utilisateur de l'application (vendeur, gérant, admin).
    /// Authentification locale (mot de passe hashé), gérée via session.
    /// </summary>
    public class Utilisateur
    {
        public int Id { get; set; }
        public string NomUtilisateur { get; set; } = string.Empty;
        public string MotDePasseHash { get; set; } = string.Empty;
        public string NomComplet { get; set; } = string.Empty;
        public string Role { get; set; } = "vendeur";  // "admin", "gerant", "vendeur"
    }
}
