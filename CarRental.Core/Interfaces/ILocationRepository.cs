using CarRental.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CarRental.Core.Interfaces
{
    public interface ILocationRepository
    {
        // Opérations CRUD de base
        Task<Location> GetByIdAsync(int id);
        Task<IEnumerable<Location>> GetAllAsync();
        Task AddAsync(Location location);
        Task UpdateAsync(Location location);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);

        // Recherches spécifiques
        Task<IEnumerable<Location>> GetByClientAsync(string clientCIN);
        Task<IEnumerable<Location>> GetByVehiculeAsync(int vehiculeId);
        Task<IEnumerable<Location>> GetByStatutAsync(string statut);
        Task<IEnumerable<Location>> GetLocationsByDateRangeAsync(DateTime startDate, DateTime endDate);

        // Recherches par statut spécifique
        Task<IEnumerable<Location>> GetLocationsEnCoursAsync();
        Task<IEnumerable<Location>> GetLocationsAVenirAsync();

        // Vérifications de disponibilité
        Task<bool> VehiculeEstDisponible(int vehiculeId, DateTime dateDebut, DateTime dateFin);

        // Calculs
        Task<decimal> CalculerMontantTotal(int locationId);

        // Statistiques
        Task<bool> ClientALoueVehiculeAsync(string clientCIN, int vehiculeId);
    }
}