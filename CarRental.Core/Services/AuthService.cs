using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CarRental.Core.Interfaces;
using CarRental.Core.Models;
using Microsoft.Extensions.Logging;

namespace CarRental.Core.Services
{
    public class AuthService : IAuthService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IEmployeeRepository employeeRepository,
            ILogger<AuthService> logger)
        {
            _employeeRepository = employeeRepository;
            _logger = logger;
        }

        // Hash du mot de passe avec SHA256
        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }

        // Vérifier le mot de passe
        public bool VerifyPassword(string password, string hashedPassword)
        {
            string hash = HashPassword(password);
            return hash == hashedPassword;
        }

        // Valider les identifiants de connexion
        public async Task<Employee?> ValidateLoginAsync(string email, string password)
        {
            try
            {
                _logger.LogInformation("Tentative de connexion pour l'email: {Email}", email);

                var employee = await _employeeRepository.GetByEmailAsync(email);

                if (employee == null)
                {
                    _logger.LogWarning("Aucun employé trouvé avec l'email: {Email}", email);
                    return null;
                }

                string hashedPassword = HashPassword(password);

                if (employee.PasswordHash != hashedPassword)
                {
                    _logger.LogWarning("Mot de passe incorrect pour l'email: {Email}", email);
                    return null;
                }

                _logger.LogInformation("Connexion réussie pour {Nom} {Prenom} (Role: {Role})", 
                    employee.Nom, employee.Prenom, employee.Role);

                return employee;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la validation de connexion pour l'email: {Email}", email);
                return null;
            }
        }

        // Enregistrer un nouvel employé
        public async Task<Employee> RegisterEmployeeAsync(Employee employee, string password)
        {
            try
            {
                _logger.LogInformation("Tentative d'enregistrement d'un nouvel employé: {Email}", employee.Email);

                // Vérifier si l'email existe déjà
                var existingByEmail = await _employeeRepository.GetByEmailAsync(employee.Email);
                if (existingByEmail != null)
                {
                    _logger.LogWarning("Un employé existe déjà avec l'email: {Email}", employee.Email);
                    throw new InvalidOperationException("Un employé avec cet email existe déjà");
                }

                // Hash du mot de passe
                employee.PasswordHash = HashPassword(password);
                employee.DateCreation = DateTime.Now;

                // Enregistrer l'employé
                await _employeeRepository.AddAsync(employee);

                _logger.LogInformation("Employé enregistré avec succès: {Nom} {Prenom}", 
                    employee.Nom, employee.Prenom);

                return employee;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'enregistrement d'un employé");
                throw;
            }
        }
    }
}