using Microsoft.AspNetCore.Mvc;
using Gest2Com.Filters;
using Gest2Com.Models;
using Gest2Com.Models.ViewModels;
using Gest2Com.Repositories;
using Gest2Com.Services;

namespace Gest2Com.Controllers
{
    /// <summary>
    /// Controller gérant le processus de vente (espèces ou crédit) :
    /// vérification du stock et de la limite de crédit, création de la
    /// vente + ses lignes, décrémentation du stock, mise à jour du solde
    /// de crédit du client si vente à crédit.
    /// </summary>
    [RequireConnexion]
    public class VentesController : Controller
    {
        private readonly VenteRepository _venteRepository;
        private readonly ProduitRepository _produitRepository;
        private readonly ClientRepository _clientRepository;
        private readonly MouvementStockRepository _mouvementRepository;
        private readonly PaiementCreditRepository _paiementRepository;

        public VentesController(
            VenteRepository venteRepository, ProduitRepository produitRepository,
            ClientRepository clientRepository, MouvementStockRepository mouvementRepository,
            PaiementCreditRepository paiementRepository)
        {
            _venteRepository = venteRepository;
            _produitRepository = produitRepository;
            _clientRepository = clientRepository;
            _mouvementRepository = mouvementRepository;
            _paiementRepository = paiementRepository;
        }

        public async Task<IActionResult> Index(string? q, string? type, string? statut)
        {
            var ventes = await _venteRepository.ListerToutesAsync(q, type, statut);
            ViewData["Recherche"] = q;
            ViewData["TypeFiltre"] = type;
            ViewData["StatutFiltre"] = statut;
            return View(ventes);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var modele = new VenteCreationViewModel
            {
                ProduitsDisponibles = await _produitRepository.ListerTousAsync(),
                ClientsDisponibles = await _clientRepository.ListerTousAsync()
            };
            return View(modele);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VenteCreationViewModel modele)
        {
            if (modele.Lignes == null || !modele.Lignes.Any(l => l.Quantite > 0))
            {
                ModelState.AddModelError("", "Ajoutez au moins un produit à la vente");
                modele.ProduitsDisponibles = await _produitRepository.ListerTousAsync();
                modele.ClientsDisponibles = await _clientRepository.ListerTousAsync();
                return View(modele);
            }

            if (modele.TypeVente == Vente.TYPE_CREDIT && modele.ClientId == null)
            {
                ModelState.AddModelError("", "Une vente à crédit doit être associée à un client");
                modele.ProduitsDisponibles = await _produitRepository.ListerTousAsync();
                modele.ClientsDisponibles = await _clientRepository.ListerTousAsync();
                return View(modele);
            }

            var vente = new Vente
            {
                Numero = "GC-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                ClientId = modele.ClientId,
                TypeVente = modele.TypeVente,
                VendeurNom = HttpContext.Session.GetString("UtilisateurNom") ?? "Vendeur",
                DateVente = DateTime.Now
            };

            decimal montantTotal = 0;

            // Vérification du stock puis construction des lignes de vente
            foreach (var ligneInput in modele.Lignes.Where(l => l.Quantite > 0))
            {
                var produit = await _produitRepository.ParIdAsync(ligneInput.ProduitId);
                if (produit == null) continue;

                if (ligneInput.Quantite > produit.QuantiteStock)
                {
                    ModelState.AddModelError("", $"Stock insuffisant pour {produit.Nom} (disponible : {produit.QuantiteStock})");
                    modele.ProduitsDisponibles = await _produitRepository.ListerTousAsync();
                    modele.ClientsDisponibles = await _clientRepository.ListerTousAsync();
                    return View(modele);
                }

                vente.Lignes.Add(new LigneVente
                {
                    ProduitId = produit.Id,
                    NomProduit = produit.Nom,
                    Quantite = ligneInput.Quantite,
                    PrixUnitaire = produit.PrixVente
                });

                montantTotal += ligneInput.Quantite * produit.PrixVente;
            }

            vente.MontantTotal = montantTotal;

            // Vérification de la limite de crédit du client si vente à crédit
            Client? client = null;
            if (modele.TypeVente == Vente.TYPE_CREDIT)
            {
                client = await _clientRepository.ParIdAsync(modele.ClientId!.Value);
                if (client != null && !client.PeutEmprunter(montantTotal))
                {
                    ModelState.AddModelError("", $"Limite de crédit dépassée pour {client.Nom} " +
                        $"(solde actuel {client.SoldeCredit:N0}, limite {client.LimiteCredit:N0})");
                    modele.ProduitsDisponibles = await _produitRepository.ListerTousAsync();
                    modele.ClientsDisponibles = await _clientRepository.ListerTousAsync();
                    return View(modele);
                }
            }

            // Détermination du montant payé immédiatement et du statut
            vente.MontantPaye = modele.TypeVente == Vente.TYPE_ESPECES
                ? montantTotal
                : Math.Min(modele.MontantVerseImmediatement, montantTotal);

            vente.Statut = vente.MontantPaye >= montantTotal
                ? Vente.STATUT_PAYEE
                : (vente.MontantPaye > 0 ? Vente.STATUT_PARTIELLE : Vente.STATUT_IMPAYEE);

            var venteId = await _venteRepository.EnregistrerAsync(vente);

            // Décrémentation du stock + traçabilité, pour chaque ligne
            foreach (var ligne in vente.Lignes)
            {
                var produit = await _produitRepository.ParIdAsync(ligne.ProduitId);
                if (produit == null) continue;

                await _produitRepository.AjusterStockAsync(produit.Id, produit.QuantiteStock - ligne.Quantite);
                await _mouvementRepository.AjouterAsync(new MouvementStock
                {
                    ProduitId = produit.Id,
                    Type = MouvementStock.TYPE_SORTIE,
                    Quantite = ligne.Quantite,
                    Motif = $"Vente {vente.Numero}"
                });
            }

            // Mise à jour du solde de crédit du client si vente à crédit
            if (modele.TypeVente == Vente.TYPE_CREDIT && client != null)
            {
                var montantRestant = montantTotal - vente.MontantPaye;
                await _clientRepository.ModifierSoldeCreditAsync(client.Id, client.SoldeCredit + montantRestant);

                if (vente.MontantPaye > 0)
                {
                    await _paiementRepository.AjouterAsync(new PaiementCredit
                    {
                        VenteId = venteId,
                        Montant = vente.MontantPaye,
                        ModePaiement = "Versement initial",
                        Notes = "Acompte versé à la création de la vente"
                    });
                }
            }

            TempData["Succes"] = $"Vente {vente.Numero} enregistrée avec succès";
            return RedirectToAction(nameof(Details), new { id = venteId });
        }

        /// <summary>Génère le reçu PDF imprimable d'une vente.</summary>
        [HttpGet]
        public async Task<IActionResult> RecuPdf(int id)
        {
            var vente = await _venteRepository.ParIdAvecDetailsAsync(id);
            if (vente == null) return NotFound();

            var pdf = RecuPdfGenerator.Generer(vente);
            return File(pdf, "application/pdf", $"Recu-{vente.Numero}.pdf");
        }

        /// <summary>Rapport de caisse : CA espèces vs crédit et encaissements réels sur une période.</summary>
        [HttpGet]
        [RequireRole("admin", "gerant")]
        public async Task<IActionResult> Rapport(DateTime? debut, DateTime? fin)
        {
            var dateDebut = (debut ?? DateTime.Today).Date;
            var dateFin = (fin ?? DateTime.Today).Date;
            var borneFin = dateFin.AddDays(1).AddSeconds(-1);

            var ventes = await _venteRepository.ListerPeriodeAsync(dateDebut, borneFin);
            var versementsCredit = await _paiementRepository.ListerPeriodeAsync(dateDebut, borneFin);

            var ventesEspeces = ventes.Where(v => v.TypeVente == Vente.TYPE_ESPECES).ToList();
            var ventesCredit = ventes.Where(v => v.TypeVente == Vente.TYPE_CREDIT).ToList();

            var modele = new RapportCaisseViewModel
            {
                DateDebut = dateDebut,
                DateFin = dateFin,
                NombreVentesEspeces = ventesEspeces.Count,
                CAEspeces = ventesEspeces.Sum(v => v.MontantTotal),
                EncaisseEspeces = ventesEspeces.Sum(v => v.MontantPaye),
                NombreVentesCredit = ventesCredit.Count,
                CACredit = ventesCredit.Sum(v => v.MontantTotal),
                EncaisseVersementsCredit = versementsCredit.Sum(v => v.Montant),
                Ventes = ventes
            };

            return View(modele);
        }

        public async Task<IActionResult> Details(int id)
        {
            var vente = await _venteRepository.ParIdAvecDetailsAsync(id);
            if (vente == null) return NotFound();
            return View(vente);
        }

        /// <summary>Enregistre un versement supplémentaire sur une vente à crédit.</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnregistrerVersement(int venteId, decimal montant, string modePaiement)
        {
            var vente = await _venteRepository.ParIdAvecDetailsAsync(venteId);
            if (vente == null) return NotFound();

            if (montant <= 0 || montant > vente.MontantRestant)
            {
                TempData["Erreur"] = "Montant invalide (supérieur au solde restant)";
                return RedirectToAction(nameof(Details), new { id = venteId });
            }

            await _paiementRepository.AjouterAsync(new PaiementCredit
            {
                VenteId = venteId,
                Montant = montant,
                ModePaiement = modePaiement
            });

            var nouveauMontantPaye = vente.MontantPaye + montant;
            var nouveauStatut = nouveauMontantPaye >= vente.MontantTotal
                ? Vente.STATUT_PAYEE
                : Vente.STATUT_PARTIELLE;

            await _venteRepository.MettreAJourStatutEtMontantPayeAsync(venteId, nouveauMontantPaye, nouveauStatut);

            if (vente.ClientId != null)
            {
                var client = await _clientRepository.ParIdAsync(vente.ClientId.Value);
                if (client != null)
                    await _clientRepository.ModifierSoldeCreditAsync(client.Id, client.SoldeCredit - montant);
            }

            TempData["Succes"] = $"Versement de {montant:N0} F enregistré";
            return RedirectToAction(nameof(Details), new { id = venteId });
        }
    }
}
