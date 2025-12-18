using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CarRental.Core.Models;

namespace CarRental.Core.Interfaces
{
    public interface IAdminRepository
    {
        // Statistiques
        Task<int> GetTotalEmployeesAsync();
        Task<int> GetTotalClientsAsync();
        Task<int> GetTotalVehiclesAsync();
        Task<int> GetPendingRequestsAsync();
        Task<int> GetVehiclesInMaintenanceAsync();
        Task<int> GetReturnsTodayAsync();

        // Récupération des listes
        Task<IEnumerable<Employee>> GetAllEmployeesAsync();
        Task<IEnumerable<Client>> GetAllClientsAsync();
        Task<IEnumerable<Vehicule>> GetAllVehiclesAsync();
        Task<IEnumerable<Location>> GetPendingLocationsAsync();

        // Gestion des demandes
        Task<Location?> GetLocationByIdAsync(int locationId);
        Task<bool> ApproveLocationAsync(int locationId);
        Task<bool> RejectLocationAsync(int locationId);

        // Activités récentes
        Task<IEnumerable<ActivityLog>> GetRecentActivitiesAsync(int count = 10);
    }
}