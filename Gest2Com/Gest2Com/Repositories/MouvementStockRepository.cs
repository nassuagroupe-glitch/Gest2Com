using Microsoft.EntityFrameworkCore;
using Gest2Com.Data;
using Gest2Com.Models;

namespace Gest2Com.Repositories
{
    public class MouvementStockRepository
    {
        private readonly AppDbContext _db;
        public MouvementStockRepository(AppDbContext db) => _db = db;

        public async Task AjouterAsync(MouvementStock mouvement)
        {
            _db.MouvementsStock.Add(mouvement);
            await _db.SaveChangesAsync();
        }

        public async Task<List<MouvementStock>> HistoriqueProduitAsync(int produitId) =>
            await _db.MouvementsStock
                .Where(m => m.ProduitId == produitId)
                .OrderByDescending(m => m.Date)
                .ToListAsync();
    }
}
