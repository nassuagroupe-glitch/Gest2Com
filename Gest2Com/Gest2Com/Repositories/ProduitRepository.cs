using Microsoft.EntityFrameworkCore;
using Gest2Com.Data;
using Gest2Com.Models;

namespace Gest2Com.Repositories
{
    /// <summary>
    /// Repository : point unique d'accès aux produits en base SQL Server.
    /// </summary>
    public class ProduitRepository
    {
        private readonly AppDbContext _db;
        public ProduitRepository(AppDbContext db) => _db = db;

        public async Task<List<Produit>> ListerTousAsync(string? recherche = null)
        {
            var requete = _db.Produits.AsQueryable();
            if (!string.IsNullOrWhiteSpace(recherche))
            {
                requete = requete.Where(p =>
                    p.Nom.Contains(recherche) ||
                    p.Reference.Contains(recherche) ||
                    p.Categorie.Contains(recherche));
            }
            return await requete.OrderBy(p => p.Nom).ToListAsync();
        }

        public async Task<Produit?> ParIdAsync(int id) => await _db.Produits.FindAsync(id);

        public async Task<List<Produit>> EnAlerteAsync() =>
            await _db.Produits.Where(p => p.QuantiteStock <= p.SeuilAlerte).ToListAsync();

        public async Task AjouterAsync(Produit produit)
        {
            _db.Produits.Add(produit);
            await _db.SaveChangesAsync();
        }

        public async Task ModifierAsync(Produit produit)
        {
            _db.Produits.Update(produit);
            await _db.SaveChangesAsync();
        }

        public async Task SupprimerAsync(int id)
        {
            var produit = await _db.Produits.FindAsync(id);
            if (produit == null) return;
            _db.Produits.Remove(produit);
            await _db.SaveChangesAsync();
        }

        public async Task AjusterStockAsync(int produitId, int nouvelleQuantite)
        {
            var produit = await _db.Produits.FindAsync(produitId);
            if (produit == null) return;
            produit.QuantiteStock = nouvelleQuantite;
            await _db.SaveChangesAsync();
        }
    }
}
