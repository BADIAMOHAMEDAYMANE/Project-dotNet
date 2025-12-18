using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CarRental.Core.Models;

namespace CarRental.Core.Interfaces
{
    public interface IAdminService
    {
        // Dashboard - Statistiques
        Task<DashboardStatistics> GetDashboardStatisticsAsync();

        // Gestion des employés
        Task<IEnumerable<Employee>> GetAllEmployeesAsync();
        Task<Employee?> GetEmployeeByIdAsync(int id);
        Task<bool> CreateEmployeeAsync(Employee employee, string password);
        Task<bool> UpdateEmployeeAsync(Employee employee);
        Task<bool> DeleteEmployeeAsync(int id);

        // Gestion des clients - UTILISE CIN
        Task<IEnumerable<Client>> GetAllClientsAsync();
        Task<Client?> GetClientByCINAsync(string cin);
        Task<bool> DeleteClientAsync(string cin);

        // Gestion des véhicules
        Task<IEnumerable<Vehicule>> GetAllVehiclesAsync();
        Task<Vehicule?> GetVehiculeByIdAsync(int id);
        Task<bool> DeleteVehiculeAsync(int id);

        // Gestion des demandes de location
        Task<IEnumerable<Location>> GetPendingLocationsAsync();
        Task<bool> ApproveLocationAsync(int locationId);
        Task<bool> RejectLocationAsync(int locationId);

        // Activités récentes
        Task<IEnumerable<ActivityLog>> GetRecentActivitiesAsync(int count = 10);
    }

    // Classe pour les statistiques du dashboard
    public class DashboardStatistics
    {
        public int TotalEmployees { get; set; }
        public int TotalClients { get; set; }
        public int TotalVehicles { get; set; }
        public int PendingRequests { get; set; }
        public int VehiclesInMaintenance { get; set; }
        public int ReturnsToday { get; set; }
        public IEnumerable<Employee> Employees { get; set; } = new List<Employee>();
        public IEnumerable<Client> Clients { get; set; } = new List<Client>();
        public IEnumerable<Vehicule> Vehicles { get; set; } = new List<Vehicule>();
        public IEnumerable<Location> PendingLocations { get; set; } = new List<Location>();
    }

    // Classe pour le log d'activité
    public class ActivityLog
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // success, info, warning
    }
}