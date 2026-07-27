using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Gest2Com.Data;
using Gest2Com.Models;

namespace Gest2Com.Repositories
{
    /// <summary>
    /// Authentification locale : mot de passe hashé SHA-256, jamais en clair.
    /// </summary>
    public class AuthRepository
    {
        private readonly AppDbContext _db;
        public AuthRepository(AppDbContext db) => _db = db;

        private static string Hacher(string motDePasse)
        {
            using var sha256 = SHA256.Create();
            var octets = sha256.ComputeHash(Encoding.UTF8.GetBytes(motDePasse));
            return Convert.ToBase64String(octets);
        }

        public async Task<Utilisateur?> ConnexionAsync(string nomUtilisateur, string motDePasse)
        {
            var hash = Hacher(motDePasse);
            return await _db.Utilisateurs.FirstOrDefaultAsync(
                u => u.NomUtilisateur == nomUtilisateur && u.MotDePasseHash == hash);
        }

        public async Task<int> CreerUtilisateurAsync(string nomUtilisateur, string motDePasse, string nomComplet, string role)
        {
            var utilisateur = new Utilisateur
            {
                NomUtilisateur = nomUtilisateur,
                MotDePasseHash = Hacher(motDePasse),
                NomComplet = nomComplet,
                Role = role
            };
            _db.Utilisateurs.Add(utilisateur);
            await _db.SaveChangesAsync();
            return utilisateur.Id;
        }

        public async Task<List<Utilisateur>> ListerTousAsync() =>
            await _db.Utilisateurs.OrderBy(u => u.NomUtilisateur).ToListAsync();

        public async Task<Utilisateur?> ParIdAsync(int id) => await _db.Utilisateurs.FindAsync(id);

        public async Task<bool> NomUtilisateurExisteAsync(string nomUtilisateur) =>
            await _db.Utilisateurs.AnyAsync(u => u.NomUtilisateur == nomUtilisateur);

        public async Task<int> NombreAdminsAsync() =>
            await _db.Utilisateurs.CountAsync(u => u.Role == "admin");

        public async Task<int> NombreUtilisateursAsync() =>
            await _db.Utilisateurs.CountAsync();

        public async Task ModifierRoleAsync(int id, string nomComplet, string role)
        {
            var utilisateur = await _db.Utilisateurs.FindAsync(id);
            if (utilisateur == null) return;
            utilisateur.NomComplet = nomComplet;
            utilisateur.Role = role;
            await _db.SaveChangesAsync();
        }

        public async Task SupprimerAsync(int id)
        {
            var utilisateur = await _db.Utilisateurs.FindAsync(id);
            if (utilisateur == null) return;
            _db.Utilisateurs.Remove(utilisateur);
            await _db.SaveChangesAsync();
        }
    }
}
