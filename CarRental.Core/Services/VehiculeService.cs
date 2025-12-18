using CarRental.Core.Interfaces;
using CarRental.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CarRental.Core.Services
{
    public class VehiculeService : IVehiculeService
    {
        private readonly IVehiculeRepository _vehiculeRepository;

        public VehiculeService(IVehiculeRepository vehiculeRepository)
        {
            _vehiculeRepository = vehiculeRepository;
        }

        public async Task<Vehicule> GetVehiculeByIdAsync(int id)
        {
            return await _vehiculeRepository.GetByIdAsync(id);
        }

        public async Task<Vehicule> GetVehiculeByImmatriculationAsync(string immatriculation)
        {
            return await _vehiculeRepository.GetByImmatriculationAsync(immatriculation);
        }

        public async Task<IEnumerable<Vehicule>> GetAllVehiculesAsync()
        {
            return await _vehiculeRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Vehicule>> GetAllActiveVehiculesAsync()
        {
            return await _vehiculeRepository.GetAllActiveAsync();
        }

        public async Task<IEnumerable<Vehicule>> GetVehiculesByStatutAsync(string statut)
        {
            if (string.IsNullOrWhiteSpace(statut))
                throw new ArgumentException("Le statut est requis");

            return await _vehiculeRepository.GetByStatutAsync(statut);
        }

        public async Task<IEnumerable<Vehicule>> GetVehiculesByCategorieAsync(int categorieId)
        {
            if (categorieId <= 0)
                throw new ArgumentException("L'ID de catégorie doit être supérieur à 0");

            return await _vehiculeRepository.GetByCategorieAsync(categorieId);
        }

        public async Task<IEnumerable<Vehicule>> GetVehiculesByMarqueAsync(string marque)
        {
            if (string.IsNullOrWhiteSpace(marque))
                throw new ArgumentException("La marque est requise");

            return await _vehiculeRepository.GetByMarqueAsync(marque);
        }

        public async Task<IEnumerable<Vehicule>> GetVehiculesDisponiblesAsync()
        {
            return await _vehiculeRepository.GetByStatutAsync("Disponible");
        }

        public async Task<Vehicule> CreateVehiculeAsync(Vehicule vehicule)
        {
            // Validations
            if (string.IsNullOrWhiteSpace(vehicule.Marque))
                throw new ArgumentException("La marque est requise");

            if (string.IsNullOrWhiteSpace(vehicule.Modele))
                throw new ArgumentException("Le modèle est requis");

            if (string.IsNullOrWhiteSpace(vehicule.Immatriculation))
                throw new ArgumentException("L'immatriculation est requise");

            // AJOUTEZ CETTE VALIDATION :
            if (vehicule.PrixParJour <= 0)
                throw new ArgumentException("Le prix par jour doit être supérieur à 0");

            if (await _vehiculeRepository.ImmatriculationExistsAsync(vehicule.Immatriculation))
                throw new InvalidOperationException($"Un véhicule avec l'immatriculation {vehicule.Immatriculation} existe déjà");

            // Validation de l'année
            if (vehicule.Annee.HasValue)
            {
                if (vehicule.Annee < 1900 || vehicule.Annee > DateTime.Now.Year + 1)
                    throw new ArgumentException($"L'année doit être entre 1900 et {DateTime.Now.Year + 1}");
            }

            // Validation du kilométrage
            if (vehicule.Kilometrage < 0)
                throw new ArgumentException("Le kilométrage ne peut pas être négatif");

            // Validation du nombre de places
            if (vehicule.NombrePlaces < 1 || vehicule.NombrePlaces > 50)
                throw new ArgumentException("Le nombre de places doit être entre 1 et 50");

            // Validation du prix d'achat
            if (vehicule.PrixAchat.HasValue && vehicule.PrixAchat < 0)
                throw new ArgumentException("Le prix d'achat ne peut pas être négatif");

            // Validation de la date d'achat
            if (vehicule.DateAchat.HasValue && vehicule.DateAchat > DateTime.Now)
                throw new ArgumentException("La date d'achat ne peut pas être dans le futur");

            // Initialisation du statut si non défini
            if (string.IsNullOrWhiteSpace(vehicule.Statut))
                vehicule.Statut = "Disponible";

            // Initialiser EstActif si non défini
            vehicule.EstActif = true;

            await _vehiculeRepository.AddAsync(vehicule);
            return vehicule;
        }

        public async Task UpdateVehiculeAsync(Vehicule vehicule)
        {
            if (!await _vehiculeRepository.ExistsAsync(vehicule.Id))
                throw new KeyNotFoundException($"Véhicule avec ID {vehicule.Id} non trouvé");

            // Vérifier si l'immatriculation existe pour un autre véhicule
            var existingVehicule = await _vehiculeRepository.GetByImmatriculationAsync(vehicule.Immatriculation);
            if (existingVehicule != null && existingVehicule.Id != vehicule.Id)
                throw new InvalidOperationException($"Un autre véhicule avec l'immatriculation {vehicule.Immatriculation} existe déjà");

            // AJOUTEZ CETTE VALIDATION :
            if (vehicule.PrixParJour <= 0)
                throw new ArgumentException("Le prix par jour doit être supérieur à 0");

            // Validations similaires à CreateVehiculeAsync
            if (vehicule.Annee.HasValue)
            {
                if (vehicule.Annee < 1900 || vehicule.Annee > DateTime.Now.Year + 1)
                    throw new ArgumentException($"L'année doit être entre 1900 et {DateTime.Now.Year + 1}");
            }

            if (vehicule.Kilometrage < 0)
                throw new ArgumentException("Le kilométrage ne peut pas être négatif");

            if (vehicule.NombrePlaces < 1 || vehicule.NombrePlaces > 50)
                throw new ArgumentException("Le nombre de places doit être entre 1 et 50");

            await _vehiculeRepository.UpdateAsync(vehicule);
        }

        public async Task DeleteVehiculeAsync(int id)
        {
            if (!await _vehiculeRepository.ExistsAsync(id))
                throw new KeyNotFoundException($"Véhicule avec ID {id} non trouvé");

            await _vehiculeRepository.DeleteAsync(id);
        }

        public async Task<bool> VehiculeExistsAsync(int id)
        {
            return await _vehiculeRepository.ExistsAsync(id);
        }

        public async Task UpdateStatutAsync(int id, string statut)
        {
            if (string.IsNullOrWhiteSpace(statut))
                throw new ArgumentException("Le statut est requis");

            var vehicule = await _vehiculeRepository.GetByIdAsync(id);
            if (vehicule == null)
                throw new KeyNotFoundException($"Véhicule avec ID {id} non trouvé");

            // Validation des statuts autorisés
            var statutsAutorises = new[] { "Disponible", "Loué", "En maintenance", "Indisponible" };
            if (!statutsAutorises.Contains(statut))
                throw new ArgumentException($"Statut invalide. Les statuts autorisés sont : {string.Join(", ", statutsAutorises)}");

            vehicule.Statut = statut;
            await _vehiculeRepository.UpdateAsync(vehicule);
        }

        public async Task UpdateKilometrageAsync(int id, int kilometrage)
        {
            if (kilometrage < 0)
                throw new ArgumentException("Le kilométrage ne peut pas être négatif");

            var vehicule = await _vehiculeRepository.GetByIdAsync(id);
            if (vehicule == null)
                throw new KeyNotFoundException($"Véhicule avec ID {id} non trouvé");

            if (kilometrage < vehicule.Kilometrage)
                throw new InvalidOperationException("Le nouveau kilométrage ne peut pas être inférieur à l'ancien");

            vehicule.Kilometrage = kilometrage;
            await _vehiculeRepository.UpdateAsync(vehicule);
        }

        public async Task EnregistrerEntretienAsync(int id, int kilometrage)
        {
            var vehicule = await _vehiculeRepository.GetByIdAsync(id);
            if (vehicule == null)
                throw new KeyNotFoundException($"Véhicule avec ID {id} non trouvé");

            if (kilometrage < 0)
                throw new ArgumentException("Le kilométrage ne peut pas être négatif");

            vehicule.DateDernierEntretien = DateTime.Now;
            vehicule.KilometrageDernierEntretien = kilometrage;

            // Si le kilométrage actuel est inférieur, on le met à jour aussi
            if (kilometrage > vehicule.Kilometrage)
                vehicule.Kilometrage = kilometrage;

            await _vehiculeRepository.UpdateAsync(vehicule);
        }

        public async Task<int> GetTotalVehiculesAsync()
        {
            return await _vehiculeRepository.GetTotalVehiculesAsync();
        }

        public async Task<int> GetVehiculesDisponiblesCountAsync()
        {
            return await _vehiculeRepository.GetVehiculesDisponiblesAsync();
        }

        // Méthode supplémentaire pour mettre à jour le prix par jour
        public async Task UpdatePrixParJourAsync(int id, decimal prixParJour)
        {
            if (prixParJour <= 0)
                throw new ArgumentException("Le prix par jour doit être supérieur à 0");

            var vehicule = await _vehiculeRepository.GetByIdAsync(id);
            if (vehicule == null)
                throw new KeyNotFoundException($"Véhicule avec ID {id} non trouvé");

            vehicule.PrixParJour = prixParJour;
            await _vehiculeRepository.UpdateAsync(vehicule);
        }
    }
}