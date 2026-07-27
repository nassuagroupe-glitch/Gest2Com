using System.Text.RegularExpressions;

namespace Gest2Com.Utils
{
    /// <summary>
    /// Normalisation des numéros de téléphone locaux (saisis sans indicatif,
    /// ex: 07XXXXXXXX) vers le format E.164 attendu par WhatsApp/Twilio.
    /// </summary>
    public static class Telephone
    {
        private const string INDICATIF_PAYS_PAR_DEFAUT = "225"; // Côte d'Ivoire

        /// <summary>Retourne null si le numéro ne contient aucun chiffre exploitable.</summary>
        public static string? VersE164(string telephone)
        {
            var chiffres = Regex.Replace(telephone ?? string.Empty, "[^0-9]", "");
            if (string.IsNullOrEmpty(chiffres)) return null;

            if (chiffres.StartsWith("0"))
                chiffres = chiffres[1..];
            else if (chiffres.StartsWith(INDICATIF_PAYS_PAR_DEFAUT))
                return "+" + chiffres;

            return "+" + INDICATIF_PAYS_PAR_DEFAUT + chiffres;
        }
    }
}
