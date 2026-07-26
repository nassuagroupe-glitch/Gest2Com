using Microsoft.EntityFrameworkCore;
using Gest2Com.Data;
using Gest2Com.Models;

namespace Gest2Com.Repositories
{
    public class PaiementCreditRepository
    {
        private readonly AppDbContext _db;
        public PaiementCreditRepository(AppDbContext db) => _db = db;

        public async Task AjouterAsync(PaiementCredit paiement)
        {
            _db.PaiementsCredit.Add(paiement);
            await _db.SaveChangesAsync();
        }

        public async Task<List<PaiementCredit>> ListerParVenteAsync(int venteId) =>
            await _db.PaiementsCredit
                .Where(p => p.VenteId == venteId)
                .OrderByDescending(p => p.DatePaiement)
                .ToListAsync();

        /// <summary>Tous les versements (acomptes inclus) encaissés sur une période, quelle que soit la date de la vente d'origine.</summary>
        public async Task<List<PaiementCredit>> ListerPeriodeAsync(DateTime debut, DateTime fin) =>
            await _db.PaiementsCredit
                .Where(p => p.DatePaiement >= debut && p.DatePaiement <= fin)
                .ToListAsync();
    }
}
