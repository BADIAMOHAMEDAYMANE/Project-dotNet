using CarRental.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CarRental.Core.Interfaces {
    public interface ILocationService {
        
        Task<Location> GetLocationByIdAsync(int id);
        Task<IEnumerable<Location>> GetAllLocationsAsync();
    Task < Location > CreateLocationAsync(Location location);
        Task UpdateLocationAsync(Location location);
        Task DeleteLocationAsync(int id);
    Task < bool > LocationExistsAsync(int id);

    
    Task < IEnumerable < Location >> GetLocationsByClientAsync(string clientCIN);
    Task < IEnumerable < Location >> GetLocationsByVehiculeAsync(int vehiculeId);
    Task < IEnumerable < Location >> GetLocationsByStatutAsync(string statut);

    
    Task < bool > VehiculeEstDisponibleAsync(int vehiculeId, DateTime dateDebut, DateTime dateFin);
    Task < bool > ValidateLocationDatesAsync(DateTime dateDebut, DateTime dateFin);

        
        Task ConfirmLocationAsync(int id);
        Task CancelLocationAsync(int id);
        Task StartLocationAsync(int id);
        Task CompleteLocationAsync(int id);
   
    Task < decimal > CalculateLocationCostAsync(int id);
    Task < IEnumerable < Location >> GetLocationsByDateRangeAsync(DateTime dateDebut, DateTime dateFin);
    Task<IEnumerable<Location>> GetLocationsByClientCINAsync(string clientCIN);
    Task<IEnumerable<Location>> GetPendingLocationsAsync();
    Task<IEnumerable<Location>> GetTodayLocationsAsync();
    }
}