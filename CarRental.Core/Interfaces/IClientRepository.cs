using CarRental.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CarRental.Core.Interfaces
{
    public interface IClientRepository
    {
        // Méthodes de lecture
        Task<Client?> GetByCINAsync(string cin);
        Task<Client?> GetByEmailAsync(string email);
        Task<IEnumerable<Client>> GetAllAsync();

        // Méthodes d'écriture
        Task AddAsync(Client client);
        Task UpdateAsync(Client client);
        Task DeleteAsync(string cin);
        Task<bool> SaveChangesAsync();

        // Méthodes de vérification
        Task<bool> ExistsAsync(string cin);
        Task<bool> CINExistsAsync(string cin);
        Task<bool> EmailExistsAsync(string email);
        Task<Client?> GetWithLocationsAsync(string cin);
    }
}