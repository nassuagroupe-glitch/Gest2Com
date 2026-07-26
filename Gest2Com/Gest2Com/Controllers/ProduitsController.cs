using Microsoft.AspNetCore.Mvc;
using Gest2Com.Filters;
using Gest2Com.Models;
using Gest2Com.Repositories;

namespace Gest2Com.Controllers
{
    /// <summary>
    /// Controller de gestion du catalogue / stock.
    /// </summary>
    [RequireConnexion]
    public class ProduitsController : Controller
    {
        private readonly ProduitRepository _repository;
        private readonly IWebHostEnvironment _environnement;

        public ProduitsController(ProduitRepository repository, IWebHostEnvironment environnement)
        {
            _repository = repository;
            _environnement = environnement;
        }

        public async Task<IActionResult> Index(string? q)
        {
            var produits = await _repository.ListerTousAsync(q);
            ViewData["Recherche"] = q;
            return View(produits);
        }

        [HttpGet]
        [RequireRole("admin", "gerant")]
        public IActionResult Create() => View(new Produit());

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("admin", "gerant")]
        public async Task<IActionResult> Create(Produit produit, IFormFile? image)
        {
            if (!ModelState.IsValid) return View(produit);

            if (image != null && image.Length > 0)
            {
                if (!image.ContentType.StartsWith("image/"))
                {
                    ModelState.AddModelError("", "Le fichier sélectionné n'est pas une image");
                    return View(produit);
                }

                produit.ImageNomFichier = await EnregistrerImageAsync(image);
            }

            await _repository.AjouterAsync(produit);
            TempData["Succes"] = $"Produit \"{produit.Nom}\" créé avec succès";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Enregistre l'image uploadée dans wwwroot/img/produits/ sous un nom unique, et retourne ce nom.</summary>
        private async Task<string> EnregistrerImageAsync(IFormFile image)
        {
            var dossierImages = Path.Combine(_environnement.WebRootPath, "img", "produits");
            Directory.CreateDirectory(dossierImages);

            var nomFichier = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
            using var flux = new FileStream(Path.Combine(dossierImages, nomFichier), FileMode.Create);
            await image.CopyToAsync(flux);

            return nomFichier;
        }

        [HttpGet]
        [RequireRole("admin", "gerant")]
        public async Task<IActionResult> Edit(int id)
        {
            var produit = await _repository.ParIdAsync(id);
            if (produit == null) return NotFound();
            return View(produit);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("admin", "gerant")]
        public async Task<IActionResult> Edit(int id, Produit produit)
        {
            if (id != produit.Id) return BadRequest();
            if (!ModelState.IsValid) return View(produit);

            await _repository.ModifierAsync(produit);
            TempData["Succes"] = "Produit mis à jour";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireRole("admin", "gerant")]
        public async Task<IActionResult> Delete(int id)
        {
            await _repository.SupprimerAsync(id);
            TempData["Succes"] = "Produit supprimé";
            return RedirectToAction(nameof(Index));
        }
    }
}
