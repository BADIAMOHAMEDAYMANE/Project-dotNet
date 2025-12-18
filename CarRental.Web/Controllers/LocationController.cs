using Microsoft.AspNetCore.Mvc;
using CarRental.Core.Models;
using CarRental.Core.Interfaces;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CarRental.Web.Controllers
{
    [Authorize]
    public class LocationController : Controller
    {
        private readonly ILocationService _locationService;
        private readonly IClientService _clientService;
        private readonly IVehiculeService _vehiculeService;
        private readonly ILogger<LocationController> _logger;
        private readonly IConfiguration _configuration;

        public LocationController(
            ILocationService locationService,
            IClientService clientService,
            IVehiculeService vehiculeService,
            ILogger<LocationController> logger,
            IConfiguration configuration)
        {
            _locationService = locationService;
            _clientService = clientService;
            _vehiculeService = vehiculeService;
            _logger = logger;
            _configuration = configuration;
        }

        // GET: Location - Accessible par Admin et Employe
        // ✅ CORRECTION: "employee" → "employe" (cohérence avec les autres contrôleurs)
        [Authorize(Roles = "admin,employe")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var locations = await _locationService.GetAllLocationsAsync();
                return View(locations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des locations");
                TempData["Error"] = "Impossible de récupérer la liste des locations.";
                return View(new List<Location>());
            }
        }

        // GET: MesLocations - Pour les clients
        [Authorize(Roles = "client")]
        public async Task<IActionResult> MesLocations()
        {
            try
            {
                var clientCIN = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                               User.FindFirst("CIN")?.Value;

                if (string.IsNullOrEmpty(clientCIN))
                {
                    TempData["Error"] = "Client non identifié. Veuillez vous reconnecter.";
                    return RedirectToAction("Index", "Home");
                }

                var locations = await _locationService.GetLocationsByClientCINAsync(clientCIN);
                return View(locations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des locations du client");
                TempData["Error"] = "Impossible de récupérer vos locations.";
                return View(new List<Location>());
            }
        }

        // GET: Location/Details/5 - Accessible selon le rôle
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || id <= 0)
                return NotFound();

            try
            {
                var location = await _locationService.GetLocationByIdAsync(id.Value);

                // Vérifier les permissions
                if (User.IsInRole("client"))
                {
                    var clientCIN = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                                   User.FindFirst("CIN")?.Value;

                    if (location.ClientCIN != clientCIN)
                    {
                        TempData["Error"] = "Vous n'avez pas accès à cette location.";
                        return RedirectToAction("MesLocations");
                    }
                }

                // Charger les données associées pour la vue
                ViewBag.Client = await _clientService.GetClientByCINAsync(location.ClientCIN);
                ViewBag.Vehicule = await _vehiculeService.GetVehiculeByIdAsync(location.VehiculeId);

                return View(location);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Location {Id} non trouvée", id);
                TempData["Error"] = "Location non trouvée.";
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la location {Id}", id);
                TempData["Error"] = "Erreur lors de la récupération des détails.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Location/Create - Accessible par Admin et Employe
        // ✅ CORRECTION
        [Authorize(Roles = "admin,employe")]
        public async Task<IActionResult> Create()
        {
            try
            {
                var clients = await _clientService.GetAllClientsAsync();
                var vehicules = await _vehiculeService.GetAllVehiculesAsync();

                ViewBag.Clients = clients
                    .Select(c => new
                    {
                        CIN = c.CIN,
                        DisplayText = $"{c.Nom} {c.Prenom} - {c.CIN}"
                    })
                    .ToList();

                ViewBag.Vehicules = vehicules
                    .Where(v => v.Statut == "Disponible" && v.EstActif)
                    .Select(v => new
                    {
                        Id = v.Id,
                        DisplayText = $"{v.Marque} {v.Modele} - {v.Immatriculation} ({v.PrixParJour:C}/jour)"
                    })
                    .ToList();

                var model = new Location
                {
                    ClientCIN = string.Empty,
                    DateDebut = DateTime.Today,
                    DateFin = DateTime.Today.AddDays(1),
                    Statut = "En attente"
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des données pour création");
                TempData["Error"] = "Impossible de charger les données nécessaires.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Location/CreerLocation - Pour les clients
        [Authorize(Roles = "client")]
        public async Task<IActionResult> CreerLocation()
        {
            try
            {
                var clientCIN = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User.FindFirst("CIN")?.Value;

                if (string.IsNullOrEmpty(clientCIN))
                {
                    TempData["Error"] = "Client non identifié.";
                    return RedirectToAction("Index", "Home");
                }

                var vehicules = await _vehiculeService.GetAllVehiculesAsync();
                var client = await _clientService.GetClientByCINAsync(clientCIN);

                ViewBag.Vehicules = vehicules
                    .Where(v => v.Statut == "Disponible" && v.EstActif)
                    .ToList();

                ViewBag.CurrentUserCIN = clientCIN;
                ViewBag.Client = client;

                return View(new Location
                {
                    ClientCIN = clientCIN,
                    DateDebut = DateTime.Today,
                    DateFin = DateTime.Today.AddDays(1),
                    Statut = "En attente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement de la création client");
                TempData["Error"] = "Erreur lors du chargement.";
                return RedirectToAction("MesLocations");
            }
        }

        // POST: Location/Create
        // ✅ CORRECTION
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,employe")]
        public async Task<IActionResult> Create(Location location)
        {
            try
            {
                _logger.LogInformation("=== DEBUT CREATION LOCATION PAR ADMIN/EMPLOYE ===");

                if (string.IsNullOrWhiteSpace(location.ClientCIN))
                {
                    ModelState.AddModelError("ClientCIN", "Le CIN du client est requis.");
                }

                ModelState.Remove("Client");
                ModelState.Remove("Vehicule");
                ModelState.Remove("PrixTotal");

                if (!ModelState.IsValid)
                {
                    await LoadViewDataForCreate();
                    return View(location);
                }

                location.Statut = "En attente";
                var createdLocation = await _locationService.CreateLocationAsync(location);

                var client = await _clientService.GetClientByCINAsync(location.ClientCIN);
                var vehicule = await _vehiculeService.GetVehiculeByIdAsync(location.VehiculeId);

                if (client != null && vehicule != null && !string.IsNullOrWhiteSpace(client.Email))
                {
                    bool emailSent = await SendLocationConfirmationEmail(
                        client.Email, client.Nom, client.Prenom,
                        vehicule.Marque, vehicule.Modele,
                        location.DateDebut, location.DateFin, vehicule.PrixParJour);

                    TempData["Success"] = emailSent
                        ? $"Location créée avec succès. Email envoyé à {client.Email}."
                        : "Location créée, mais l'envoi de l'email a échoué.";
                }
                else
                {
                    TempData["Success"] = "Location créée avec succès.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création");
                TempData["Error"] = ex.Message;
                await LoadViewDataForCreate();
                return View(location);
            }
        }

        // POST: Location/CreerLocation - Pour les clients
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "client")]
        public async Task<IActionResult> CreerLocation(Location location)
        {
            try
            {
                var clientCIN = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                               User.FindFirst("CIN")?.Value;

                if (string.IsNullOrEmpty(clientCIN))
                {
                    TempData["Error"] = "Client non identifié.";
                    return RedirectToAction("Index", "Home");
                }

                location.ClientCIN = clientCIN;
                location.Statut = "En attente";

                ModelState.Remove("Client");
                ModelState.Remove("Vehicule");
                ModelState.Remove("PrixTotal");
                ModelState.Remove("ClientCIN");

                if (!ModelState.IsValid)
                {
                    await LoadViewDataForClientCreate(clientCIN);
                    return View(location);
                }

                var createdLocation = await _locationService.CreateLocationAsync(location);

                var client = await _clientService.GetClientByCINAsync(clientCIN);
                var vehicule = await _vehiculeService.GetVehiculeByIdAsync(location.VehiculeId);

                if (client != null && vehicule != null && !string.IsNullOrWhiteSpace(client.Email))
                {
                    await SendLocationConfirmationEmail(
                        client.Email, client.Nom, client.Prenom,
                        vehicule.Marque, vehicule.Modele,
                        location.DateDebut, location.DateFin, vehicule.PrixParJour);
                }

                TempData["Success"] = "Réservation créée avec succès.";
                return RedirectToAction("MesLocations");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création par client");
                TempData["Error"] = ex.Message;
                await LoadViewDataForClientCreate(User.FindFirst("CIN")?.Value);
                return View(location);
            }
        }

        // GET: Location/Edit/5
        // ✅ CORRECTION
        [Authorize(Roles = "admin,employe")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id <= 0) return NotFound();

            try
            {
                var location = await _locationService.GetLocationByIdAsync(id.Value);
                var clients = await _clientService.GetAllClientsAsync();
                var vehicules = await _vehiculeService.GetAllVehiculesAsync();

                ViewBag.Clients = clients.Select(c => new
                {
                    CIN = c.CIN,
                    DisplayText = $"{c.Nom} {c.Prenom} - {c.CIN}"
                }).ToList();

                ViewBag.Vehicules = vehicules.Where(v => v.EstActif).Select(v => new
                {
                    Id = v.Id,
                    DisplayText = $"{v.Marque} {v.Modele} - {v.Immatriculation}"
                }).ToList();

                return View(location);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération");
                return NotFound();
            }
        }

        // POST: Location/Edit/5
        // ✅ CORRECTION
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,employe")]
        public async Task<IActionResult> Edit(int id, Location location)
        {
            if (id != location.Id) return NotFound();

            try
            {
                ModelState.Remove("Client");
                ModelState.Remove("Vehicule");
                ModelState.Remove("PrixTotal");

                if (!ModelState.IsValid)
                {
                    await LoadViewDataForEdit();
                    return View(location);
                }

                await _locationService.UpdateLocationAsync(location);
                TempData["Success"] = "Location modifiée avec succès.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification");
                TempData["Error"] = ex.Message;
                await LoadViewDataForEdit();
                return View(location);
            }
        }

        // GET: Location/Delete/5
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || id <= 0) return NotFound();

            try
            {
                var location = await _locationService.GetLocationByIdAsync(id.Value);
                ViewBag.Client = await _clientService.GetClientByCINAsync(location.ClientCIN);
                ViewBag.Vehicule = await _vehiculeService.GetVehiculeByIdAsync(location.VehiculeId);
                return View(location);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur");
                return NotFound();
            }
        }

        // POST: Location/Delete/5
        // ✅ CORRECTION: "Admin" → "admin"
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _locationService.DeleteLocationAsync(id);
                TempData["Success"] = "Location supprimée avec succès.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression");
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // ========== ACTIONS POUR LA GESTION DES STATUTS ==========

        // ✅ CORRECTION: "employee" → "employe"
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,employe")]
        public async Task<IActionResult> Confirm(int id)
        {
            try
            {
                await _locationService.ConfirmLocationAsync(id);
                TempData["Success"] = "Location confirmée avec succès.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur");
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        // ✅ CORRECTION
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,employe")]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                await _locationService.CancelLocationAsync(id);
                TempData["Success"] = "Location annulée avec succès.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur");
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        // ✅ CORRECTION
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,employe")]
        public async Task<IActionResult> Start(int id)
        {
            try
            {
                await _locationService.StartLocationAsync(id);
                TempData["Success"] = "Location démarrée avec succès.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur");
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        // ✅ CORRECTION
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,employe")]
        public async Task<IActionResult> Complete(int id)
        {
            try
            {
                await _locationService.CompleteLocationAsync(id);
                var montant = await _locationService.CalculateLocationCostAsync(id);
                TempData["Success"] = $"Location terminée. Montant : {montant:C}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur");
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        // ========== MÉTHODES PRIVÉES ==========

        private async Task LoadViewDataForCreate()
        {
            var clients = await _clientService.GetAllClientsAsync();
            var vehicules = await _vehiculeService.GetAllVehiculesAsync();

            ViewBag.Clients = clients.Select(c => new
            {
                CIN = c.CIN,
                DisplayText = $"{c.Nom} {c.Prenom} - {c.CIN}"
            }).ToList();

            ViewBag.Vehicules = vehicules
                .Where(v => v.Statut == "Disponible" && v.EstActif)
                .Select(v => new
                {
                    Id = v.Id,
                    DisplayText = $"{v.Marque} {v.Modele} - {v.Immatriculation} ({v.PrixParJour:C}/jour)"
                }).ToList();
        }

        private async Task LoadViewDataForClientCreate(string clientCIN)
        {
            var vehicules = await _vehiculeService.GetAllVehiculesAsync();
            var client = await _clientService.GetClientByCINAsync(clientCIN);

            ViewBag.Vehicules = vehicules
                .Where(v => v.Statut == "Disponible" && v.EstActif)
                .ToList();

            ViewBag.CurrentUserCIN = clientCIN;
            ViewBag.Client = client;
        }

        private async Task LoadViewDataForEdit()
        {
            var clients = await _clientService.GetAllClientsAsync();
            var vehicules = await _vehiculeService.GetAllVehiculesAsync();

            ViewBag.Clients = clients.Select(c => new
            {
                CIN = c.CIN,
                DisplayText = $"{c.Nom} {c.Prenom} - {c.CIN}"
            }).ToList();

            ViewBag.Vehicules = vehicules.Where(v => v.EstActif).Select(v => new
            {
                Id = v.Id,
                DisplayText = $"{v.Marque} {v.Modele} - {v.Immatriculation}"
            }).ToList();
        }

        // ========== MÉTHODES D'ENVOI D'EMAILS (simplifiées) ==========

        private async Task<bool> SendLocationConfirmationEmail(
            string toEmail, string nom, string prenom,
            string marque, string modele,
            DateTime dateDebut, DateTime dateFin, decimal prixParJour)
        {
            try
            {
                var nombreJours = Math.Max((dateFin - dateDebut).Days, 1);
                var montantTotal = prixParJour * nombreJours;

                var subject = "Confirmation de votre location - CarRental";
                var body = $@"Bonjour {prenom} {nom},

Confirmation de votre réservation :
- Véhicule : {marque} {modele}
- Du {dateDebut:dd/MM/yyyy} au {dateFin:dd/MM/yyyy}
- Durée : {nombreJours} jour(s)
- Montant estimé : {montantTotal:C}

Cordialement,
L'équipe CarRental";

                return await SendEmail(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur envoi email");
                return false;
            }
        }

        private async Task<bool> SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:Password"];

                if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
                {
                    _logger.LogWarning("Configuration email manquante");
                    return false;
                }

                using var smtpClient = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, "CarRental"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };

                mailMessage.To.Add(toEmail);
                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur envoi email");
                return false;
            }
        }
    }
}