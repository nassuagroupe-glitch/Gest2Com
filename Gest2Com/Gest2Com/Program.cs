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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
