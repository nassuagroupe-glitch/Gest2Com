using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Gest2Com.Models;

namespace Gest2Com.Services
{
    /// <summary>
    /// Génère le reçu PDF imprimable d'une vente (espèces ou crédit).
    /// La vente doit être chargée avec ses lignes, son client et ses paiements
    /// (cf. VenteRepository.ParIdAvecDetailsAsync).
    /// </summary>
    public static class RecuPdfGenerator
    {
        public static byte[] Generer(Vente vente)
        {
            return Document.Create(document =>
            {
                document.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(24);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("Gest2Com").FontSize(18).Bold();
                        col.Item().Text("Reçu de vente").FontSize(12).SemiBold();
                        col.Item().PaddingTop(6).LineHorizontal(1);
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Spacing(3);
                        col.Item().Text($"N° {vente.Numero}");
                        col.Item().Text($"Date : {vente.DateVente:dd/MM/yyyy HH:mm}");
                        col.Item().Text($"Vendeur : {vente.VendeurNom}");
                        col.Item().Text($"Client : {vente.Client?.Nom ?? "Client comptoir"}");
                        col.Item().Text($"Type de vente : {(vente.TypeVente == Vente.TYPE_ESPECES ? "Espèces" : "Crédit")}");

                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Produit").Bold();
                                header.Cell().Text("Qté").Bold();
                                header.Cell().Text("P.U.").Bold();
                                header.Cell().Text("Sous-total").Bold();
                                header.Cell().ColumnSpan(4).PaddingTop(2).PaddingBottom(4).LineHorizontal(1);
                            });

                            foreach (var ligne in vente.Lignes)
                            {
                                table.Cell().Text(ligne.NomProduit);
                                table.Cell().Text(ligne.Quantite.ToString());
                                table.Cell().Text($"{ligne.PrixUnitaire:N0} F");
                                table.Cell().Text($"{ligne.SousTotal():N0} F");
                            }
                        });

                        col.Item().PaddingTop(8).LineHorizontal(1);
                        col.Item().AlignRight().Text($"Total : {vente.MontantTotal:N0} F").Bold().FontSize(12);
                        col.Item().AlignRight().Text($"Payé : {vente.MontantPaye:N0} F");

                        if (vente.MontantRestant > 0)
                        {
                            col.Item().AlignRight().Text($"Restant dû : {vente.MontantRestant:N0} F")
                                .FontColor(Colors.Red.Medium).Bold();
                        }

                        if (vente.Paiements.Any())
                        {
                            col.Item().PaddingTop(12).Text("Versements").SemiBold();
                            foreach (var paiement in vente.Paiements.OrderBy(p => p.DatePaiement))
                            {
                                col.Item().Text($"{paiement.DatePaiement:dd/MM/yyyy HH:mm} — {paiement.Montant:N0} F ({paiement.ModePaiement})");
                            }
                        }
                    });

                    page.Footer().AlignCenter().Text("Merci de votre confiance").FontSize(9).Italic();
                });
            }).GeneratePdf();
        }
    }
}
