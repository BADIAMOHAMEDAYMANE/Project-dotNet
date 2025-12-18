using Microsoft.EntityFrameworkCore;
using CarRental.Core.Interfaces;
using CarRental.Core.Models;
using CarRental.Data;

namespace CarRental.Data.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private readonly ApplicationDbContext _context;

        public ClientRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Client?> GetByCINAsync(string cin)
        {
            return await _context.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CIN == cin);
        }

        public async Task<Client?> GetByEmailAsync(string email)
        {
            return await _context.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Email == email);
        }

        public async Task<IEnumerable<Client>> GetAllAsync()
        {
            return await _context.Clients
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddAsync(Client client)
        {
            await _context.Clients.AddAsync(client);
        }

        public async Task UpdateAsync(Client client)
        {
            _context.Clients.Update(client);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(string cin)
        {
            var client = await GetByCINAsync(cin);
            if (client != null)
            {
                _context.Clients.Remove(client);
            }
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> CINExistsAsync(string cin)
        {
            return await _context.Clients
                .AnyAsync(c => c.CIN == cin);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Clients
                .AnyAsync(c => c.Email == email);
        }

        public async Task<bool> ExistsAsync(string cin)
        {
            return await _context.Clients
                .AnyAsync(c => c.CIN == cin);
        }
        public async Task<Client?> GetWithLocationsAsync(string cin)
        {
            return await _context.Clients
                .Include(c => c.Locations)
                    .ThenInclude(l => l.Vehicule)
                .FirstOrDefaultAsync(c => c.CIN == cin);
        }
    }
}