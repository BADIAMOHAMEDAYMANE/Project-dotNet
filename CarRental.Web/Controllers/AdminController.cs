using System;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CarRental.Core.Interfaces;
using CarRental.Core.Models;
using IOFile = System.IO.File; // ✅ ALIAS pour éviter le conflit avec Controller.File()

namespace CarRental.Web.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IFactureService? _factureService;
        private readonly ILocationService? _locationService;
        private readonly IClientService? _clientService;
        private readonly IVehiculeService? _vehiculeService;
        private readonly IConfiguration? _configuration;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IAdminService adminService,
            ILogger<AdminController> logger,
            IFactureService? factureService = null,
            ILocationService? locationService = null,
            IClientService? clientService = null,
            IVehiculeService? vehiculeService = null,
            IConfiguration? configuration = null)
        {
            _adminService = adminService;
            _logger = logger;
            _factureService = factureService;
            _locationService = locationService;
            _clientService = clientService;
            _vehiculeService = vehiculeService;
            _configuration = configuration;
        }

        // GET: /Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var statistics = await _adminService.GetDashboardStatisticsAsync();

                // Passer les statistiques au ViewBag
                ViewBag.TotalEmployees = statistics.TotalEmployees;
                ViewBag.TotalClients = statistics.TotalClients;
                ViewBag.TotalVehicles = statistics.TotalVehicles;
                ViewBag.PendingRequests = statistics.PendingRequests;
                ViewBag.VehiclesInMaintenance = statistics.VehiclesInMaintenance;
                ViewBag.ReturnsToday = statistics.ReturnsToday;

                ViewBag.Employees = statistics.Employees;
                ViewBag.Clients = statistics.Clients;
                ViewBag.Vehicles = statistics.Vehicles;
                ViewBag.PendingLocations = statistics.PendingLocations;

                _logger.LogInformation("Dashboard chargé avec succès pour {User}", User.Identity?.Name);

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement du dashboard");
                TempData["Error"] = "Erreur lors du chargement du dashboard";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /Admin/Profile - Affichage du profil de l'admin connecté
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            _logger.LogInformation("=== GET /Admin/Profile ===");
            _logger.LogInformation("User: {Name}", User.Identity?.Name);

            try
            {
                // Récupérer l'ID de l'employé connecté depuis les claims
                var employeeIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                                     User.FindFirstValue("EmployeeId");

                if (string.IsNullOrEmpty(employeeIdClaim))
                {
                    _logger.LogWarning("❌ ID employé non trouvé dans les claims");
                    TempData["Error"] = "Informations de profil introuvables.";
                    return RedirectToAction("Dashboard");
                }

                _logger.LogInformation("🔍 Recherche de l'employé avec ID: {EmployeeId}", employeeIdClaim);

                // Convertir l'ID en int
                if (!int.TryParse(employeeIdClaim, out int employeeId))
                {
                    _logger.LogWarning("❌ ID employé invalide: {EmployeeId}", employeeIdClaim);
                    TempData["Error"] = "Identifiant invalide.";
                    return RedirectToAction("Dashboard");
                }

                // Récupérer les informations de l'employé via le service
                var employee = await _adminService.GetEmployeeByIdAsync(employeeId);

                if (employee == null)
                {
                    _logger.LogWarning("❌ Employé non trouvé avec ID: {EmployeeId}", employeeId);
                    TempData["Error"] = "Profil non trouvé.";
                    return RedirectToAction("Dashboard");
                }

                _logger.LogInformation("✅ Affichage du profil pour: {Nom} {Prenom}", employee.Nom, employee.Prenom);
                return View(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la récupération du profil admin");
                TempData["Error"] = "Une erreur est survenue lors du chargement de votre profil.";
                return RedirectToAction("Dashboard");
            }
        }

        // GET: /Admin/CreateEmployee
        public IActionResult CreateEmployee()
        {
            return View();
        }

        // POST: /Admin/CreateEmployee
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmployee(Employee employee, string password, string confirmPassword)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Validation du mot de passe
                    if (string.IsNullOrEmpty(password))
                    {
                        ModelState.AddModelError("password", "Le mot de passe est requis.");
                        return View(employee);
                    }

                    if (password != confirmPassword)
                    {
                        ModelState.AddModelError("confirmPassword", "Les mots de passe ne correspondent pas.");
                        return View(employee);
                    }

                    if (password.Length < 6)
                    {
                        ModelState.AddModelError("password", "Le mot de passe doit contenir au moins 6 caractères.");
                        return View(employee);
                    }

                    var result = await _adminService.CreateEmployeeAsync(employee, password);
                    if (result)
                    {
                        TempData["Success"] = "Employé créé avec succès";
                        _logger.LogInformation("Employé créé par {User}", User.Identity?.Name);
                        return RedirectToAction("Dashboard");
                    }
                    else
                    {
                        TempData["Error"] = "Erreur lors de la création de l'employé (email peut-être déjà utilisé)";
                    }
                }

                return View(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de l'employé");
                TempData["Error"] = "Erreur lors de la création de l'employé";
                return View(employee);
            }
        }

        // GET: /Admin/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var employee = await _adminService.GetEmployeeByIdAsync(id);
                if (employee == null)
                {
                    TempData["Error"] = "Employé non trouvé";
                    return RedirectToAction("Dashboard");
                }

                return View("~/Views/Employee/Edit.cshtml", employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement de l'employé #{Id}", id);
                TempData["Error"] = "Erreur lors du chargement de l'employé";
                return RedirectToAction("Dashboard");
            }
        }

        // POST: /Admin/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Employee employee)
        {
            try
            {
                if (id != employee.ID)
                {
                    TempData["Error"] = "ID d'employé invalide";
                    return RedirectToAction("Dashboard");
                }

                if (ModelState.IsValid)
                {
                    var result = await _adminService.UpdateEmployeeAsync(employee);
                    if (result)
                    {
                        TempData["Success"] = "Employé modifié avec succès";
                        _logger.LogInformation("Employé #{EmployeeId} modifié par {User}",
                            employee.ID, User.Identity?.Name);
                        return RedirectToAction("Dashboard");
                    }
                }

                TempData["Error"] = "Erreur lors de la modification de l'employé";
                return View("~/Views/Employee/Edit.cshtml", employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification de l'employé #{Id}", id);
                TempData["Error"] = "Erreur lors de la modification de l'employé";
                return View("~/Views/Employee/Edit.cshtml", employee);
            }
        }

        // GET: /Admin/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var employee = await _adminService.GetEmployeeByIdAsync(id);
                if (employee == null)
                {
                    TempData["Error"] = "Employé non trouvé";
                    return RedirectToAction("Dashboard");
                }

                return View("~/Views/Employee/Edit.cshtml", employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement de l'employé #{Id}", id);
                TempData["Error"] = "Erreur lors du chargement de l'employé";
                return RedirectToAction("Dashboard");
            }
        }

        // POST: /Admin/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _adminService.DeleteEmployeeAsync(id);
                if (result)
                {
                    TempData["Success"] = "Employé supprimé avec succès";
                    _logger.LogInformation("Employé #{EmployeeId} supprimé par {User}",
                        id, User.Identity?.Name);
                }
                else
                {
                    TempData["Error"] = "Employé non trouvé";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'employé #{Id}", id);
                TempData["Error"] = "Erreur lors de la suppression de l'employé";
            }

            return RedirectToAction("Dashboard");
        }

        // POST: /Admin/Approve/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                _logger.LogInformation("=== DEBUT APPROBATION Location #{LocationId} ===", id);

                // 1. Vérifier les services disponibles
                _logger.LogInformation("📋 Vérification des services:");
                _logger.LogInformation("  - LocationService: {Status}", _locationService != null ? "✅ Disponible" : "❌ NULL");
                _logger.LogInformation("  - FactureService: {Status}", _factureService != null ? "✅ Disponible" : "❌ NULL");
                _logger.LogInformation("  - ClientService: {Status}", _clientService != null ? "✅ Disponible" : "❌ NULL");
                _logger.LogInformation("  - VehiculeService: {Status}", _vehiculeService != null ? "✅ Disponible" : "❌ NULL");
                _logger.LogInformation("  - Configuration: {Status}", _configuration != null ? "✅ Disponible" : "❌ NULL");

                // 2. Récupérer la location
                Location? location = null;
                if (_locationService != null)
                {
                    location = await _locationService.GetLocationByIdAsync(id);
                    if (location != null)
                    {
                        _logger.LogInformation("✅ Location trouvée: ID={Id}, Client={ClientCIN}, Vehicule={VehiculeId}",
                            location.Id, location.ClientCIN, location.VehiculeId);
                    }
                    else
                    {
                        _logger.LogWarning("❌ Location #{LocationId} introuvable", id);
                    }
                }
                else
                {
                    _logger.LogError("❌ LocationService est NULL - impossible de récupérer la location");
                }

                if (location == null)
                {
                    TempData["Error"] = "Location introuvable";
                    return RedirectToAction("Dashboard");
                }

                // 3. Approuver la location
                _logger.LogInformation("🔄 Approbation de la location #{LocationId}...", id);
                var approvalResult = await _adminService.ApproveLocationAsync(id);

                if (!approvalResult)
                {
                    _logger.LogError("❌ Échec de l'approbation de la location #{LocationId}", id);
                    TempData["Error"] = "Erreur lors de l'approbation";
                    return RedirectToAction("Dashboard");
                }

                _logger.LogInformation("✅ Location #{LocationId} approuvée avec succès", id);

                // 4. Génération de la facture
                if (_factureService != null && _locationService != null)
                {
                    try
                    {
                        _logger.LogInformation("📄 Début de la génération de la facture...");

                        // Vérifier si une facture existe déjà
                        var factureExiste = await _factureService.FactureExistePourLocationAsync(id);
                        _logger.LogInformation("🔍 Facture existante: {Existe}", factureExiste ? "OUI ⚠️" : "NON ✅");

                        if (!factureExiste)
                        {
                            // Recharger la location avec statut mis à jour
                            _logger.LogInformation("🔄 Rechargement de la location avec statut mis à jour...");
                            location = await _locationService.GetLocationByIdAsync(id);

                            if (location == null)
                            {
                                _logger.LogError("❌ Impossible de recharger la location #{LocationId}", id);
                                TempData["Warning"] = "✅ Demande approuvée, mais erreur lors du rechargement de la location";
                                return RedirectToAction("Dashboard");
                            }

                            _logger.LogInformation("📝 Appel de CreateFactureAsync pour Location #{LocationId}...", id);
                            _logger.LogInformation("   - Format: PDF");
                            _logger.LogInformation("   - ClientCIN: {CIN}", location.ClientCIN);
                            _logger.LogInformation("   - VehiculeId: {VehiculeId}", location.VehiculeId);
                            _logger.LogInformation("   - DateDebut: {DateDebut}", location.DateDebut);
                            _logger.LogInformation("   - DateFin: {DateFin}", location.DateFin);

                            var facture = await _factureService.CreateFactureAsync(location, "PDF");

                            if (facture != null)
                            {
                                _logger.LogInformation("✅ Facture créée avec succès:");
                                _logger.LogInformation("   - ID: {FactureId}", facture.Id);
                                _logger.LogInformation("   - Montant Total: {Montant} MAD", facture.MontantTotal);
                                _logger.LogInformation("   - Chemin Fichier: {CheminFichier}", facture.CheminFichier);
                                _logger.LogInformation("   - Fichier existe: {Existe}",
                                    !string.IsNullOrEmpty(facture.CheminFichier) && IOFile.Exists(facture.CheminFichier) ? "✅ OUI" : "❌ NON");

                                // 5. Envoyer l'email avec la facture
                                if (_clientService != null && _vehiculeService != null && _configuration != null)
                                {
                                    _logger.LogInformation("📧 Préparation de l'envoi de l'email...");

                                    var client = await _clientService.GetClientByCINAsync(location.ClientCIN);
                                    var vehicule = await _vehiculeService.GetVehiculeByIdAsync(location.VehiculeId);

                                    if (client != null)
                                    {
                                        _logger.LogInformation("✅ Client trouvé: {Nom} {Prenom} - Email: {Email}",
                                            client.Nom, client.Prenom, client.Email ?? "PAS D'EMAIL");
                                    }
                                    else
                                    {
                                        _logger.LogWarning("❌ Client non trouvé avec CIN: {CIN}", location.ClientCIN);
                                    }

                                    if (vehicule != null)
                                    {
                                        _logger.LogInformation("✅ Véhicule trouvé: {Marque} {Modele}",
                                            vehicule.Marque, vehicule.Modele);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("❌ Véhicule non trouvé avec ID: {VehiculeId}", location.VehiculeId);
                                    }

                                    if (client != null && vehicule != null && !string.IsNullOrWhiteSpace(client.Email))
                                    {
                                        _logger.LogInformation("📤 Envoi de l'email à {Email}...", client.Email);

                                        bool emailSent = await EnvoyerEmailAvecFacture(
                                            client.Email,
                                            client.Nom,
                                            client.Prenom,
                                            vehicule.Marque,
                                            vehicule.Modele,
                                            location.DateDebut,
                                            location.DateFin,
                                            facture.CheminFichier,
                                            facture.MontantTotal
                                        );

                                        if (emailSent)
                                        {
                                            TempData["Success"] = $"✅ Demande approuvée ! Facture générée et envoyée à {client.Email}";
                                            _logger.LogInformation("✅ Email avec facture envoyé avec succès à {Email}", client.Email);
                                        }
                                        else
                                        {
                                            _logger.LogWarning("⚠️ L'envoi de l'email a échoué");
                                            TempData["Warning"] = "✅ Demande approuvée et facture générée, mais l'envoi de l'email a échoué.";
                                        }
                                    }
                                    else
                                    {
                                        _logger.LogWarning("⚠️ Impossible d'envoyer l'email - Données manquantes");
                                        TempData["Success"] = "✅ Demande approuvée avec succès ! Facture générée.";
                                    }
                                }
                                else
                                {
                                    _logger.LogWarning("⚠️ Services manquants pour l'envoi d'email");
                                    TempData["Success"] = "✅ Demande approuvée avec succès ! Facture générée.";
                                }
                            }
                            else
                            {
                                _logger.LogError("❌ CreateFactureAsync a retourné NULL");
                                TempData["Warning"] = "✅ Demande approuvée, mais la facture n'a pas pu être générée.";
                            }
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ Une facture existe déjà pour Location #{LocationId}", id);
                            TempData["Success"] = "✅ Demande approuvée avec succès !";
                        }
                    }
                    catch (Exception factureEx)
                    {
                        _logger.LogError(factureEx, "❌ EXCEPTION lors de la génération de la facture:");
                        _logger.LogError("   - Message: {Message}", factureEx.Message);
                        _logger.LogError("   - StackTrace: {StackTrace}", factureEx.StackTrace);
                        if (factureEx.InnerException != null)
                        {
                            _logger.LogError("   - InnerException: {InnerMessage}", factureEx.InnerException.Message);
                        }
                        TempData["Warning"] = "✅ Demande approuvée, mais une erreur est survenue lors de la génération de la facture.";
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ FactureService ou LocationService est NULL - Pas de génération de facture");
                    TempData["Success"] = "✅ Demande approuvée avec succès !";
                }

                _logger.LogInformation("=== FIN APPROBATION Location #{LocationId} ===", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERREUR GLOBALE lors de l'approbation de la location #{Id}", id);
                _logger.LogError("   - Message: {Message}", ex.Message);
                _logger.LogError("   - StackTrace: {StackTrace}", ex.StackTrace);
                TempData["Error"] = "❌ Erreur lors de l'approbation : " + ex.Message;
            }

            return RedirectToAction("Dashboard");
        }

        // POST: /Admin/Reject/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            try
            {
                var result = await _adminService.RejectLocationAsync(id);

                if (result)
                {
                    TempData["Success"] = "Demande rejetée";
                    _logger.LogInformation("Location #{LocationId} rejetée par {User}",
                        id, User.Identity?.Name);
                }
                else
                {
                    TempData["Error"] = "Location introuvable";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du rejet de la location #{Id}", id);
                TempData["Error"] = "Erreur lors du rejet de la demande";
            }

            return RedirectToAction("Dashboard");
        }

        // GET: /Admin/Employees
        public async Task<IActionResult> Employees()
        {
            try
            {
                var employees = await _adminService.GetAllEmployeesAsync();
                return View(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des employés");
                TempData["Error"] = "Erreur lors du chargement des employés";
                return RedirectToAction("Dashboard");
            }
        }

        // ========== MÉTHODES PRIVÉES POUR L'ENVOI D'EMAILS ==========

        private async Task<bool> EnvoyerEmailAvecFacture(
            string toEmail,
            string nom,
            string prenom,
            string marque,
            string modele,
            DateTime dateDebut,
            DateTime dateFin,
            string pdfFilePath,
            decimal montantTotal)
        {
            try
            {
                var subject = "✅ Location Approuvée - Votre Facture - CarRental";
                var body = $@"Bonjour {prenom} {nom},

🎉 Excellente nouvelle ! Votre demande de location a été APPROUVÉE.

📋 DÉTAILS DE VOTRE LOCATION :
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• Véhicule : {marque} {modele}
• Date de début : {dateDebut:dddd dd MMMM yyyy}
• Date de fin : {dateFin:dddd dd MMMM yyyy}
• Montant Total TTC : {montantTotal * 1.20m:N2} MAD

📄 FACTURE :
Vous trouverez votre facture en pièce jointe de cet email.

📍 PROCHAINES ÉTAPES :
1. Vérifiez les détails de votre réservation
2. Présentez-vous à notre agence le jour du départ avec :
   - Votre CIN
   - Votre permis de conduire
   - Un moyen de paiement

📞 BESOIN D'AIDE ?
Notre équipe est à votre disposition :
• Tél : +212 5XX-XXXXXX
• Email : contact@carrental.ma

Merci d'avoir choisi CarRental pour votre location !

Cordialement,
L'équipe CarRental
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";

                return await SendEmailWithAttachment(toEmail, subject, body, pdfFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de l'email avec facture");
                return false;
            }
        }

        private async Task<bool> SendEmailWithAttachment(
            string toEmail,
            string subject,
            string body,
            string attachmentPath)
        {
            try
            {
                if (_configuration == null)
                {
                    _logger.LogWarning("Configuration non disponible");
                    return false;
                }

                var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPortString = _configuration["EmailSettings:SmtpPort"] ?? "587";
                if (!int.TryParse(smtpPortString, out int smtpPort))
                {
                    smtpPort = 587;
                }

                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:Password"];
                var senderName = _configuration["EmailSettings:SenderName"] ?? "CarRental Application";

                if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
                {
                    _logger.LogWarning("Configuration email manquante");
                    return false;
                }

                using var smtpClient = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true,
                    Timeout = 30000
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };

                mailMessage.To.Add(toEmail);

                if (!string.IsNullOrEmpty(attachmentPath) && IOFile.Exists(attachmentPath)) // ✅ Utiliser IOFile
                {
                    var attachment = new Attachment(attachmentPath);
                    mailMessage.Attachments.Add(attachment);
                    _logger.LogInformation("Pièce jointe ajoutée : {FileName}", System.IO.Path.GetFileName(attachmentPath)); // ✅ Utiliser le chemin complet
                }

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Email avec pièce jointe envoyé avec succès à {Email}", toEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de l'email avec pièce jointe à {Email}", toEmail);
                return false;
            }
        }
    }
}