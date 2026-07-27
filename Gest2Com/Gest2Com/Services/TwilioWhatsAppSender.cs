using Gest2Com.Utils;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Gest2Com.Services
{
    /// <summary>Envoi de messages WhatsApp via l'API Twilio.</summary>
    public class TwilioWhatsAppSender : IWhatsAppSender
    {
        private readonly string _accountSid;
        private readonly string _authToken;
        private readonly string _whatsAppFrom;
        private readonly ILogger<TwilioWhatsAppSender> _logger;

        public TwilioWhatsAppSender(IConfiguration configuration, ILogger<TwilioWhatsAppSender> logger)
        {
            _accountSid = configuration["Twilio:AccountSid"] ?? string.Empty;
            _authToken = configuration["Twilio:AuthToken"] ?? string.Empty;
            _whatsAppFrom = configuration["Twilio:WhatsAppFrom"] ?? string.Empty;
            _logger = logger;
        }

        public async Task<(bool Succes, string? Erreur)> EnvoyerAsync(string telephoneLocal, string message)
        {
            if (string.IsNullOrEmpty(_accountSid) || string.IsNullOrEmpty(_authToken) || string.IsNullOrEmpty(_whatsAppFrom))
                return (false, "Twilio n'est pas configuré (identifiants manquants).");

            var telephoneE164 = Telephone.VersE164(telephoneLocal);
            if (telephoneE164 == null)
                return (false, "Numéro de téléphone invalide.");

            try
            {
                TwilioClient.Init(_accountSid, _authToken);
                await MessageResource.CreateAsync(
                    from: new PhoneNumber($"whatsapp:{_whatsAppFrom}"),
                    to: new PhoneNumber($"whatsapp:{telephoneE164}"),
                    body: message);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Échec de l'envoi WhatsApp Twilio vers {Telephone}", telephoneE164);
                return (false, ex.Message);
            }
        }
    }
}
