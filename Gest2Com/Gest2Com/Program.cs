using Microsoft.EntityFrameworkCore;
using Gest2Com.Data;
using Gest2Com.Repositories;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Permet à l'application de tourner comme Windows Service (sc.exe / services.msc)
builder.Host.UseWindowsService();

// Injection de dépendances : DbContext (SQL Server) + Repositories (Model)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ProduitRepository>();
builder.Services.AddScoped<ClientRepository>();
builder.Services.AddScoped<VenteRepository>();
builder.Services.AddScoped<PaiementCreditRepository>();
builder.Services.AddScoped<MouvementStockRepository>();
builder.Services.AddScoped<AuthRepository>();

builder.Services.AddControllersWithViews();

// Session utilisée pour l'authentification locale (pas d'ASP.NET Identity)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
});

var app = builder.Build();

// Crée la base et applique les migrations en attente au démarrage : évite d'avoir
// à lancer manuellement `dotnet ef database update` sous chaque compte qui exécute le service.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

// Connexion automatique en développement : évite de ressaisir un login à chaque
// redémarrage sur localhost. Ne s'active jamais en dehors de Development.
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        if (context.Session.GetInt32("UtilisateurId") == null)
        {
            var authRepository = context.RequestServices.GetRequiredService<AuthRepository>();
            var utilisateurs = await authRepository.ListerTousAsync();
            var utilisateur = utilisateurs.FirstOrDefault(u => u.Role == "admin") ?? utilisateurs.FirstOrDefault();
            if (utilisateur != null)
            {
                context.Session.SetInt32("UtilisateurId", utilisateur.Id);
                context.Session.SetString("UtilisateurNom", utilisateur.NomComplet);
                context.Session.SetString("UtilisateurRole", utilisateur.Role);
            }
        }
        await next();
    });
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
