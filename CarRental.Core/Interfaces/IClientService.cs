using CarRental.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CarRental.Core.Interfaces
{
    public interface IClientService
    {
        Task<Client> GetClientByCINAsync(string cin);
        Task<IEnumerable<Client>> GetAllClientsAsync();
        Task<Client> GetClientByEmailAsync(string email);
        Task<Client> CreateClientAsync(Client client);
        Task UpdateClientAsync(Client client);
        Task DeleteClientAsync(string cin);
        Task<bool> ClientExistsAsync(string cin);
    }
}