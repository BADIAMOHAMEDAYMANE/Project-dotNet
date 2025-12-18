using CarRental.Core.Interfaces;
using CarRental.Core.Models;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CarRental.Core.Services
{
    public class FactureService : IFactureService
    {
        private readonly IFactureRepository _factureRepository;
        private readonly ILogger<FactureService> _logger;
        private readonly string _facturesDirectory;

        public FactureService(
            IFactureRepository factureRepository,
            ILogger<FactureService> logger)
        {
            _factureRepository = factureRepository;
            _logger = logger;

            // Créer le dossier pour stocker les factures
            _facturesDirectory = Path.Combine("wwwroot", "factures");
            if (!Directory.Exists(_facturesDirectory))
            {
                Directory.CreateDirectory(_facturesDirectory);
                _logger.LogInformation("📁 Dossier factures créé : {Path}", _facturesDirectory);
            }
        }
        public async Task<IEnumerable<Facture>> GetFacturesByClientEmailAsync(string email)
        {
            return await _factureRepository.GetFacturesByClientEmailAsync(email);
        }

        public async Task<Facture?> GetByIdAsync(int id)
        {
            return await _factureRepository.GetByIdAsync(id);
        }

        public async Task<Facture?> GetByLocationIdAsync(int locationId)
        {
            return await _factureRepository.GetByLocationIdAsync(locationId);
        }

        public async Task<IEnumerable<Facture>> GetAllAsync()
        {
            return await _factureRepository.GetAllAsync();
        }

        public async Task<bool> FactureExistePourLocationAsync(int locationId)
        {
            var facture = await _factureRepository.GetByLocationIdAsync(locationId);
            return facture != null;
        }

        public async Task<Facture> CreateFactureAsync(Location location, string format)
        {
            _logger.LogInformation("=== DEBUT CREATION FACTURE pour Location #{LocationId} ===", location.Id);

            try
            {
                // 🔒 Règle métier : location confirmée
                if (location.Statut != "Confirmée")
                {
                    throw new InvalidOperationException("La location doit être confirmée pour générer une facture.");
                }

                // 🔁 Une seule facture par location
                var existante = await _factureRepository.GetByLocationIdAsync(location.Id);
                if (existante != null)
                {
                    _logger.LogWarning("⚠️ Une facture existe déjà pour Location #{LocationId}", location.Id);
                    return existante;
                }

                // Valider que les données nécessaires sont présentes
                if (location.Client == null || location.Vehicule == null)
                {
                    throw new InvalidOperationException("Les données Client et Véhicule sont requises pour générer une facture.");
                }

                // Calculer le montant total
                var montantTotal = location.CalculerMontantTotal();

                var facture = new Facture
                {
                    LocationId = location.Id,
                    DateFacture = DateTime.Now,
                    MontantTotal = montantTotal,
                    Format = format.ToUpper()
                };

                // 📄 Génération du fichier selon le format
                facture.CheminFichier = format.ToUpper() == "CSV"
                    ? GenererCSV(location, facture)
                    : GenererPDF(location, facture);

                // Sauvegarder en base de données
                await _factureRepository.AddAsync(facture);

                _logger.LogInformation("✅ Facture #{FactureId} créée avec succès pour Location #{LocationId}",
                    facture.Id, location.Id);

                return facture;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la création de la facture pour Location #{LocationId}", location.Id);
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var facture = await _factureRepository.GetByIdAsync(id);

                if (facture != null)
                {
                    // Supprimer le fichier physique
                    if (!string.IsNullOrEmpty(facture.CheminFichier) && File.Exists(facture.CheminFichier))
                    {
                        File.Delete(facture.CheminFichier);
                        _logger.LogInformation("🗑️ Fichier supprimé : {FilePath}", facture.CheminFichier);
                    }

                    await _factureRepository.DeleteAsync(id);
                    _logger.LogInformation("✅ Facture #{FactureId} supprimée", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la suppression de la facture #{Id}", id);
                throw;
            }
        }

        // =========================
        // GÉNÉRATION CSV
        // =========================
        private string GenererCSV(Location location, Facture facture)
        {
            try
            {
                var fileName = $"Facture_{location.Id}_{DateTime.Now:yyyyMMddHHmmss}.csv";
                var chemin = Path.Combine(_facturesDirectory, fileName);

                var lignes = new[]
                {
                    "Client,Véhicule,Immatriculation,DateDébut,DateFin,NombreJours,PrixParJour,MontantHT,TVA,MontantTTC",
                    $"\"{location.Client?.Nom} {location.Client?.Prenom}\"," +
                    $"\"{location.Vehicule?.Marque} {location.Vehicule?.Modele}\"," +
                    $"\"{location.Vehicule?.Immatriculation}\"," +
                    $"{location.DateDebut:yyyy-MM-dd}," +
                    $"{location.DateFin:yyyy-MM-dd}," +
                    $"{location.NombreJours}," +
                    $"{location.Vehicule?.PrixParJour:F2}," +
                    $"{facture.MontantTotal:F2}," +
                    $"{facture.MontantTotal * 0.20m:F2}," +
                    $"{facture.MontantTotal * 1.20m:F2}"
                };

                File.WriteAllLines(chemin, lignes);
                _logger.LogInformation("📄 CSV généré : {FilePath}", chemin);

                return chemin;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la génération du CSV");
                throw;
            }
        }

        // =========================
        // GÉNÉRATION PDF avec QuestPDF
        // =========================
        private string GenererPDF(Location location, Facture facture)
        {
            try
            {
                QuestPDF.Settings.License = LicenseType.Community;

                var fileName = $"Facture_{location.Id}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                var chemin = Path.Combine(_facturesDirectory, fileName);

                var nombreJours = location.NombreJours;
                var prixParJour = location.Vehicule?.PrixParJour ?? 0;
                var montantHT = facture.MontantTotal;
                var tva = montantHT * 0.20m;
                var montantTTC = montantHT + tva;

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(50);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(11));

                        // ========== EN-TÊTE ==========
                        page.Header()
                            .Row(row =>
                            {
                                // Logo et informations entreprise
                                row.RelativeItem().Column(column =>
                                {
                                    column.Item().Text("CARRENTAL").FontSize(32).Bold().FontColor(Colors.Blue.Darken2);
                                    column.Item().Text("Location de Véhicules Premium").FontSize(12).FontColor(Colors.Grey.Darken1);
                                    column.Item().PaddingTop(8).Text("123 Boulevard Mohammed V, Casablanca").FontSize(9);
                                    column.Item().Text("Tél: +212 522-123456 | Email: contact@carrental.ma").FontSize(9);
                                    column.Item().Text("RC: 123456 | IF: 7654321 | CNSS: 9876543").FontSize(8).FontColor(Colors.Grey.Medium);
                                });

                                // Numéro de facture
                                row.ConstantItem(140).AlignRight().Column(column =>
                                {
                                    column.Item().Background(Colors.Blue.Darken2).Padding(10).Column(col =>
                                    {
                                        col.Item().Text("FACTURE").Bold().FontSize(14).FontColor(Colors.White);
                                        col.Item().Text($"N° {location.Id:D6}").FontSize(16).Bold().FontColor(Colors.White);
                                    });
                                    column.Item().PaddingTop(5).Text($"Date: {DateTime.Now:dd/MM/yyyy}").FontSize(10);
                                });
                            });

                        // ========== CONTENU ==========
                        page.Content()
                            .PaddingVertical(25)
                            .Column(column =>
                            {
                                // Informations Client
                                column.Item().PaddingBottom(25).Row(row =>
                                {
                                    row.RelativeItem().Background(Colors.Grey.Lighten3).Padding(15).Column(col =>
                                    {
                                        col.Item().Text("FACTURÉ À :").Bold().FontSize(13).FontColor(Colors.Blue.Darken2);
                                        col.Item().PaddingTop(8).Text($"{location.Client?.Nom} {location.Client?.Prenom}").FontSize(12).Bold();
                                        col.Item().PaddingTop(3).Text($"CIN: {location.Client?.CIN}").FontSize(10);
                                        col.Item().Text($"Email: {location.Client?.Email}").FontSize(10);
                                        col.Item().Text($"Téléphone: {location.Client?.Telephone}").FontSize(10);
                                        col.Item().Text($"Permis: {location.Client?.NumeroPermis}").FontSize(10);
                                    });
                                });

                                // Titre de la section
                                column.Item().PaddingBottom(15).Text("DÉTAILS DE LA LOCATION")
                                    .Bold().FontSize(15).FontColor(Colors.Blue.Darken2);

                                // Tableau des détails
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(4);  // Description
                                        columns.RelativeColumn(2);  // Quantité
                                        columns.RelativeColumn(2);  // Prix Unitaire
                                        columns.RelativeColumn(2);  // Total
                                    });

                                    // En-tête du tableau
                                    table.Header(header =>
                                    {
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(10)
                                            .Text("Description").FontColor(Colors.White).Bold().FontSize(11);
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(10)
                                            .Text("Durée").FontColor(Colors.White).Bold().FontSize(11);
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(10)
                                            .Text("Prix/Jour").FontColor(Colors.White).Bold().FontSize(11);
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(10)
                                            .Text("Total HT").FontColor(Colors.White).Bold().FontSize(11);
                                    });

                                    // Ligne de détails
                                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12)
                                        .Column(col =>
                                        {
                                            col.Item().Text($"{location.Vehicule?.Marque} {location.Vehicule?.Modele}")
                                                .FontSize(12).Bold();
                                            col.Item().PaddingTop(3).Text($"Immatriculation: {location.Vehicule?.Immatriculation}")
                                                .FontSize(10).FontColor(Colors.Grey.Darken1);
                                            col.Item().PaddingTop(5).Text($"Du {location.DateDebut:dd/MM/yyyy} au {location.DateFin:dd/MM/yyyy}")
                                                .FontSize(10);
                                            col.Item().PaddingTop(2).Text($"Catégorie: {location.Vehicule?.Categorie?.Nom ?? "Standard"}")
                                                .FontSize(9).FontColor(Colors.Grey.Medium);
                                        });

                                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12)
                                        .AlignCenter().AlignMiddle().Text($"{nombreJours} jour(s)").FontSize(11);

                                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12)
                                        .AlignRight().AlignMiddle().Text($"{prixParJour:N2} MAD").FontSize(11);

                                    table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(12)
                                        .AlignRight().AlignMiddle().Text($"{montantHT:N2} MAD").Bold().FontSize(11);
                                });

                                // Section des totaux
                                column.Item().PaddingTop(25).AlignRight().Column(col =>
                                {
                                    // Sous-total
                                    col.Item().Row(r =>
                                    {
                                        r.ConstantItem(180).Text("Sous-total HT:").FontSize(12);
                                        r.ConstantItem(120).AlignRight().Text($"{montantHT:N2} MAD").FontSize(12);
                                    });

                                    // TVA
                                    col.Item().PaddingTop(8).Row(r =>
                                    {
                                        r.ConstantItem(180).Text("TVA (20%):").FontSize(12);
                                        r.ConstantItem(120).AlignRight().Text($"{tva:N2} MAD").FontSize(12);
                                    });

                                    // Total TTC
                                    col.Item().PaddingTop(10).Background(Colors.Blue.Darken2).Padding(12).Row(r =>
                                    {
                                        r.ConstantItem(180).Text("TOTAL TTC:").Bold().FontSize(15).FontColor(Colors.White);
                                        r.ConstantItem(120).AlignRight().Text($"{montantTTC:N2} MAD")
                                            .Bold().FontSize(15).FontColor(Colors.White);
                                    });
                                });

                                // Informations de paiement
                                column.Item().PaddingTop(35).Border(1).BorderColor(Colors.Blue.Lighten2)
                                    .Background(Colors.Blue.Lighten5).Padding(15).Column(col =>
                                    {
                                        col.Item().Text("💳 CONDITIONS DE PAIEMENT").Bold().FontSize(12).FontColor(Colors.Blue.Darken2);
                                        col.Item().PaddingTop(8).Text("• Paiement à régler au plus tard 7 jours après la date de facturation")
                                            .FontSize(10);
                                        col.Item().PaddingTop(3).Text("• Modes de paiement acceptés : Espèces, Carte bancaire, Virement bancaire")
                                            .FontSize(10);
                                        col.Item().PaddingTop(3).Text("• RIB : 011 780 0000123456789012 34 (Bank of Africa)")
                                            .FontSize(10);
                                    });

                                // Message de remerciement
                                column.Item().PaddingTop(20).AlignCenter()
                                    .Text("Merci pour votre confiance ! Nous vous souhaitons une excellente location.")
                                    .FontSize(11).Italic().FontColor(Colors.Blue.Darken1);
                            });

                        // ========== PIED DE PAGE ==========
                        page.Footer()
                            .AlignCenter()
                            .Column(col =>
                            {
                                col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                                col.Item().PaddingTop(10).Text(text =>
                                {
                                    text.Span("CarRental © 2024 | Location de véhicules premium")
                                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                                    text.Span(" | www.carrental.ma")
                                        .FontSize(9).FontColor(Colors.Blue.Medium);
                                });
                                col.Item().PaddingTop(3).Text($"Document généré le {DateTime.Now:dd/MM/yyyy à HH:mm}")
                                    .FontSize(8).FontColor(Colors.Grey.Medium);
                            });
                    });
                }).GeneratePdf(chemin);

                _logger.LogInformation("📄 PDF généré : {FilePath}", chemin);
                return chemin;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la génération du PDF");
                throw;
            }
        }
    }
}