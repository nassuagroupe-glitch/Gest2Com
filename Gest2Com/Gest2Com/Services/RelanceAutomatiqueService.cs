namespace Gest2Com.Services
{
    /// <summary>
    /// Tâche planifiée : envoie chaque jour à 08:00 les relances WhatsApp aux
    /// clients en crédit en retard, en respectant le cooldown entre deux relances
    /// automatiques d'un même client (voir RelanceCreditService).
    /// </summary>
    public class RelanceAutomatiqueService : BackgroundService
    {
        private static readonly TimeSpan HeureExecution = new(8, 0, 0);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RelanceAutomatiqueService> _logger;

        public RelanceAutomatiqueService(IServiceScopeFactory scopeFactory, ILogger<RelanceAutomatiqueService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var delai = CalculerDelaiAvantProchaineExecution();
                try
                {
                    await Task.Delay(delai, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                await EnvoyerRelancesDuJourAsync(stoppingToken);
            }
        }

        private static TimeSpan CalculerDelaiAvantProchaineExecution()
        {
            var maintenant = DateTime.Now;
            var prochaineExecution = maintenant.Date + HeureExecution;
            if (prochaineExecution <= maintenant)
                prochaineExecution = prochaineExecution.AddDays(1);

            return prochaineExecution - maintenant;
        }

        private async Task EnvoyerRelancesDuJourAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var relanceService = scope.ServiceProvider.GetRequiredService<RelanceCreditService>();
            var whatsAppSender = scope.ServiceProvider.GetRequiredService<IWhatsAppSender>();
            var clientRepository = scope.ServiceProvider.GetRequiredService<Repositories.ClientRepository>();

            List<Models.ViewModels.ClientRelanceViewModel> relances;
            try
            {
                relances = await relanceService.ObtenirRelancesEligiblesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Échec de la récupération des relances de crédit à relancer automatiquement");
                return;
            }

            foreach (var relance in relances.Where(RelanceCreditService.EstEligibleEnvoiAuto))
            {
                if (stoppingToken.IsCancellationRequested) break;

                var (succes, erreur) = await whatsAppSender.EnvoyerAsync(relance.Telephone, relance.Message);
                if (succes)
                {
                    await clientRepository.EnregistrerRelanceAsync(relance.ClientId);
                    _logger.LogInformation("Relance automatique envoyée à {Client} (id {ClientId})", relance.Nom, relance.ClientId);
                }
                else
                {
                    _logger.LogWarning("Échec de la relance automatique pour {Client} (id {ClientId}) : {Erreur}", relance.Nom, relance.ClientId, erreur);
                }
            }
        }
    }
}
