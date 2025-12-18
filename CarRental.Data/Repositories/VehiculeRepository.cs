using CarRental.Core.Models;
using CarRental.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CarRental.Data.Repositories
{
    public class VehiculeRepository : IVehiculeRepository
    {
        private readonly ApplicationDbContext _context;

        public VehiculeRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Vehicule> GetByIdAsync(int id)
        {
            return await _context.Vehicules
                .Include(v => v.Categorie)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<Vehicule> GetByImmatriculationAsync(string immatriculation)
        {
            return await _context.Vehicules
                .Include(v => v.Categorie)
                .FirstOrDefaultAsync(v => v.Immatriculation == immatriculation);
        }

        public async Task<IEnumerable<Vehicule>> GetAllAsync()
        {
            return await _context.Vehicules
                .Include(v => v.Categorie)
                .OrderBy(v => v.Marque)
                .ThenBy(v => v.Modele)
                .ToListAsync();
        }

        public async Task<IEnumerable<Vehicule>> GetAllActiveAsync()
        {
            return await _context.Vehicules
                .Include(v => v.Categorie)
                .Where(v => v.EstActif)
                .OrderBy(v => v.Marque)
                .ThenBy(v => v.Modele)
                .ToListAsync();
        }

        public async Task<IEnumerable<Vehicule>> GetByStatutAsync(string statut)
        {
            return await _context.Vehicules
                .Include(v => v.Categorie)
                .Where(v => v.Statut == statut && v.EstActif)
                .OrderBy(v => v.Marque)
                .ThenBy(v => v.Modele)
                .ToListAsync();
        }

        public async Task<IEnumerable<Vehicule>> GetByCategorieAsync(int categorieId)
        {
            return await _context.Vehicules
                .Include(v => v.Categorie)
                .Where(v => v.CategorieId == categorieId && v.EstActif)
                .OrderBy(v => v.Marque)
                .ThenBy(v => v.Modele)
                .ToListAsync();
        }

        public async Task<IEnumerable<Vehicule>> GetByMarqueAsync(string marque)
        {
            return await _context.Vehicules
                .Include(v => v.Categorie)
                .Where(v => v.Marque == marque && v.EstActif)
                .OrderBy(v => v.Modele)
                .ToListAsync();
        }

        public async Task AddAsync(Vehicule vehicule)
        {
            await _context.Vehicules.AddAsync(vehicule);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Vehicule vehicule)
        {
            // Check if there's already a tracked entity with the same ID
            var existingEntry = _context.ChangeTracker.Entries<Vehicule>()
                .FirstOrDefault(e => e.Entity.Id == vehicule.Id);

            if (existingEntry != null)
            {
                // Update the existing tracked entity instead of attaching a new one
                existingEntry.CurrentValues.SetValues(vehicule);

                // Make sure navigation properties are preserved
                if (vehicule.CategorieId.HasValue && existingEntry.Entity.CategorieId != vehicule.CategorieId)
                {
                    existingEntry.Entity.CategorieId = vehicule.CategorieId;
                }
            }
            else
            {
                // Try to find the entity in the database
                var existingVehicule = await _context.Vehicules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == vehicule.Id);

                if (existingVehicule != null)
                {
                    // Attach the new entity and mark as modified
                    _context.Vehicules.Attach(vehicule);
                    _context.Entry(vehicule).State = EntityState.Modified;
                }
                else
                {
                    // This shouldn't happen for updates, but just in case
                    _context.Vehicules.Update(vehicule);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var vehicule = await GetByIdAsync(id);
            if (vehicule != null)
            {
                _context.Vehicules.Remove(vehicule);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Vehicules.AnyAsync(v => v.Id == id);
        }

        public async Task<bool> ImmatriculationExistsAsync(string immatriculation)
        {
            return await _context.Vehicules.AnyAsync(v => v.Immatriculation == immatriculation);
        }

        public async Task<int> GetTotalVehiculesAsync()
        {
            return await _context.Vehicules.CountAsync(v => v.EstActif);
        }

        public async Task<int> GetVehiculesDisponiblesAsync()
        {
            return await _context.Vehicules
                .CountAsync(v => v.EstActif && v.Statut == "Disponible");
        }
    }
}