using CarRental.Core.Models;
using CarRental.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CarRental.Core.Services
{
    public class LocationService : ILocationService
    {
        private readonly ILocationRepository _locationRepository;
        private readonly IVehiculeRepository _vehiculeRepository;
        private readonly IClientRepository _clientRepository;

        public LocationService(
            ILocationRepository locationRepository,
            IVehiculeRepository vehiculeRepository,
            IClientRepository clientRepository)
        {
            _locationRepository = locationRepository;
            _vehiculeRepository = vehiculeRepository;
            _clientRepository = clientRepository;
        }

        public async Task<Location> GetLocationByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("ID de location invalide", nameof(id));

            var location = await _locationRepository.GetByIdAsync(id);
            if (location == null)
                throw new KeyNotFoundException($"Location avec ID {id} non trouvée");

            return location;
        }

        public async Task<IEnumerable<Location>> GetAllLocationsAsync()
        {
            return await _locationRepository.GetAllAsync();
        }

        // AJOUTEZ CETTE MÉTHODE :
        public async Task<IEnumerable<Location>> GetLocationsByClientCINAsync(string clientCIN)
        {
            if (string.IsNullOrWhiteSpace(clientCIN))
                throw new ArgumentException("Le CIN du client est requis", nameof(clientCIN));

            return await _locationRepository.GetByClientAsync(clientCIN);
        }

        public async Task<Location> CreateLocationAsync(Location location)
        {
            // Validation des données
            ValidateLocation(location);

            // Vérifier que le client existe
            var client = await _clientRepository.GetByCINAsync(location.ClientCIN);
            if (client == null)
            {
                throw new InvalidOperationException("Le client spécifié n'existe pas.");
            }

            // Vérifier que le véhicule existe et est disponible
            var vehicule = await _vehiculeRepository.GetByIdAsync(location.VehiculeId);
            if (vehicule == null)
            {
                throw new InvalidOperationException("Le véhicule spécifié n'existe pas.");
            }

            if (vehicule.Statut != "Disponible")
            {
                throw new InvalidOperationException("Le véhicule n'est pas disponible.");
            }

            // Vérifier la disponibilité pour les dates
            if (!await VehiculeEstDisponibleAsync(location.VehiculeId, location.DateDebut, location.DateFin))
            {
                throw new InvalidOperationException("Le véhicule n'est pas disponible pour ces dates.");
            }

            // Calculer le prix total
            var nombreJours = (location.DateFin - location.DateDebut).Days;
            if (nombreJours < 1) nombreJours = 1;
            location.PrixTotal = vehicule.PrixParJour * nombreJours;

            // Définir le statut par défaut
            location.Statut = "En attente";

            await _locationRepository.AddAsync(location);
            return location;
        }

        public async Task UpdateLocationAsync(Location location)
        {
            var existingLocation = await _locationRepository.GetByIdAsync(location.Id);
            if (existingLocation == null)
            {
                throw new KeyNotFoundException($"Location avec ID {location.Id} non trouvée.");
            }

            // Validation des données
            ValidateLocation(location);

            // Recalculer le prix si les dates ou le véhicule changent
            if (existingLocation.DateDebut != location.DateDebut ||
                existingLocation.DateFin != location.DateFin ||
                existingLocation.VehiculeId != location.VehiculeId)
            {
                var vehicule = await _vehiculeRepository.GetByIdAsync(location.VehiculeId);
                if (vehicule != null)
                {
                    var nombreJours = (location.DateFin - location.DateDebut).Days;
                    if (nombreJours < 1) nombreJours = 1;
                    location.PrixTotal = vehicule.PrixParJour * nombreJours;
                }
            }

            await _locationRepository.UpdateAsync(location);
        }

        public async Task DeleteLocationAsync(int id)
        {
            await _locationRepository.DeleteAsync(id);
        }

        public async Task<bool> LocationExistsAsync(int id)
        {
            return await _locationRepository.ExistsAsync(id);
        }

        public async Task<IEnumerable<Location>> GetLocationsByClientAsync(string clientCIN)
        {
            return await _locationRepository.GetByClientAsync(clientCIN);
        }

        public async Task<IEnumerable<Location>> GetLocationsByVehiculeAsync(int vehiculeId)
        {
            return await _locationRepository.GetByVehiculeAsync(vehiculeId);
        }

        public async Task<IEnumerable<Location>> GetLocationsByStatutAsync(string statut)
        {
            return await _locationRepository.GetByStatutAsync(statut);
        }

        public async Task<bool> VehiculeEstDisponibleAsync(int vehiculeId, DateTime dateDebut, DateTime dateFin)
        {
            return await _locationRepository.VehiculeEstDisponible(vehiculeId, dateDebut, dateFin);
        }

        public async Task<bool> ValidateLocationDatesAsync(DateTime dateDebut, DateTime dateFin)
        {
            // La date de début ne peut pas être dans le passé
            if (dateDebut.Date < DateTime.Today)
            {
                return false;
            }

            // La date de fin doit être après la date de début
            if (dateFin <= dateDebut)
            {
                return false;
            }

            // La durée ne peut pas dépasser 365 jours
            if ((dateFin - dateDebut).Days > 365)
            {
                return false;
            }

            return await Task.FromResult(true);
        }

        public async Task ConfirmLocationAsync(int id)
        {
            var location = await _locationRepository.GetByIdAsync(id);
            if (location == null)
            {
                throw new KeyNotFoundException($"Location avec ID {id} non trouvée.");
            }

            if (location.Statut != "En attente")
            {
                throw new InvalidOperationException("Seules les locations en attente peuvent être confirmées.");
            }

            location.Statut = "Approuvée";
            await _locationRepository.UpdateAsync(location);
        }

        public async Task CancelLocationAsync(int id)
        {
            var location = await _locationRepository.GetByIdAsync(id);
            if (location == null)
            {
                throw new KeyNotFoundException($"Location avec ID {id} non trouvée.");
            }

            if (location.Statut == "Terminée")
            {
                throw new InvalidOperationException("Impossible d'annuler une location terminée.");
            }

            location.Statut = "Annulée";
            await _locationRepository.UpdateAsync(location);
        }

        public async Task StartLocationAsync(int id)
        {
            var location = await _locationRepository.GetByIdAsync(id);
            if (location == null)
            {
                throw new KeyNotFoundException($"Location avec ID {id} non trouvée.");
            }

            if (location.Statut != "Approuvée" && location.Statut != "Confirmée")
            {
                throw new InvalidOperationException("Seules les locations approuvées peuvent être démarrées.");
            }

            if (location.DateDebut.Date > DateTime.Today)
            {
                throw new InvalidOperationException("La location ne peut pas démarrer avant la date prévue.");
            }

            location.Statut = "En cours";
            await _locationRepository.UpdateAsync(location);

            // Mettre à jour le statut du véhicule
            var vehicule = await _vehiculeRepository.GetByIdAsync(location.VehiculeId);
            if (vehicule != null)
            {
                vehicule.Statut = "Loué";
                await _vehiculeRepository.UpdateAsync(vehicule);
            }
        }

        public async Task CompleteLocationAsync(int id)
        {
            var location = await _locationRepository.GetByIdAsync(id);
            if (location == null)
            {
                throw new KeyNotFoundException($"Location avec ID {id} non trouvée.");
            }

            if (location.Statut != "En cours")
            {
                throw new InvalidOperationException("Seules les locations en cours peuvent être terminées.");
            }

            location.Statut = "Terminée";
            await _locationRepository.UpdateAsync(location);

            // Remettre le véhicule en disponible
            var vehicule = await _vehiculeRepository.GetByIdAsync(location.VehiculeId);
            if (vehicule != null)
            {
                vehicule.Statut = "Disponible";
                await _vehiculeRepository.UpdateAsync(vehicule);
            }
        }

        public async Task<decimal> CalculateLocationCostAsync(int locationId)
        {
            return await _locationRepository.CalculerMontantTotal(locationId);
        }

        public async Task<IEnumerable<Location>> GetLocationsByDateRangeAsync(DateTime dateDebut, DateTime dateFin)
        {
            return await _locationRepository.GetLocationsByDateRangeAsync(dateDebut, dateFin);
        }

        // Méthode privée de validation
        private void ValidateLocation(Location location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));

            if (string.IsNullOrWhiteSpace(location.ClientCIN))
                throw new ArgumentException("Le CIN du client est requis");

            if (location.VehiculeId <= 0)
                throw new ArgumentException("L'ID du véhicule est invalide");

            if (location.DateDebut == default)
                throw new ArgumentException("La date de début est requise");

            if (location.DateFin == default)
                throw new ArgumentException("La date de fin est requise");

            if (location.DateFin <= location.DateDebut)
                throw new ArgumentException("La date de fin doit être postérieure à la date de début");
        }
        public async Task<IEnumerable<Location>> GetPendingLocationsAsync()
        {
            var locations = await _locationRepository.GetAllAsync();
            return locations.Where(l => l.Statut == "En attente");
        }

        public async Task<IEnumerable<Location>> GetTodayLocationsAsync()
        {
            var locations = await _locationRepository.GetAllAsync();
            return locations.Where(l => l.DateDebut.Date == DateTime.Today.Date);
        }
    }
}