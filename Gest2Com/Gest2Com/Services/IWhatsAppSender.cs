namespace Gest2Com.Services
{
    /// <summary>Envoi de messages WhatsApp (relances de crédit) vers un numéro local.</summary>
    public interface IWhatsAppSender
    {
        Task<(bool Succes, string? Erreur)> EnvoyerAsync(string telephoneLocal, string message);
    }
}
