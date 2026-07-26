using Microsoft.EntityFrameworkCore;
using Gest2Com.Models;

namespace Gest2Com.Data
{
    /// <summary>
    /// Contexte Entity Framework Core : point unique d'accès à SQL Server.
    /// Fait partie du Model au sens large (persistance).
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Produit> Produits => Set<Produit>();
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Vente> Ventes => Set<Vente>();
        public DbSet<LigneVente> LignesVente => Set<LigneVente>();
        public DbSet<PaiementCredit> PaiementsCredit => Set<PaiementCredit>();
        public DbSet<MouvementStock> MouvementsStock => Set<MouvementStock>();
        public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Vente>()
                .HasOne(v => v.Client)
                .WithMany(c => c.Ventes)
                .HasForeignKey(v => v.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LigneVente>()
                .HasOne(l => l.Vente)
                .WithMany(v => v.Lignes)
                .HasForeignKey(l => l.VenteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LigneVente>()
                .HasOne(l => l.Produit)
                .WithMany(p => p.LignesVente)
                .HasForeignKey(l => l.ProduitId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PaiementCredit>()
                .HasOne(p => p.Vente)
                .WithMany(v => v.Paiements)
                .HasForeignKey(p => p.VenteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Utilisateur>()
                .HasIndex(u => u.NomUtilisateur)
                .IsUnique();
        }
    }
}
