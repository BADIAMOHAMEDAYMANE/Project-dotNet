using CarRental.Core.Interfaces;
using CarRental.Core.Models;
using System;
using System.Collections.Generic;
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

        public async Task<Client> GetClientByCINAsync(string cin)
        {
            return await _clientRepository.GetByCINAsync(cin);
        }

        public async Task<IEnumerable<Client>> GetAllClientsAsync()
        {
            return await _clientRepository.GetAllAsync();
        }

        public async Task<Client> GetClientByEmailAsync(string email)
        {
            return await _clientRepository.GetByEmailAsync(email);
        }

        public async Task<Client> CreateClientAsync(Client client)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(client.CIN))
                throw new ArgumentException("Le CIN est requis");

            if (await _clientRepository.CINExistsAsync(client.CIN))
                throw new InvalidOperationException($"Un client avec le CIN {client.CIN} existe déjà");

            if (await _clientRepository.EmailExistsAsync(client.Email))
                throw new InvalidOperationException($"Un client avec l'email {client.Email} existe déjà");

            // Calcul de l'âge
            var age = DateTime.Now.Year - client.DateNaissance.Year;
            if (DateTime.Now < client.DateNaissance.AddYears(age))
                age--;

            if (age < 18)
                throw new InvalidOperationException("Le client doit avoir au moins 18 ans");

            await _clientRepository.AddAsync(client);
            return client;
        }

        public async Task UpdateClientAsync(Client client)
        {
            if (!await _clientRepository.ExistsAsync(client.CIN))
                throw new KeyNotFoundException($"Client avec CIN {client.CIN} non trouvé");

            await _clientRepository.UpdateAsync(client);
        }

        public async Task DeleteClientAsync(string cin)
        {
            if (!await _clientRepository.ExistsAsync(cin))
                throw new KeyNotFoundException($"Client avec CIN {cin} non trouvé");

            await _clientRepository.DeleteAsync(cin);
        }

        public async Task<bool> ClientExistsAsync(string cin)
        {
            return await _clientRepository.ExistsAsync(cin);
        }
    }
}