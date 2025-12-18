using CarRental.Core.Models;
using System.Threading.Tasks;

namespace CarRental.Core.Interfaces
{
    public interface IClientAuthService
    {
        Task<Client?> ValidateLoginAsync(string email, string password);
        Task RegisterClientAsync(Client client, string password);
    }
}