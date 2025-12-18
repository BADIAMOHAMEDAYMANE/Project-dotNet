using CarRental.Core.Interfaces;
using CarRental.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CarRental.Core.Services
{
    public class ClientService : IClientService
    {
        private readonly IClientRepository _clientRepository;

        public ClientService(IClientRepository clientRepository)
        {
            _clientRepository = clientRepository;
        }

        public async Task<Client?> GetClientByCINAsync(string cin)
        {
            return await _clientRepository.GetByCINAsync(cin);
        }

        public async Task<Client?> GetClientWithLocationsAsync(string cin)
        {
            return await _clientRepository.GetWithLocationsAsync(cin);
        }

        public async Task<IEnumerable<Client>> GetAllClientsAsync()
        {
            return await _clientRepository.GetAllAsync();
        }

        public async Task<Client?> GetClientByEmailAsync(string email)
        {
            return await _clientRepository.GetByEmailAsync(email);
        }

        public async Task<Client> CreateClientAsync(Client client)
        {
            // Validation du CIN
            if (string.IsNullOrWhiteSpace(client.CIN))
                throw new ArgumentException("Le CIN est requis");

            if (await _clientRepository.CINExistsAsync(client.CIN))
                throw new InvalidOperationException($"Un client avec le CIN {client.CIN} existe déjà");

            // Validation de l'email
            if (await _clientRepository.EmailExistsAsync(client.Email))
                throw new InvalidOperationException($"Un client avec l'email {client.Email} existe déjà");

            // Validation du mot de passe
            if (string.IsNullOrWhiteSpace(client.Password))
                throw new ArgumentException("Le mot de passe est requis");

            if (client.Password.Length < 6)
                throw new ArgumentException("Le mot de passe doit contenir au moins 6 caractères");

            // Vérification de la correspondance des mots de passe
            if (client.Password != client.ConfirmPassword)
                throw new ArgumentException("Les mots de passe ne correspondent pas");

            // Calcul de l'âge
            var age = DateTime.Now.Year - client.DateNaissance.Year;
            if (DateTime.Now < client.DateNaissance.AddYears(age))
                age--;

            if (age < 18)
                throw new InvalidOperationException("Le client doit avoir au moins 18 ans");

            // Hasher le mot de passe avant de le sauvegarder
            client.Password = BCrypt.Net.BCrypt.HashPassword(client.Password);

            // Date d'inscription
            client.DateInscription = DateTime.Now;

            await _clientRepository.AddAsync(client);
            await _clientRepository.SaveChangesAsync();

            // Retourner le client sans le mot de passe pour la sécurité
            client.Password = string.Empty;
            client.ConfirmPassword = null;

            return client;
        }

        public async Task UpdateClientAsync(Client client)
        {
            if (!await _clientRepository.ExistsAsync(client.CIN))
                throw new KeyNotFoundException($"Client avec CIN {client.CIN} non trouvé");

            await _clientRepository.UpdateAsync(client);
            await _clientRepository.SaveChangesAsync();
        }

        public async Task DeleteClientAsync(string cin)
        {
            if (!await _clientRepository.ExistsAsync(cin))
                throw new KeyNotFoundException($"Client avec CIN {cin} non trouvé");

            await _clientRepository.DeleteAsync(cin);
            await _clientRepository.SaveChangesAsync();
        }

        public async Task<bool> ClientExistsAsync(string cin)
        {
            return await _clientRepository.ExistsAsync(cin);
        }

        public bool VerifyPassword(string enteredPassword, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(enteredPassword, hashedPassword);
        }

        // Méthode pour récupérer les données du dashboard - RETOURNE DashboardData
        public async Task<DashboardData> GetDashboardDataAsync(string cin)
        {
            var client = await _clientRepository.GetWithLocationsAsync(cin);

            if (client == null)
                throw new KeyNotFoundException($"Client avec CIN {cin} non trouvé");

            var locations = client.Locations ?? new List<Location>();

            // Calculer les statistiques
            var totalLocations = locations.Count;
            var activeLocations = locations.Count(l => l.Statut == "En cours");
            var completedLocations = locations.Count(l => l.Statut == "Terminée");
            var pendingLocations = locations.Count(l => l.Statut == "En attente");

            // Si les statuts ne sont pas définis, calculer par date
            if (totalLocations > 0 && (activeLocations + completedLocations + pendingLocations) == 0)
            {
                activeLocations = locations.Count(l => l.DateDebut <= DateTime.Now && l.DateFin > DateTime.Now);
                completedLocations = locations.Count(l => l.DateFin <= DateTime.Now);
                pendingLocations = locations.Count(l => l.DateDebut > DateTime.Now);
            }

            return new DashboardData
            {
                Client = client,
                Locations = locations.ToList(),
                TotalLocations = totalLocations,
                ActiveLocations = activeLocations,
                CompletedLocations = completedLocations,
                PendingLocations = pendingLocations
            };
        }
    }
}