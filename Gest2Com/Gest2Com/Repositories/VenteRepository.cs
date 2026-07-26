using Microsoft.EntityFrameworkCore;
using Gest2Com.Data;
using Gest2Com.Models;

namespace Gest2Com.Repositories
{
    public class VenteRepository
    {
        private readonly AppDbContext _db;
        public VenteRepository(AppDbContext db) => _db = db;

        public async Task<List<Vente>> ListerToutesAsync(string? recherche = null, string? type = null, string? statut = null)
        {
            var requete = _db.Ventes.Include(v => v.Client).AsQueryable();

            if (!string.IsNullOrWhiteSpace(recherche))
            {
                requete = requete.Where(v =>
                    v.Numero.Contains(recherche) ||
                    (v.Client != null && v.Client.Nom.Contains(recherche)));
            }
            if (!string.IsNullOrWhiteSpace(type))
                requete = requete.Where(v => v.TypeVente == type);
            if (!string.IsNullOrWhiteSpace(statut))
                requete = requete.Where(v => v.Statut == statut);

            return await requete.OrderByDescending(v => v.DateVente).ToListAsync();
        }

        public async Task<List<Vente>> ListerCreditsEnCoursAsync() =>
            await _db.Ventes.Include(v => v.Client)
                .Where(v => v.TypeVente == Vente.TYPE_CREDIT && v.Statut != Vente.STATUT_PAYEE)
                .OrderByDescending(v => v.DateVente)
                .ToListAsync();

        public async Task<Vente?> ParIdAvecDetailsAsync(int id) =>
            await _db.Ventes
                .Include(v => v.Client)
                .Include(v => v.Lignes)
                .Include(v => v.Paiements)
                .FirstOrDefaultAsync(v => v.Id == id);

        /// <summary>
        /// Enregistre la vente et ses lignes dans une transaction SQL unique,
        /// pour garantir la cohérence (pas de vente à moitié enregistrée).
        /// </summary>
        public async Task<int> EnregistrerAsync(Vente vente)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.Ventes.Add(vente);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return vente.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task MettreAJourStatutEtMontantPayeAsync(int venteId, decimal montantPaye, string statut)
        {
            var vente = await _db.Ventes.FindAsync(venteId);
            if (vente == null) return;
            vente.MontantPaye = montantPaye;
            vente.Statut = statut;
            await _db.SaveChangesAsync();
        }

        public async Task<decimal> ChiffreAffairesPeriodeAsync(DateTime debut, DateTime fin) =>
            await _db.Ventes
                .Where(v => v.DateVente >= debut && v.DateVente <= fin)
                .SumAsync(v => v.MontantTotal);

        public async Task<List<Vente>> ListerPeriodeAsync(DateTime debut, DateTime fin) =>
            await _db.Ventes.Include(v => v.Client)
                .Where(v => v.DateVente >= debut && v.DateVente <= fin)
                .OrderByDescending(v => v.DateVente)
                .ToListAsync();
    }
}
