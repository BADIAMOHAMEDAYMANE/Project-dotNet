using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CarRental.Core.Interfaces;
using CarRental.Core.Models;
using CarRental.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarRental.Data.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmployeeRepository> _logger;

        public EmployeeRepository(
            ApplicationDbContext context,
            ILogger<EmployeeRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Employee?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Employees.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'employé ID: {Id}", id);
                throw;
            }
        }

        public async Task<Employee?> GetByEmailAsync(string email)
        {
            try
            {
                return await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'employé Email: {Email}", email);
                throw;
            }
        }

        public async Task<IEnumerable<Employee>> GetAllAsync()
        {
            try
            {
                return await _context.Employees
                    .OrderBy(e => e.Nom)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de tous les employés");
                throw;
            }
        }

        public async Task<Employee> AddAsync(Employee employee)
        {
            try
            {
                
                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Employé ajouté: {Nom} {Prenom} (ID: {Id})", 
                    employee.Nom, employee.Prenom, employee.ID);
                
                return employee;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'ajout de l'employé");
                throw;
            }
        }

        public async Task<Employee> UpdateAsync(Employee employee)
        {
            try
            {
                _context.Entry(employee).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Employé mis à jour: {Nom} {Prenom} (ID: {Id})", 
                    employee.Nom, employee.Prenom, employee.ID);
                
                return employee;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'employé ID: {Id}", employee.ID);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var employee = await GetByIdAsync(id);
                if (employee == null)
                {
                    return false;
                }

                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Employé supprimé: {Nom} {Prenom} (ID: {Id})", 
                    employee.Nom, employee.Prenom, employee.ID);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'employé ID: {Id}", id);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Employees.AnyAsync(e => e.ID == id);
        }
    }
}