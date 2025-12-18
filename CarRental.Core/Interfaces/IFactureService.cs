using System.Collections.Generic;
using System.Threading.Tasks;
using CarRental.Core.Models;

namespace CarRental.Core.Interfaces
{
    public interface IFactureService
    {
        
        
        Task<Facture?> GetByIdAsync(int id);
        Task<Facture?> GetByLocationIdAsync(int locationId);
        Task<IEnumerable<Facture>> GetAllAsync();
        Task<Facture> CreateFactureAsync(Location location, string format);
        Task DeleteAsync(int id);
        Task<bool> FactureExistePourLocationAsync(int locationId);
        Task<IEnumerable<Facture>> GetFacturesByClientEmailAsync(string email);
    }
}