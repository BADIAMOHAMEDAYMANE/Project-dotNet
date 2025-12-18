using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CarRental.Core.Interfaces;
using CarRental.Core.Models;
using CarRental.Data;

namespace CarRental.Data.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly ApplicationDbContext _context;

        public AdminRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Statistiques
        public async Task<int> GetTotalEmployeesAsync()
        {
            // Filtrer les employés avec le rôle "Employe" (insensible à la casse)
            return await _context.Employees
                .CountAsync(e => e.Role.ToLower() == "employe" ||
                                e.Role.ToLower() == "employé" ||
                                e.Role.ToLower() == "employee");
        }

        public async Task<int> GetTotalClientsAsync()
        {
            return await _context.Clients.CountAsync();
        }

        public async Task<int> GetTotalVehiclesAsync()
        {
            return await _context.Vehicules.CountAsync();
        }

        public async Task<int> GetPendingRequestsAsync()
        {
            return await _context.Locations
                .CountAsync(l => l.Statut == "EnAttente" ||
                                l.Statut == "En attente" ||
                                l.Statut == "Pending");
        }

        public async Task<int> GetVehiclesInMaintenanceAsync()
        {
            return await _context.Vehicules
                .CountAsync(v => v.Statut == "Maintenance" || v.Statut == "En maintenance");
        }

        public async Task<int> GetReturnsTodayAsync()
        {
            var today = DateTime.Today;
            return await _context.Locations
                .CountAsync(l => l.DateFin.Date == today &&
                                (l.Statut == "En cours" ||
                                 l.Statut == "Active" ||
                                 l.Statut == "Confirmée"));
        }

        // Récupération des listes
        public async Task<IEnumerable<Employee>> GetAllEmployeesAsync()
        {
            // Retourner UNIQUEMENT les employés avec le rôle "Employe" (pas Admin ni basic)
            return await _context.Employees
                .Where(e => e.Role.ToLower() == "employe" ||
                            e.Role.ToLower() == "employé" ||
                            e.Role.ToLower() == "employee")
                .OrderBy(e => e.Nom)
                .ThenBy(e => e.Prenom)
                .ToListAsync();
        }

        public async Task<IEnumerable<Client>> GetAllClientsAsync()
        {
            // Charger les clients
            var clients = await _context.Clients
                .OrderBy(c => c.Nom)
                .ThenBy(c => c.Prenom)
                .ToListAsync();

            return clients;
        }

        public async Task<IEnumerable<Vehicule>> GetAllVehiclesAsync()
        {
            return await _context.Vehicules
                .Include(v => v.Categorie)
                .OrderBy(v => v.Marque)
                .ThenBy(v => v.Modele)
                .ToListAsync();
        }

        public async Task<IEnumerable<Location>> GetPendingLocationsAsync()
        {
            return await _context.Locations
                .Include(l => l.Client)
                .Include(l => l.Vehicule)
                    .ThenInclude(v => v.Categorie)
                .Where(l => l.Statut == "EnAttente" ||
                            l.Statut == "En attente" ||
                            l.Statut == "Pending")
                .OrderByDescending(l => l.DateDebut)
                .ToListAsync();
        }

        // Gestion des demandes
        public async Task<Location?> GetLocationByIdAsync(int locationId)
        {
            return await _context.Locations
                .Include(l => l.Client)
                .Include(l => l.Vehicule)
                    .ThenInclude(v => v.Categorie)
                .FirstOrDefaultAsync(l => l.Id == locationId);
        }

        public async Task<bool> ApproveLocationAsync(int locationId)
        {
            var location = await _context.Locations
                .Include(l => l.Vehicule)
                .FirstOrDefaultAsync(l => l.Id == locationId);

            if (location == null)
                return false;

            // Vérifier que la location est bien en attente
            if (location.Statut != "EnAttente" &&
                location.Statut != "En attente" &&
                location.Statut != "Pending")
            {
                return false;
            }

            // Mettre à jour le statut
            location.Statut = "Confirmée";

            // Marquer le véhicule comme loué
            if (location.Vehicule != null)
            {
                location.Vehicule.Statut = "Loué";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectLocationAsync(int locationId)
        {
            var location = await _context.Locations
                .Include(l => l.Vehicule)
                .FirstOrDefaultAsync(l => l.Id == locationId);

            if (location == null)
                return false;

            // Vérifier que la location est bien en attente
            if (location.Statut != "EnAttente" &&
                location.Statut != "En attente" &&
                location.Statut != "Pending")
            {
                return false;
            }

            // Mettre à jour le statut
            location.Statut = "Rejetée";

            // Remettre le véhicule disponible
            if (location.Vehicule != null)
            {
                location.Vehicule.Statut = "Disponible";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // Activités récentes
        public async Task<IEnumerable<ActivityLog>> GetRecentActivitiesAsync(int count = 10)
        {
            var activities = new List<ActivityLog>();

            // Dernières locations
            var recentLocations = await _context.Locations
                .Include(l => l.Client)
                .Include(l => l.Vehicule)
                .OrderByDescending(l => l.DateDebut)
                .Take(5)
                .ToListAsync();

            foreach (var location in recentLocations)
            {
                var timeDiff = DateTime.Now - location.DateDebut;
                string timeAgo = GetTimeAgo(timeDiff);

                string action = location.Statut switch
                {
                    "Confirmée" => "Location approuvée",
                    "En cours" => "Location en cours",
                    "Terminée" => "Location terminée",
                    "Rejetée" => "Location rejetée",
                    _ => "Nouvelle demande de location"
                };

                string icon = location.Statut switch
                {
                    "Confirmée" => "fa-check-circle",
                    "En cours" => "fa-car",
                    "Terminée" => "fa-flag-checkered",
                    "Rejetée" => "fa-times-circle",
                    _ => "fa-clock"
                };

                string type = location.Statut switch
                {
                    "Confirmée" => "success",
                    "En cours" => "info",
                    "Terminée" => "success",
                    "Rejetée" => "warning",
                    _ => "info"
                };

                string clientName = location.Client != null
                    ? $"{location.Client.Prenom} {location.Client.Nom}"
                    : "Client";

                string vehicleName = location.Vehicule != null
                    ? $"{location.Vehicule.Marque} {location.Vehicule.Modele}"
                    : "Véhicule";

                activities.Add(new ActivityLog
                {
                    Id = location.Id,
                    Action = action,
                    Description = $"{clientName} - {vehicleName} - {timeAgo}",
                    Date = location.DateDebut,
                    Icon = icon,
                    Type = type
                });
            }

            // Derniers clients ajoutés
            var recentClients = await _context.Clients
                .OrderByDescending(c => c.CIN)
                .Take(3)
                .ToListAsync();

            foreach (var client in recentClients)
            {
                activities.Add(new ActivityLog
                {
                    Id = 0,
                    Action = "Nouveau client enregistré",
                    Description = $"{client.Prenom} {client.Nom}",
                    Date = DateTime.Now.AddHours(-2), // Simulé - ajustez selon votre base
                    Icon = "fa-user-plus",
                    Type = "success"
                });
            }

            // Derniers véhicules ajoutés
            var recentVehicles = await _context.Vehicules
                .OrderByDescending(v => v.Id)
                .Take(2)
                .ToListAsync();

            foreach (var vehicle in recentVehicles)
            {
                activities.Add(new ActivityLog
                {
                    Id = 0,
                    Action = "Véhicule ajouté",
                    Description = $"{vehicle.Marque} {vehicle.Modele} - {vehicle.Immatriculation}",
                    Date = DateTime.Now.AddHours(-5), // Simulé
                    Icon = "fa-car",
                    Type = "info"
                });
            }

            return activities
                .OrderByDescending(a => a.Date)
                .Take(count);
        }

        /// <summary>
        /// Calcule le temps écoulé depuis une date
        /// </summary>
        private string GetTimeAgo(TimeSpan timeDiff)
        {
            if (timeDiff.TotalSeconds < 60)
                return "À l'instant";

            if (timeDiff.TotalMinutes < 60)
                return $"Il y a {(int)timeDiff.TotalMinutes} min";

            if (timeDiff.TotalHours < 24)
                return $"Il y a {(int)timeDiff.TotalHours}h";

            if (timeDiff.TotalDays < 2)
                return "Hier";

            if (timeDiff.TotalDays < 7)
                return $"Il y a {(int)timeDiff.TotalDays} jours";

            return DateTime.Now.Subtract(timeDiff).ToString("dd/MM/yyyy");
        }
    }
}