using CarRental.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CarRental.Core.Interfaces
{
	public interface IFactureRepository
	{
		Task<Facture?> GetByIdAsync(int id);
		Task<Facture?> GetByLocationIdAsync(int locationId);
		Task<IEnumerable<Facture>> GetAllAsync();
		Task<Facture> AddAsync(Facture facture);
		Task DeleteAsync(int id);
        Task<IEnumerable<Facture>> GetFacturesByClientEmailAsync(string email);
    }
}