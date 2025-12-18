using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CarRental.Core.Interfaces;
using CarRental.Core.Models;

namespace CarRental.Core.Services
{
    public class AdminService : IAdminService
    {
        private readonly IAdminRepository _adminRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IClientRepository _clientRepository;
        private readonly IVehiculeRepository _vehiculeRepository;
        private readonly IAuthService _authService;
        private readonly ILogger<AdminService> _logger;

        public AdminService(
            IAdminRepository adminRepository,
            IEmployeeRepository employeeRepository,
            IClientRepository clientRepository,
            IVehiculeRepository vehiculeRepository,
            IAuthService authService,
            ILogger<AdminService> logger)
        {
            _adminRepository = adminRepository;
            _employeeRepository = employeeRepository;
            _clientRepository = clientRepository;
            _vehiculeRepository = vehiculeRepository;
            _authService = authService;
            _logger = logger;
        }

        // Dashboard - Statistiques
        public async Task<DashboardStatistics> GetDashboardStatisticsAsync()
        {
            try
            {
                var statistics = new DashboardStatistics
                {
                    TotalEmployees = await _adminRepository.GetTotalEmployeesAsync(),
                    TotalClients = await _adminRepository.GetTotalClientsAsync(),
                    TotalVehicles = await _adminRepository.GetTotalVehiclesAsync(),
                    PendingRequests = await _adminRepository.GetPendingRequestsAsync(),
                    VehiclesInMaintenance = await _adminRepository.GetVehiclesInMaintenanceAsync(),
                    ReturnsToday = await _adminRepository.GetReturnsTodayAsync(),
                    Employees = await _adminRepository.GetAllEmployeesAsync(),
                    Clients = await _adminRepository.GetAllClientsAsync(),
                    Vehicles = await _adminRepository.GetAllVehiclesAsync(),
                    PendingLocations = await _adminRepository.GetPendingLocationsAsync()
                };

                _logger.LogInformation("Statistiques du dashboard récupérées avec succès");
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques du dashboard");
                throw;
            }
        }

        // Gestion des employés
        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        {
            try
            {
                return await _adminRepository.GetAllEmployeesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des employés");
                throw;
            }
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            try
            {
                return await _employeeRepository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'employé {Id}", id);
                throw;
            }
        }

        public async Task<bool> CreateEmployeeAsync(Employee employee, string password)
        {
            try
            {
                await _authService.RegisterEmployeeAsync(employee, password);
                _logger.LogInformation("Employé créé: {Email}", employee.Email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de l'employé {Email}", employee.Email);
                return false;
            }
        }

        public async Task<bool> UpdateEmployeeAsync(Employee employee)
        {
            try
            {
                await _employeeRepository.UpdateAsync(employee);
                _logger.LogInformation("Employé mis à jour: {Id}", employee.ID);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'employé {Id}", employee.ID);
                return false;
            }
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            try
            {
                await _employeeRepository.DeleteAsync(id);
                _logger.LogInformation("Employé supprimé: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'employé {Id}", id);
                return false;
            }
        }

        // Gestion des clients - UTILISE CIN
        public async Task<IEnumerable<Client>> GetAllClientsAsync()
        {
            try
            {
                return await _adminRepository.GetAllClientsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des clients");
                throw;
            }
        }

        public async Task<Client?> GetClientByCINAsync(string cin)
        {
            try
            {
                return await _clientRepository.GetByCINAsync(cin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du client {CIN}", cin);
                throw;
            }
        }

        public async Task<bool> DeleteClientAsync(string cin)
        {
            try
            {
                await _clientRepository.DeleteAsync(cin);
                _logger.LogInformation("Client supprimé: CIN {CIN}", cin);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du client {CIN}", cin);
                return false;
            }
        }

        // Gestion des véhicules
        public async Task<IEnumerable<Vehicule>> GetAllVehiclesAsync()
        {
            try
            {
                return await _adminRepository.GetAllVehiclesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des véhicules");
                throw;
            }
        }

        public async Task<Vehicule?> GetVehiculeByIdAsync(int id)
        {
            try
            {
                return await _vehiculeRepository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du véhicule {Id}", id);
                throw;
            }
        }

        public async Task<bool> DeleteVehiculeAsync(int id)
        {
            try
            {
                await _vehiculeRepository.DeleteAsync(id);
                _logger.LogInformation("Véhicule supprimé: {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du véhicule {Id}", id);
                return false;
            }
        }

        // Gestion des demandes de location
        public async Task<IEnumerable<Location>> GetPendingLocationsAsync()
        {
            try
            {
                return await _adminRepository.GetPendingLocationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des demandes en attente");
                throw;
            }
        }

        public async Task<bool> ApproveLocationAsync(int locationId)
        {
            try
            {
                var result = await _adminRepository.ApproveLocationAsync(locationId);
                if (result)
                {
                    _logger.LogInformation("Location approuvée: {LocationId}", locationId);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'approbation de la location {LocationId}", locationId);
                return false;
            }
        }

        public async Task<bool> RejectLocationAsync(int locationId)
        {
            try
            {
                var result = await _adminRepository.RejectLocationAsync(locationId);
                if (result)
                {
                    _logger.LogInformation("Location rejetée: {LocationId}", locationId);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du rejet de la location {LocationId}", locationId);
                return false;
            }
        }

        // Activités récentes
        public async Task<IEnumerable<ActivityLog>> GetRecentActivitiesAsync(int count = 10)
        {
            try
            {
                return await _adminRepository.GetRecentActivitiesAsync(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des activités récentes");
                throw;
            }
        }
    }
}