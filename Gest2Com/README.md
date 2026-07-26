# Gest2Com (ASP.NET Core MVC / C# / SQL Server)

Application web de vente (espèces et crédit) et de gestion de stock, conçue
pour fonctionner sur un poste Windows (IIS Express en développement, ou
IIS/Kestrel en production locale), avec une base **SQL Server locale**.

Contrairement aux projets précédents où le MVC était recréé "à la main"
(Android, WPF), **ASP.NET Core MVC implémente nativement ce pattern** :
Controllers, Views (Razor) et Models sont des briques de premier ordre du
framework.

## Structure du projet

```
Gest2Com/
├── Models/                          → MODEL (entités)
│   ├── Produit.cs
│   ├── Client.cs                    (LimiteCredit, SoldeCredit)
│   ├── Vente.cs                     (Especes / Credit, statuts de paiement)
│   ├── LigneVente.cs
│   ├── PaiementCredit.cs            (versements successifs sur une vente à crédit)
│   ├── MouvementStock.cs
│   ├── Utilisateur.cs
│   └── ViewModels/
│       ├── VenteCreationViewModel.cs
│       ├── TableauBordViewModel.cs
│       ├── RapportCaisseViewModel.cs
│       └── ClientRelanceViewModel.cs
│
├── Data/
│   └── AppDbContext.cs              (Entity Framework Core → SQL Server)
│
├── Repositories/                    → accès données (partie du Model au sens large)
│   ├── ProduitRepository.cs
│   ├── ClientRepository.cs
│   ├── VenteRepository.cs
│   ├── PaiementCreditRepository.cs
│   ├── MouvementStockRepository.cs
│   └── AuthRepository.cs            (auth locale, mdp hashé SHA-256)
│
├── Services/
│   └── RecuPdfGenerator.cs          (reçu de vente PDF, via QuestPDF)
│
├── Filters/
│   └── RequireConnexionAttribute.cs (contrôle d'accès par session, cf. plus bas)
│
├── Controllers/                     → CONTROLLER
│   ├── AccountController.cs         (connexion par session)
│   ├── HomeController.cs            (tableau de bord)
│   ├── ProduitsController.cs        (CRUD catalogue/stock)
│   ├── ClientsController.cs         (dont relances clients en retard)
│   ├── VentesController.cs          (cœur métier : espèces/crédit, rapport de caisse, reçu PDF)
│   └── UtilisateursController.cs    (gestion des comptes et rôles, admin uniquement)
│
├── Views/                           → VIEW (Razor .cshtml)
│   ├── _ViewImports.cshtml          (active les Tag Helpers asp-for/asp-action/asp-route-*)
│   ├── Shared/_Layout.cshtml
│   ├── Account/Login.cshtml
│   ├── Home/Index.cshtml
│   ├── Produits/Index.cshtml, Create.cshtml, Edit.cshtml
│   ├── Clients/Index.cshtml, Create.cshtml, Edit.cshtml, Credits.cshtml, Relances.cshtml
│   ├── Ventes/Index.cshtml, Create.cshtml, Details.cshtml, Rapport.cshtml
│   └── Utilisateurs/Index.cshtml, Create.cshtml, Edit.cshtml
│
├── wwwroot/css/site.css, wwwroot/lib/ (jQuery Validation, vendorisé)
├── Program.cs                       (point d'entrée, injection de dépendances)
└── appsettings.json                 (chaîne de connexion SQL Server)
```

## Logique métier : vente espèces vs vente à crédit

Toute la logique est centralisée dans `VentesController.Create` :

1. **Vérification du stock** pour chaque ligne du panier — refus si
   quantité demandée > stock disponible.
2. **Vente à crédit uniquement** : vérification de la **limite de crédit**
   du client (`Client.PeutEmprunter`) — refus si le solde dû dépasserait
   la limite autorisée.
3. Calcul du montant payé immédiatement :
   - **Espèces** → payé intégralement à la création
   - **Crédit** → le montant saisi comme acompte (peut être 0), le reste
     devient une dette suivie sur `Client.SoldeCredit`
4. **Décrémentation du stock** + journal (`MouvementStock`) pour chaque
   produit vendu.
5. **Mise à jour du solde de crédit du client** si vente à crédit.

Ensuite, `VentesController.EnregistrerVersement` permet d'enregistrer des
**versements successifs** sur une vente à crédit non soldée (visible sur
la page `Ventes/Details`), qui réduisent à la fois le solde restant de la
vente et le solde de crédit global du client.

## Base de données SQL Server

| Table              | Rôle                                                     |
|----------------------|--------------------------------------------------------------|
| `Produits`            | Catalogue + stock                                        |
| `Clients`              | Nom, contact, `LimiteCredit`, `SoldeCredit` (dette actuelle) |
| `Ventes`                | En-tête : type (Especes/Credit), montant, statut         |
| `LignesVente`            | Détail des produits vendus (copie figée du prix/nom)     |
| `PaiementsCredit`         | Historique des versements sur les ventes à crédit         |
| `MouvementsStock`          | Traçabilité entrées/sorties de stock                     |
| `Utilisateurs`               | Comptes (mot de passe hashé SHA-256)                    |

## Installation (poste Windows avec .NET 8 SDK + SQL Server / SQL Server Express)

```bash
cd Gest2Com
dotnet restore
```

Adapte la chaîne de connexion dans `appsettings.json` selon ton instance
SQL Server locale (`Server=localhost\SQLEXPRESS;...` par défaut).

Crée la base de données et les tables via les migrations Entity Framework :

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Puis lance l'application :

```bash
dotnet run
```

L'application est accessible sur `https://localhost:5001` (ou le port
affiché dans la console).

## Premier compte utilisateur

Comme pour les projets précédents, aucun compte n'existe à l'installation.
Crée le premier compte admin via `dotnet ef` en exécutant temporairement
ce code (par exemple dans `Program.cs`, juste après `var app = builder.Build();`) :

```csharp
using (var scope = app.Services.CreateScope())
{
    var authRepo = scope.ServiceProvider.GetRequiredService<AuthRepository>();
    await authRepo.CreerUtilisateurAsync("admin", "motdepasse", "Administrateur", "admin");
}
```

Lance l'application une fois, puis retire ce bloc. Les comptes suivants
(vendeurs, gérants, autres admins) se créent ensuite directement dans
l'interface via **Utilisateurs → + Nouvel utilisateur** (menu visible
uniquement pour les comptes admin).

## Gestion des utilisateurs et des rôles

Trois rôles : `admin`, `gerant`, `vendeur` (stockés en clair sur
`Utilisateur.Role`, pas d'enum — cohérent avec le reste du projet qui évite
la sur-ingénierie). La page `/Utilisateurs` (menu "Utilisateurs", réservé
aux admins) permet de :

- créer un utilisateur (nom d'utilisateur, mot de passe, nom complet, rôle) ;
- modifier le nom complet et le rôle d'un utilisateur existant ;
- supprimer un utilisateur.

Protections appliquées côté `UtilisateursController` : impossible de
supprimer son propre compte, et impossible de retirer le rôle admin ou de
supprimer le dernier compte admin restant (pour ne jamais se retrouver
bloqué hors de la gestion des utilisateurs). Le contrôle d'accès repose sur
la session (`UtilisateurRole`), pas sur `[Authorize]`/ASP.NET Identity —
cohérent avec l'auth locale déjà en place.

## Contrôle d'accès

Tous les controllers métier (`Home`, `Produits`, `Clients`, `Ventes`) sont
protégés par `[RequireConnexion]` (`Filters/RequireConnexionAttribute.cs`) :
sans session active, toute requête redirige vers `/Account/Login`. C'est un
`ActionFilterAttribute` maison plutôt que `[Authorize]`/ASP.NET Identity,
pour rester cohérent avec l'auth locale par session déjà en place.
`AccountController` (Login/Déconnexion) reste volontairement non protégé.

### Restrictions par rôle

`Filters/RequireRoleAttribute.cs` complète `[RequireConnexion]` : il exige en
plus que le rôle de la session fasse partie d'une liste autorisée, sinon
redirige vers le tableau de bord avec un message d'erreur. Un `vendeur` peut
vendre (`Ventes/Create`), consulter le catalogue et les fiches clients, et
encaisser des versements sur les ventes à crédit (`EnregistrerVersement`) —
ce sont les actions quotidiennes d'un poste de vente. Sont réservées à
`admin`/`gerant` (`[RequireRole("admin", "gerant")]`) les actions plus
sensibles :

- gestion du catalogue (`Produits` : Create/Edit/Delete) ;
- gestion des fiches clients, y compris la limite de crédit (`Clients` :
  Create/Edit/Delete) ;
- rapport de caisse (`Ventes/Rapport`), qui expose le chiffre d'affaires
  global.

`UtilisateursController` utilise le même attribut au niveau du controller
(`[RequireRole("admin")]`), qui remplace la vérification manuelle
`VerifierAcces()` répétée précédemment sur chaque action.

## Validation côté client

jQuery Validation + jQuery Validation Unobtrusive (`wwwroot/lib/`, chargés
dans `_Layout.cshtml`) exploitent directement les attributs `data-val-*`
déjà générés par les Tag Helpers à partir des `[Required]`/`[Range]` sur les
Models — aucune règle de validation dupliquée en JavaScript. Actif sur tous
les formulaires qui utilisent `asp-for` (Produits Create/Edit, Clients
Create/Edit) : la soumission est bloquée et un message d'erreur inline
s'affiche tant qu'un champ invalide n'est pas corrigé, sans aller-retour
serveur. Le formulaire `Ventes/Create` (champs `name=` bruts pour le binding
de collection `Lignes[i].*`) et `Utilisateurs/Create` (champs bruts,
validation métier trop spécifique pour de simples attributs) restent sur la
validation HTML5 native (`required`) + validation serveur uniquement.
Fichiers vendorisés en local (pas de CDN) pour que l'app reste utilisable
hors ligne sur le poste de vente.

