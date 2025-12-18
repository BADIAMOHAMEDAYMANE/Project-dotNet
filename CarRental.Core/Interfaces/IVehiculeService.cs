using CarRental.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CarRental.Core.Interfaces
{
    public interface IVehiculeService
    {
        Task<Vehicule> GetVehiculeByIdAsync(int id);
        Task<Vehicule> GetVehiculeByImmatriculationAsync(string immatriculation);
        Task<IEnumerable<Vehicule>> GetAllVehiculesAsync();
        Task<IEnumerable<Vehicule>> GetAllActiveVehiculesAsync();
        Task<IEnumerable<Vehicule>> GetVehiculesByStatutAsync(string statut);
        Task<IEnumerable<Vehicule>> GetVehiculesByCategorieAsync(int categorieId);
        Task<IEnumerable<Vehicule>> GetVehiculesByMarqueAsync(string marque);
        Task<IEnumerable<Vehicule>> GetVehiculesDisponiblesAsync();
        Task<Vehicule> CreateVehiculeAsync(Vehicule vehicule);
        Task UpdateVehiculeAsync(Vehicule vehicule);
        Task DeleteVehiculeAsync(int id);
        Task<bool> VehiculeExistsAsync(int id);
        Task UpdateStatutAsync(int id, string statut);
        Task UpdateKilometrageAsync(int id, int kilometrage);
        Task EnregistrerEntretienAsync(int id, int kilometrage);
        Task<int> GetTotalVehiculesAsync();
        Task<int> GetVehiculesDisponiblesCountAsync();
    }
}