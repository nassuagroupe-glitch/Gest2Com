using Microsoft.EntityFrameworkCore;
using Gest2Com.Data;
using Gest2Com.Models;

namespace Gest2Com.Repositories
{
    public class ClientRepository
    {
        private readonly AppDbContext _db;
        public ClientRepository(AppDbContext db) => _db = db;

        public async Task<List<Client>> ListerTousAsync(string? recherche = null)
        {
            var requete = _db.Clients.AsQueryable();
            if (!string.IsNullOrWhiteSpace(recherche))
            {
                requete = requete.Where(c =>
                    c.Nom.Contains(recherche) ||
                    c.Telephone.Contains(recherche));
            }
            return await requete.OrderBy(c => c.Nom).ToListAsync();
        }

        public async Task<Client?> ParIdAsync(int id) => await _db.Clients.FindAsync(id);

        public async Task<List<Client>> AvecCreditEnCoursAsync() =>
            await _db.Clients.Where(c => c.SoldeCredit > 0).OrderByDescending(c => c.SoldeCredit).ToListAsync();

        public async Task AjouterAsync(Client client)
        {
            _db.Clients.Add(client);
            await _db.SaveChangesAsync();
        }

        public async Task ModifierAsync(Client client)
        {
            _db.Clients.Update(client);
            await _db.SaveChangesAsync();
        }

        /// <summary>Supprime le client. Retourne false (sans rien supprimer) s'il a des ventes enregistrées à son nom.</summary>
        public async Task<bool> SupprimerAsync(int id)
        {
            var client = await _db.Clients.FindAsync(id);
            if (client == null) return true;

            var aDesVentes = await _db.Ventes.AnyAsync(v => v.ClientId == id);
            if (aDesVentes) return false;

            _db.Clients.Remove(client);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task ModifierSoldeCreditAsync(int clientId, decimal nouveauSolde)
        {
            var client = await _db.Clients.FindAsync(clientId);
            if (client == null) return;
            client.SoldeCredit = nouveauSolde;
            await _db.SaveChangesAsync();
        }
    }
}
