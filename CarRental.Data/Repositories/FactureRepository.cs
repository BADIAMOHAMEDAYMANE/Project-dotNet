using CarRental.Core.Interfaces;
using CarRental.Core.Models;
using CarRental.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CarRental.Data.Repositories
{
    public class FactureRepository : IFactureRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FactureRepository>? _logger;

        public FactureRepository(ApplicationDbContext context, ILogger<FactureRepository>? logger = null)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<IEnumerable<Facture>> GetFacturesByClientEmailAsync(string email)
        {
            return await _context.Factures
                .Include(f => f.Location)
                    .ThenInclude(l => l.Client)
                .Include(f => f.Location)
                    .ThenInclude(l => l.Vehicule)
                .Where(f => f.Location.Client.Email == email)
                .OrderByDescending(f => f.DateFacture)
                .ToListAsync();
        }

        public async Task<Facture?> GetByIdAsync(int id)
        {
            var facture = await _context.Factures
                .Include(f => f.Location)
                    .ThenInclude(l => l.Client)
                .Include(f => f.Location)
                    .ThenInclude(l => l.Vehicule)
                .FirstOrDefaultAsync(f => f.Id == id);

            // DÉBOGAGE
            if (facture != null)
            {
                System.Diagnostics.Debug.WriteLine($"Repository - Facture {facture.Id} récupérée");
                System.Diagnostics.Debug.WriteLine($"Repository - Location chargée: {facture.Location != null}");
                System.Diagnostics.Debug.WriteLine($"Repository - Client chargé: {facture.Location?.Client != null}");
            }

            return facture;
        }

        public async Task<Facture?> GetByLocationIdAsync(int locationId)
        {
            try
            {
                _logger?.LogInformation("🔍 Recherche facture pour LocationId={LocationId}", locationId);

                // ✅ SOLUTION DÉFINITIVE: SQL brut direct
                // Évite 100% le problème LocationId1 d'EF Core
                var facture = await _context.Factures
                    .FromSqlRaw(@"
                        SELECT Id, LocationId, DateFacture, MontantTotal, Format, CheminFichier 
                        FROM Factures 
                        WHERE LocationId = {0} 
                        LIMIT 1", locationId)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (facture != null)
                {
                    _logger?.LogInformation("✅ Facture trouvée: ID={Id}, Montant={Montant} MAD",
                        facture.Id, facture.MontantTotal);
                }
                else
                {
                    _logger?.LogInformation("ℹ️ Aucune facture pour LocationId={LocationId}", locationId);
                }

                return facture;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "❌ Erreur recherche facture LocationId={LocationId}", locationId);
                throw;
            }
        }

        public async Task<IEnumerable<Facture>> GetAllAsync()
        {
            try
            {
                // ✅ SQL brut
                return await _context.Factures
                    .FromSqlRaw(@"
                        SELECT Id, LocationId, DateFacture, MontantTotal, Format, CheminFichier 
                        FROM Factures 
                        ORDER BY DateFacture DESC")
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erreur récupération toutes factures");
                throw;
            }
        }

        public async Task<Facture> AddAsync(Facture facture)
        {
            try
            {
                _logger?.LogInformation("💾 Création facture pour LocationId={LocationId}", facture.LocationId);

                // ✅ IMPORTANT: Détacher la navigation property
                facture.Location = null;

                // Utiliser Add classique (pas de SQL brut pour INSERT)
                _context.Factures.Add(facture);
                await _context.SaveChangesAsync();

                _logger?.LogInformation("✅ Facture #{Id} créée avec succès", facture.Id);
                return facture;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "❌ Erreur création facture");
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                // Utiliser SQL brut pour SELECT
                var facture = await _context.Factures
                    .FromSqlRaw(@"
                        SELECT Id, LocationId, DateFacture, MontantTotal, Format, CheminFichier 
                        FROM Factures 
                        WHERE Id = {0}", id)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (facture != null)
                {
                    _context.Factures.Remove(facture);
                    await _context.SaveChangesAsync();
                    _logger?.LogInformation("✅ Facture #{Id} supprimée", id);
                }
                else
                {
                    _logger?.LogWarning("⚠️ Facture #{Id} introuvable pour suppression", id);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "❌ Erreur suppression facture #{Id}", id);
                throw;
            }
        }
    }
}