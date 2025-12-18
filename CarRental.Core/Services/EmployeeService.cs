using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using CarRental.Core.Interfaces;
using CarRental.Core.Models;
using System.Security.Cryptography;
using System.Text;
namespace CarRental.Core.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(
            IEmployeeRepository employeeRepository,
            ILogger<EmployeeService> logger)
        {
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int id)
        {
            return await _employeeRepository.GetByIdAsync(id);
        }

        public async Task<Employee?> GetEmployeeByEmailAsync(string email)
        {
            return await _employeeRepository.GetByEmailAsync(email);
        }

        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        {
            return await _employeeRepository.GetAllAsync();
        }

        public async Task<Employee> CreateEmployeeAsync(Employee employee)
        {
            // Hasher le mot de passe avant de l'enregistrer
            employee.PasswordHash = HashPassword(employee.PasswordHash);
            return await _employeeRepository.AddAsync(employee);
        }

        public async Task<Employee> UpdateEmployeeAsync(Employee employee)
        {
            return await _employeeRepository.UpdateAsync(employee);
        }

        public async Task<bool> DeleteEmployeeAsync(int id)
        {
            return await _employeeRepository.DeleteAsync(id);
        }

        public async Task<bool> VerifyEmployeeCredentialsAsync(string email, string password)
        {
            var employee = await _employeeRepository.GetByEmailAsync(email);
            if (employee == null)
                return false;

            return VerifyPassword(password, employee.PasswordHash);
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLower();
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            var hashedPassword = HashPassword(password);
            return hashedPassword == storedHash;
        }
    }
}