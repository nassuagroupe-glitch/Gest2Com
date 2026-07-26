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
        public ProduitsController(ProduitRepository repository) => _repository = repository;

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
        public async Task<IActionResult> Create(Produit produit)
        {
            if (!ModelState.IsValid) return View(produit);

            await _repository.AjouterAsync(produit);
            TempData["Succes"] = $"Produit \"{produit.Nom}\" créé avec succès";
            return RedirectToAction(nameof(Index));
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
