using CarRental.Core.Models;
using CarRental.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CarRental.Data.Repositories
{
    public class LocationRepository : ILocationRepository
    {
        private readonly ApplicationDbContext _context;

        public LocationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        private DbSet<Location> Locations => _context.Locations;

        public async Task<Location> GetByIdAsync(int id)
        {
            return await Locations
                .Include(l => l.Client)
                .Include(l => l.Vehicule)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<IEnumerable<Location>> GetAllAsync()
        {
            return await Locations
                .Include(l => l.Client)
                .Include(l => l.Vehicule)
                .OrderByDescending(l => l.DateDebut)
                .ToListAsync();
        }

        public async Task<IEnumerable<Location>> GetByClientAsync(string clientCIN)
        {
            return await Locations
                .Include(l => l.Client)
                .Include(l => l.Vehicule)
                .Where(l => l.ClientCIN == clientCIN)
                .OrderByDescending(l => l.DateDebut)
                .ToListAsync();
        }

        public async Task<IEnumerable<Location>> GetByVehiculeAsync(int vehiculeId)
        {
            return await Locations
                .Include(l => l.Client)
                .Include(l => l.Vehicule)
                .Where(l => l.VehiculeId == vehiculeId)
                .OrderByDescending(l => l.DateDebut)
                .ToListAsync();
        }

        public async Task<IEnumerable<Location>> GetByStatutAsync(string statut)
        {
            return await Locations
                .Include(l => l.Client)
                .Include(l => l.Vehicule)
                .Where(l => l.Statut == statut)
                .OrderByDescending(l => l.DateDebut)
                .ToListAsync();
        }

        public async Task AddAsync(Location location)
        {
            // Vérifier la disponibilité du véhicule
            bool disponible = await VehiculeEstDisponible(
                location.VehiculeId,
                location.DateDebut,
                location.DateFin
            );

            if (!disponible)
            {
                throw new InvalidOperationException(
                    "Le véhicule n'est pas disponible pour les dates sélectionnées.");
            }

            // Définir le statut initial
            location.Statut = "En attente";

            await Locations.AddAsync(location);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Location location)
        {
            var existingLocation = await GetByIdAsync(location.Id);

            if (existingLocation == null)
            {
                throw new KeyNotFoundException($"Location avec ID {location.Id} non trouvée.");
            }

            // Si on change les dates ou le véhicule, vérifier la disponibilité
            if (existingLocation.VehiculeId != location.VehiculeId ||
                existingLocation.DateDebut != location.DateDebut ||
                existingLocation.DateFin != location.DateFin)
            {
                bool disponible = await VehiculeEstDisponibleExcludingLocation(
                    location.VehiculeId,
                    location.DateDebut,
                    location.DateFin,
                    location.Id
                );

                if (!disponible)
                {
                    throw new InvalidOperationException(
                        "Le véhicule n'est pas disponible pour les nouvelles dates sélectionnées.");
                }
            }

            _context.Entry(existingLocation).CurrentValues.SetValues(location);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var location = await GetByIdAsync(id);
            if (location != null)
            {
                if (location.Statut == "Terminée" || location.Statut == "Annulée" || location.Statut == "Rejetée")
                {
                    Locations.Remove(location);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    throw new InvalidOperationException(
                        "Impossible de supprimer une location en attente, approuvée ou en cours.");
                }
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await Locations.AnyAsync(l => l.Id == id);
        }

        public async Task<bool> VehiculeEstDisponible(int vehiculeId, DateTime dateDebut, DateTime dateFin)
        {
            if (dateFin <= dateDebut)
            {
                throw new ArgumentException("La date de fin doit être postérieure à la date de début.");
            }

            var locationsActives = new[] { "En attente", "Approuvée", "Confirmée", "En cours" };

            bool chevauchement = await Locations
                .Where(l => l.VehiculeId == vehiculeId)
                .Where(l => locationsActives.Contains(l.Statut))
                .AnyAsync(l =>
                    (dateDebut >= l.DateDebut && dateDebut < l.DateFin) ||
                    (dateFin > l.DateDebut && dateFin <= l.DateFin) ||
                    (dateDebut <= l.DateDebut && dateFin >= l.DateFin)
                );

            return !chevauchement;
        }

        private async Task<bool> VehiculeEstDisponibleExcludingLocation(
            int vehiculeId, DateTime dateDebut, DateTime dateFin, int locationIdToExclude)
        {
            if (dateFin <= dateDebut)
            {
                throw new ArgumentException("La date de fin doit être postérieure à la date de début.");
            }

            var locationsActives = new[] { "En attente", "Approuvée", "Confirmée", "En cours" };

            bool chevauchement = await Locations
                .Where(l => l.VehiculeId == vehiculeId)
                .Where(l => l.Id != locationIdToExclude)
                .Where(l => locationsActives.Contains(l.Statut))
                .AnyAsync(l =>
                    (dateDebut >= l.DateDebut && dateDebut < l.DateFin) ||
                    (dateFin > l.DateDebut && dateFin <= l.DateFin) ||
                    (dateDebut <= l.DateDebut && dateFin >= l.DateFin)
                );

            return !chevauchement;
        }

        public async Task<IEnumerable<Location>> GetLocationsEnCoursAsync()
        {
            return await Locations
                .Include(l => l.Client)
                .Include(l => l.Vehicule)
                .Where(l => l.Statut == "En cours")
                .OrderBy(l => l.DateDebut)
                .ToListAsync();
        }

        public async Task<IEnumerable<Location>> GetLocationsAVenirAsync()
        {
            return await Locations
                .Include(l => l.Client)
                .Include(l => l.Vehicule)
                .Where(l => (l.Statut == "Approuvée" || l.Statut == "Confirmée")
                            && l.DateDebut > DateTime.Now)
                .OrderBy(l => l.DateDebut)
                .ToListAsync();
        }

        public async Task<IEnumerable<Location>> GetLocationsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await Locations
                .Include(l => l.Client)
                .Include(l => l.Vehicule)
                .Where(l => l.DateDebut >= startDate && l.DateDebut <= endDate)
                .OrderByDescending(l => l.DateDebut)
                .ToListAsync();
        }

        public async Task<decimal> CalculerMontantTotal(int locationId)
        {
            var location = await GetByIdAsync(locationId);
            if (location == null)
                return 0;

            // Si le prix total existe déjà, le retourner
            if (location.PrixTotal.HasValue)
                return location.PrixTotal.Value;

            // Sinon calculer à partir du véhicule
            var nombreJours = (location.DateFin - location.DateDebut).Days;
            if (nombreJours < 1) nombreJours = 1;

            if (location.Vehicule != null)
            {
                return location.Vehicule.PrixParJour * nombreJours;
            }

            return 0;
        }

        // Implementation of the missing interface method
        public async Task<bool> ClientALoueVehiculeAsync(string clientCIN, int vehiculeId)
        {
            return await Locations
                .AnyAsync(l => l.ClientCIN == clientCIN &&
                               l.VehiculeId == vehiculeId &&
                               l.Statut != "Rejetée" &&
                               l.Statut != "Annulée");
        }
    }
}