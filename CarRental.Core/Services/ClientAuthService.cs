using CarRental.Core.Interfaces;
using CarRental.Core.Models;
using Microsoft.Extensions.Logging;

namespace CarRental.Core.Services
{
    public class ClientAuthService : IClientAuthService
    {
        private readonly IClientRepository _clientRepository;
        private readonly ILogger<ClientAuthService> _logger;

        public ClientAuthService(
            IClientRepository clientRepository,
            ILogger<ClientAuthService> logger)
        {
            _clientRepository = clientRepository;
            _logger = logger;
        }

        public async Task<Client?> ValidateLoginAsync(string email, string password)
        {
            try
            {
                _logger.LogInformation("Validation du login pour l'email: {Email}", email);

                // Récupérer le client par email
                var client = await _clientRepository.GetByEmailAsync(email);

                if (client == null)
                {
                    _logger.LogWarning("Client non trouvé pour l'email: {Email}", email);
                    return null;
                }

                _logger.LogDebug("Client trouvé: {Nom} {Prenom} (CIN: {CIN})",
                    client.Nom, client.Prenom, client.CIN);

                // Vérifier le mot de passe avec BCrypt
                if (BCrypt.Net.BCrypt.Verify(password, client.Password))
                {
                    _logger.LogInformation("✅ Authentification réussie pour {Nom} {Prenom}",
                        client.Nom, client.Prenom);

                    // Retourner le client sans le mot de passe (pour la sécurité)
                    var clientResponse = new Client
                    {
                        CIN = client.CIN,
                        Nom = client.Nom,
                        Prenom = client.Prenom,
                        Email = client.Email,
                        Telephone = client.Telephone,
                        DateNaissance = client.DateNaissance,
                        Adresse = client.Adresse,
                        Ville = client.Ville,
                        CodePostal = client.CodePostal,
                        NumeroPermis = client.NumeroPermis,
                        DateInscription = client.DateInscription
                    };

                    clientResponse.Password = string.Empty; // Sécurité

                    return clientResponse;
                }

                _logger.LogWarning("❌ Mot de passe incorrect pour {Email}", email);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la validation du login pour {Email}", email);
                return null;
            }
        }

        public async Task RegisterClientAsync(Client client, string password)
        {
            try
            {
                _logger.LogInformation("Enregistrement d'un nouveau client: {Email}", client.Email);

                // Validation
                if (string.IsNullOrWhiteSpace(client.Email))
                    throw new ArgumentException("L'email est requis.");

                if (string.IsNullOrWhiteSpace(password))
                    throw new ArgumentException("Le mot de passe est requis.");

                if (string.IsNullOrWhiteSpace(client.CIN))
                    throw new ArgumentException("Le CIN est requis.");

                // Vérifier l'unicité
                if (await _clientRepository.EmailExistsAsync(client.Email))
                    throw new InvalidOperationException($"L'email {client.Email} est déjà utilisé.");

                if (await _clientRepository.CINExistsAsync(client.CIN))
                    throw new InvalidOperationException($"Le CIN {client.CIN} est déjà utilisé.");

                // Hashage du mot de passe avec BCrypt
                client.Password = BCrypt.Net.BCrypt.HashPassword(password);
                client.DateInscription = DateTime.Now;

                // Enregistrer
                await _clientRepository.AddAsync(client);
                await _clientRepository.SaveChangesAsync();

                _logger.LogInformation("✅ Client enregistré avec succès: {Nom} {Prenom}",
                    client.Nom, client.Prenom);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'enregistrement du client: {Email}", client.Email);
                throw;
            }
        }
    }
}