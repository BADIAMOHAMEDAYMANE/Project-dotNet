using CarRental.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CarRental.Core.Interfaces
{
    public interface IVehiculeRepository
    {
        Task<Vehicule> GetByIdAsync(int id);
        Task<Vehicule> GetByImmatriculationAsync(string immatriculation);
        Task<IEnumerable<Vehicule>> GetAllAsync();
        Task<IEnumerable<Vehicule>> GetAllActiveAsync();
        Task<IEnumerable<Vehicule>> GetByStatutAsync(string statut);
        Task<IEnumerable<Vehicule>> GetByCategorieAsync(int categorieId);
        Task<IEnumerable<Vehicule>> GetByMarqueAsync(string marque);
        Task AddAsync(Vehicule vehicule);
        Task UpdateAsync(Vehicule vehicule);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task<bool> ImmatriculationExistsAsync(string immatriculation);
        Task<int> GetTotalVehiculesAsync();
        Task<int> GetVehiculesDisponiblesAsync();
    }
}