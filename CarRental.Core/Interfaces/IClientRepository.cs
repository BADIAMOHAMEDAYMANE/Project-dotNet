using CarRental.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CarRental.Core.Interfaces
{
    public interface IClientRepository
    {
        Task<Client> GetByCINAsync(string cin);
        Task<IEnumerable<Client>> GetAllAsync();
        Task<Client> GetByEmailAsync(string email);
        Task AddAsync(Client client);
        Task UpdateAsync(Client client);
        Task DeleteAsync(string cin);
        Task<bool> ExistsAsync(string cin);
        Task<bool> CINExistsAsync(string cin);
        Task<bool> EmailExistsAsync(string email);
    }
}