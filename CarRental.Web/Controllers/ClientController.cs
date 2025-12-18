using Microsoft.AspNetCore.Mvc;
using CarRental.Core.Models;
using CarRental.Core.Interfaces;
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarRental.Web.Controllers
{
    [Authorize]
    public class ClientController : Controller
    {
        private readonly IClientService _clientService;
        private readonly ILogger<ClientController> _logger;
        private readonly IConfiguration _configuration;

        public ClientController(IClientService clientService, ILogger<ClientController> logger, IConfiguration configuration)
        {
            _clientService = clientService;
            _logger = logger;
            _configuration = configuration;
        }

        // Dashboard accessible aux clients et admin (pour voir leur propre dashboard)
        [Authorize(Roles = "client,admin")]
        public async Task<IActionResult> Dashboard()
        {
            _logger.LogInformation("=== DÉBUT DASHBOARD ===");

            try
            {
                // Récupérer le client connecté
                var clientCIN = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                               User.FindFirst("CIN")?.Value;

                _logger.LogInformation("🔍 Recherche du CIN dans les claims...");
                _logger.LogInformation("ClaimTypes.NameIdentifier: {NameId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                _logger.LogInformation("CIN claim: {CIN}", User.FindFirst("CIN")?.Value);
                _logger.LogInformation("CIN final sélectionné: {ClientCIN}", clientCIN);

                // Afficher tous les claims pour debug
                var allClaims = User.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
                _logger.LogInformation("📋 Tous les claims ({Count}): {Claims}", allClaims.Count, string.Join(", ", allClaims));

                if (string.IsNullOrEmpty(clientCIN))
                {
                    _logger.LogError("❌ CIN est null ou vide !");
                    TempData["Error"] = "Client non identifié.";
                    return RedirectToAction("Login", "ClientAccount");
                }

                _logger.LogInformation("✅ CIN trouvé: {CIN}", clientCIN);
                _logger.LogInformation("📞 Appel à GetDashboardDataAsync...");

                // Récupérer les données du dashboard via le service
                var dashboardData = await _clientService.GetDashboardDataAsync(clientCIN);

                _logger.LogInformation("📦 Résultat GetDashboardDataAsync:");
                _logger.LogInformation("  - dashboardData is null: {IsNull}", dashboardData == null);

                if (dashboardData != null)
                {
                    _logger.LogInformation("  - dashboardData.Client is null: {IsClientNull}", dashboardData.Client == null);

                    if (dashboardData.Client != null)
                    {
                        _logger.LogInformation("  - Client trouvé: {Nom} {Prenom}",
                            dashboardData.Client.Nom, dashboardData.Client.Prenom);
                    }
                    else
                    {
                        _logger.LogError("❌ dashboardData.Client est NULL !");
                    }
                }
                else
                {
                    _logger.LogError("❌ dashboardData est NULL !");
                }

                if (dashboardData?.Client == null)
                {
                    _logger.LogError("❌ ERREUR: Impossible de récupérer les données du client");
                    _logger.LogError("Redirection vers Home/Index à cause de dashboardData.Client == null");
                    TempData["Error"] = "Client non trouvé dans la base de données.";
                    return RedirectToAction("Login", "ClientAccount"); // ← Changé de "Index", "Home"
                }

                _logger.LogInformation("✅ Données récupérées avec succès");
                _logger.LogInformation("  - TotalLocations: {Total}", dashboardData.TotalLocations);
                _logger.LogInformation("  - ActiveLocations: {Active}", dashboardData.ActiveLocations);
                _logger.LogInformation("  - CompletedLocations: {Completed}", dashboardData.CompletedLocations);

                // Passer les données à la vue via ViewBag
                ViewBag.Client = dashboardData.Client;
                ViewBag.TotalLocations = dashboardData.TotalLocations;
                ViewBag.ActiveLocations = dashboardData.ActiveLocations;
                ViewBag.CompletedLocations = dashboardData.CompletedLocations;
                ViewBag.ClientLocations = dashboardData.Locations;
                ViewBag.HasPendingLocations = dashboardData.PendingLocations > 0;

                _logger.LogInformation("✅ Affichage de la vue Dashboard");
                return View();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, "❌ KeyNotFoundException: Client non trouvé");
                TempData["Error"] = "Client non trouvé.";
                return RedirectToAction("Login", "ClientAccount");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception dans Dashboard");
                _logger.LogError("Type d'exception: {ExceptionType}", ex.GetType().Name);
                _logger.LogError("Message: {Message}", ex.Message);
                _logger.LogError("StackTrace: {StackTrace}", ex.StackTrace);
                TempData["Error"] = $"Erreur lors du chargement du dashboard: {ex.Message}";
                return RedirectToAction("Login", "ClientAccount");
            }
        }

        // GET: Client/Index - Accessible uniquement par admin
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var clients = await _clientService.GetAllClientsAsync();
                return View(clients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des clients");
                TempData["Error"] = "Impossible de récupérer la liste des clients.";
                return View(new List<Client>());
            }
        }

        // GET: Client/Details/AB123456 - Accessible selon le rôle
        public async Task<IActionResult> Details(string? id)
        {
            _logger.LogInformation("=== Accès à Details ===");
            _logger.LogInformation("Paramètre id reçu: {Id}", id);
            _logger.LogInformation("Utilisateur: {User}, Rôle(s): {Roles}",
                User.Identity?.Name,
                string.Join(", ", User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)));

            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("ID/CIN null ou vide");
                TempData["Error"] = "Identifiant client manquant.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var client = await _clientService.GetClientByCINAsync(id);

                if (client == null)
                {
                    _logger.LogWarning("Client non trouvé avec le CIN: {CIN}", id);
                    TempData["Error"] = $"Client avec le CIN {id} introuvable.";
                    return RedirectToAction("Index", "Home");
                }

                // Vérifier les permissions
                if (User.IsInRole("client"))
                {
                    var clientCIN = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                                   User.FindFirst("CIN")?.Value;

                    _logger.LogInformation("Client connecté - CIN: {ClientCIN}, CIN demandé: {RequestedCIN}",
                        clientCIN, id);

                    if (id != clientCIN)
                    {
                        _logger.LogWarning("Tentative d'accès non autorisé au profil {RequestedCIN} par {ClientCIN}",
                            id, clientCIN);
                        TempData["Error"] = "Vous n'avez pas accès à ce profil.";
                        return RedirectToAction("MonProfil");
                    }
                }

                _logger.LogInformation("Affichage du profil client: {Nom} {Prenom} (CIN: {CIN})",
                    client.Nom, client.Prenom, client.CIN);
                return View(client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du client {CIN}", id);
                TempData["Error"] = "Une erreur s'est produite lors de la récupération du profil.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Client/MonProfil - Pour les clients
        [Authorize(Roles = "client")]
        public async Task<IActionResult> MonProfil()
        {
            try
            {
                var clientCIN = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                               User.FindFirst("CIN")?.Value;

                _logger.LogInformation("Accès MonProfil - CIN: {CIN}", clientCIN);

                if (string.IsNullOrEmpty(clientCIN))
                {
                    _logger.LogWarning("CIN non trouvé dans les claims");
                    TempData["Error"] = "Client non identifié. Veuillez vous reconnecter.";
                    return RedirectToAction("Login", "ClientAccount");
                }

                var client = await _clientService.GetClientByCINAsync(clientCIN);
                if (client == null)
                {
                    _logger.LogWarning("Client non trouvé en base avec CIN: {CIN}", clientCIN);
                    TempData["Error"] = "Profil non trouvé.";
                    return RedirectToAction("Login", "ClientAccount");
                }

                return View("Details", client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du profil");
                TempData["Error"] = "Impossible de récupérer votre profil.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: Client/Create - Accessible par Admin
        [Authorize(Roles = "admin")]
        public IActionResult Create()
        {
            var model = new Client
            {
                DateInscription = DateTime.Now
            };
            return View(model);
        }

        // POST: Client/Create - Accessible par Admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create(Client client)
        {
            try
            {
                _logger.LogInformation("=== DEBUT CREATION CLIENT ===");
                _logger.LogInformation("CIN reçu: {CIN}", client.CIN);
                _logger.LogInformation("Email reçu: {Email}", client.Email);

                // IMPORTANT: Capturer le mot de passe en clair AVANT le hashing
                string plainPassword = client.Password!;

                // Définir DateInscription si non définie
                if (client.DateInscription == default)
                    client.DateInscription = DateTime.Now;

                // Nettoyer ModelState pour les champs non pertinents
                ModelState.Remove("ConfirmPassword");

                if (!ModelState.IsValid)
                {
                    _logger.LogError("Validation échouée");
                    foreach (var entry in ModelState)
                    {
                        if (entry.Value?.Errors.Count > 0)
                        {
                            foreach (var error in entry.Value.Errors)
                            {
                                _logger.LogError("Erreur [{Key}]: {Message}", entry.Key, error.ErrorMessage);
                            }
                        }
                    }
                    return View(client);
                }

                // Créer le client via le service
                var createdClient = await _clientService.CreateClientAsync(client);
                _logger.LogInformation("Client créé avec succès - CIN: {CIN}", client.CIN);

                // Envoyer email de bienvenue avec le mot de passe
                bool emailSent = await SendWelcomeEmail(
                    client.Email!,
                    client.Nom!,
                    client.Prenom!,
                    plainPassword,
                    client.CIN!
                );

                if (emailSent)
                {
                    _logger.LogInformation("SUCCÈS: Email de bienvenue envoyé avec succès à {Email}", client.Email);
                    TempData["Success"] = $"Client {client.Nom} {client.Prenom} créé avec succès. Un email avec le mot de passe a été envoyé à {client.Email}.";
                }
                else
                {
                    _logger.LogWarning("ÉCHEC: Échec de l'envoi de l'email à {Email}", client.Email);
                    TempData["Warning"] = $"Client {client.Nom} {client.Prenom} créé avec succès, mais l'envoi de l'email a échoué. Le mot de passe est: '{plainPassword}'. Veuillez le communiquer manuellement au client.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Erreur de validation métier");
                ModelState.AddModelError("", ex.Message);
                TempData["Error"] = ex.Message;
                return View(client);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Erreur d'argument");
                ModelState.AddModelError("", ex.Message);
                TempData["Error"] = ex.Message;
                return View(client);
            }
            catch (DbUpdateException dbEx) when (IsDuplicateEmailError(dbEx))
            {
                _logger.LogWarning(dbEx, "Email déjà utilisé: {Email}", client.Email);
                ModelState.AddModelError("Email", "Cet email est déjà utilisé par un autre client.");
                TempData["Error"] = $"L'email '{client.Email}' est déjà utilisé par un autre client.";
                return View(client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur inattendue lors de la création");
                TempData["Error"] = "Une erreur inattendue s'est produite.";
                return View(client);
            }
        }

        // GET: Client/Edit/AB123456 - Accessible par Admin pour modifier d'autres clients
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(string? cin)
        {
            if (string.IsNullOrEmpty(cin)) return NotFound();

            try
            {
                var client = await _clientService.GetClientByCINAsync(cin);
                if (client == null) return NotFound();

                return View(client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du client {CIN}", cin);
                return NotFound();
            }
        }

        // GET: Client/EditMonProfil - Pour les clients qui veulent modifier leur propre profil
        [Authorize(Roles = "client")]
        public async Task<IActionResult> EditMonProfil()
        {
            try
            {
                var clientCIN = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                               User.FindFirst("CIN")?.Value;

                if (string.IsNullOrEmpty(clientCIN))
                {
                    TempData["Error"] = "Client non identifié. Veuillez vous reconnecter.";
                    return RedirectToAction("Login", "ClientAccount");
                }

                var client = await _clientService.GetClientByCINAsync(clientCIN);
                if (client == null)
                {
                    TempData["Error"] = "Profil non trouvé.";
                    return RedirectToAction("Index", "Home");
                }

                return View("Edit", client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du profil pour modification");
                TempData["Error"] = "Impossible de récupérer votre profil.";
                return RedirectToAction("MonProfil");
            }
        }

        // POST: Client/Edit/AB123456 - Accessible par Admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Edit(string cin, Client client)
        {
            if (cin != client.CIN)
                return NotFound();

            try
            {
                ModelState.Remove("ConfirmPassword");
                ModelState.Remove("Password");

                if (!ModelState.IsValid)
                {
                    foreach (var error in ModelState.Where(e => e.Value?.Errors.Count > 0))
                    {
                        _logger.LogWarning("Erreur validation {Key}: {Error}",
                            error.Key,
                            string.Join(", ", error.Value!.Errors.Select(e => e.ErrorMessage)));
                    }
                    return View(client);
                }

                // Récupérer l'ancien client pour comparer
                var oldClient = await _clientService.GetClientByCINAsync(cin);
                if (oldClient == null)
                    return NotFound();

                bool emailChanged = oldClient.Email != client.Email;
                string oldEmail = oldClient.Email!;

                // Conserver DateInscription et Password
                client.DateInscription = oldClient.DateInscription;
                client.Password = oldClient.Password;

                // Mettre à jour via le service
                await _clientService.UpdateClientAsync(client);

                _logger.LogInformation("Client modifié: {CIN}", cin);

                // Envoyer email de confirmation de mise à jour
                bool emailSent = await SendUpdateConfirmationEmail(
                    oldEmail,
                    client.Nom!,
                    client.Prenom!,
                    client.CIN!,
                    emailChanged,
                    emailChanged ? client.Email : null
                );

                if (emailSent)
                {
                    _logger.LogInformation("Email de confirmation envoyé pour la modification du client {CIN}", cin);
                    TempData["Success"] = "Client modifié avec succès. Un email de confirmation a été envoyé au client.";
                }
                else
                {
                    _logger.LogWarning("Échec de l'envoi de l'email de confirmation pour {CIN}", cin);
                    TempData["Warning"] = "Client modifié avec succès, mais l'envoi de l'email de confirmation a échoué.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Client {CIN} non trouvé", cin);
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Erreur de validation métier");
                ModelState.AddModelError("", ex.Message);
                TempData["Error"] = ex.Message;
                return View(client);
            }
            catch (DbUpdateException dbEx) when (IsDuplicateEmailError(dbEx))
            {
                _logger.LogWarning(dbEx, "Email déjà utilisé lors de la modification: {Email}", client.Email);
                ModelState.AddModelError("Email", "Cet email est déjà utilisé par un autre client.");
                TempData["Error"] = $"L'email '{client.Email}' est déjà utilisé par un autre client. Veuillez en choisir un autre.";
                return View(client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification du client {CIN}", cin);
                TempData["Error"] = "Erreur lors de la modification.";
                return View(client);
            }
        }

        // POST: Client/EditMonProfil - Pour les clients qui modifient leur propre profil
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "client")]
        public async Task<IActionResult> EditMonProfil(Client client)
        {
            var clientCIN = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                           User.FindFirst("CIN")?.Value;

            if (string.IsNullOrEmpty(clientCIN) || clientCIN != client.CIN)
            {
                TempData["Error"] = "Action non autorisée.";
                return RedirectToAction("MonProfil");
            }

            try
            {
                ModelState.Remove("ConfirmPassword");
                ModelState.Remove("Password");

                if (!ModelState.IsValid)
                {
                    return View("Edit", client);
                }

                // Récupérer l'ancien client pour comparer
                var oldClient = await _clientService.GetClientByCINAsync(clientCIN);
                if (oldClient == null)
                {
                    TempData["Error"] = "Profil non trouvé.";
                    return RedirectToAction("MonProfil");
                }

                bool emailChanged = oldClient.Email != client.Email;
                string oldEmail = oldClient.Email!;

                // Conserver DateInscription et Password
                client.DateInscription = oldClient.DateInscription;
                client.Password = oldClient.Password;

                // Mettre à jour via le service
                await _clientService.UpdateClientAsync(client);

                _logger.LogInformation("Client modifié son propre profil: {CIN}", clientCIN);

                // Envoyer email de confirmation de mise à jour
                bool emailSent = await SendUpdateConfirmationEmail(
                    oldEmail,
                    client.Nom!,
                    client.Prenom!,
                    client.CIN!,
                    emailChanged,
                    emailChanged ? client.Email : null
                );

                if (emailSent)
                {
                    TempData["Success"] = "Profil modifié avec succès. Un email de confirmation a été envoyé.";
                }
                else
                {
                    TempData["Warning"] = "Profil modifié avec succès, mais l'envoi de l'email de confirmation a échoué.";
                }

                return RedirectToAction("MonProfil");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Erreur de validation métier");
                ModelState.AddModelError("", ex.Message);
                TempData["Error"] = ex.Message;
                return View("Edit", client);
            }
            catch (DbUpdateException dbEx) when (IsDuplicateEmailError(dbEx))
            {
                _logger.LogWarning(dbEx, "Email déjà utilisé lors de la modification: {Email}", client.Email);
                ModelState.AddModelError("Email", "Cet email est déjà utilisé par un autre client.");
                TempData["Error"] = $"L'email '{client.Email}' est déjà utilisé par un autre client. Veuillez en choisir un autre.";
                return View("Edit", client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification du profil");
                TempData["Error"] = "Erreur lors de la modification.";
                return View("Edit", client);
            }
        }

        // GET: Client/Delete/AB123456 - Accessible uniquement par Admin
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(string? cin)
        {
            if (string.IsNullOrEmpty(cin)) return NotFound();

            try
            {
                var client = await _clientService.GetClientByCINAsync(cin);
                if (client == null) return NotFound();

                return View(client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du client {CIN}", cin);
                return NotFound();
            }
        }

        // POST: Client/Delete - Accessible uniquement par Admin
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteConfirmed(string cin)
        {
            try
            {
                // Récupérer le client avant suppression pour envoyer l'email
                var client = await _clientService.GetClientByCINAsync(cin);

                if (client != null)
                {
                    // Envoyer email de notification de suppression
                    bool emailSent = await SendDeletionNotificationEmail(
                        client.Email!,
                        client.Nom!,
                        client.Prenom!,
                        client.CIN!
                    );

                    if (emailSent)
                    {
                        _logger.LogInformation("Email de notification de suppression envoyé à {Email}", client.Email);
                    }
                }

                await _clientService.DeleteClientAsync(cin);
                _logger.LogInformation("Client supprimé: {CIN}", cin);
                TempData["Success"] = "Client supprimé avec succès. Un email de notification a été envoyé au client.";
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Client {CIN} non trouvé", cin);
                TempData["Warning"] = "Le client n'existe plus.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du client {CIN}", cin);
                TempData["Error"] = "Erreur lors de la suppression.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ========== MÉTHODES UTILITAIRES ==========

        // Méthode pour détecter les erreurs de duplication d'email dans DbUpdateException
        private bool IsDuplicateEmailError(DbUpdateException ex)
        {
            if (ex.InnerException is MySqlConnector.MySqlException mysqlEx)
            {
                // Code d'erreur MySQL pour violation de contrainte unique
                if (mysqlEx.Number == 1062)
                {
                    return mysqlEx.Message.ToLower().Contains("email");
                }
            }
            return false;
        }

        // Email de bienvenue pour nouveau client
        private async Task<bool> SendWelcomeEmail(string toEmail, string nom, string prenom, string password, string cin)
        {
            _logger.LogInformation("=== DEBUT ENVOI EMAIL DE BIENVENUE ===");
            _logger.LogInformation("Destinataire: {ToEmail}", toEmail);
            _logger.LogInformation("Nom: {Nom} {Prenom}", nom, prenom);

            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPortString = _configuration["EmailSettings:SmtpPort"] ?? "587";
                if (!int.TryParse(smtpPortString, out int smtpPort))
                {
                    smtpPort = 587;
                }

                var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "badiamohamedaymane@gmail.com";
                var senderPassword = _configuration["EmailSettings:Password"];
                var senderName = _configuration["EmailSettings:SenderName"] ?? "CarRental Application";

                if (string.IsNullOrEmpty(senderPassword))
                {
                    _logger.LogError("ERREUR: Le mot de passe d'application email n'est pas configuré");
                    return false;
                }

                var smtpClient = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Timeout = 30000
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = "Bienvenue - Votre compte CarRental a été créé",
                    Body = $@"Bonjour {prenom} {nom},

Bienvenue sur notre plateforme CarRental !

Votre compte a été créé avec succès. Voici vos informations de connexion :

**Informations de votre compte :**
- CIN : {cin}
- Nom : {nom} {prenom}
- Email : {toEmail}
- Mot de passe : {password}

**Important :**
Pour des raisons de sécurité, nous vous recommandons de changer ce mot de passe lors de votre première connexion.

**Accès à votre espace client :**
Vous pouvez désormais vous connecter à votre espace client pour :
- Consulter et modifier vos informations personnelles
- Réserver des véhicules
- Consulter l'historique de vos locations

Cordialement,
L'équipe CarRental",
                    IsBodyHtml = false
                };

                mailMessage.To.Add(toEmail);
                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("SUCCÈS: Email envoyé");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi de l'email");
                return false;
            }
        }

        // Email de confirmation de mise à jour
        private async Task<bool> SendUpdateConfirmationEmail(string toEmail, string nom, string prenom, string cin, bool emailChanged = false, string? newEmail = null)
        {
            try
            {
                var subject = emailChanged ?
                    "Mise à jour de votre compte CarRental - Email modifié" :
                    "Confirmation de mise à jour de votre compte CarRental";

                var body = emailChanged && !string.IsNullOrEmpty(newEmail) ?
                    $@"Bonjour {prenom} {nom},

Votre adresse email a été modifiée :
- Ancien email : {toEmail}
- Nouvel email : {newEmail}

Utilisez désormais {newEmail} pour vous connecter.

Cordialement,
L'équipe CarRental" :
                    $@"Bonjour {prenom} {nom},

Vos informations ont été mises à jour avec succès.

Cordialement,
L'équipe CarRental";

                return await SendEmail(toEmail, subject, body);
            }
            catch
            {
                return false;
            }
        }

        // Email de notification de suppression
        private async Task<bool> SendDeletionNotificationEmail(string toEmail, string nom, string prenom, string cin)
        {
            try
            {
                var subject = "Notification - Suppression de votre compte CarRental";
                var body = $@"Bonjour {prenom} {nom},

Votre compte CarRental (CIN: {cin}) a été supprimé.

Si vous pensez qu'il s'agit d'une erreur, contactez notre service client.

Cordialement,
L'équipe CarRental";

                return await SendEmail(toEmail, subject, body);
            }
            catch
            {
                return false;
            }
        }

        // Méthode générique pour envoyer des emails
        private async Task<bool> SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPortString = _configuration["EmailSettings:SmtpPort"] ?? "587";
                if (!int.TryParse(smtpPortString, out int smtpPort))
                {
                    smtpPort = 587;
                }

                var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "badiamohamedaymane@gmail.com";
                var senderPassword = _configuration["EmailSettings:Password"];
                var senderName = _configuration["EmailSettings:SenderName"] ?? "CarRental Application";

                if (string.IsNullOrEmpty(senderPassword))
                    return false;

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
                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}