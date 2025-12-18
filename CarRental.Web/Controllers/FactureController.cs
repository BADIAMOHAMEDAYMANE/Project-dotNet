using CarRental.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CarRental.Web.Controllers
{
    public class FactureController : Controller
    {
        private readonly IFactureService _factureService;
        private readonly ILogger<FactureController> _logger;

        // Clé secrète pour générer les tokens (à mettre dans appsettings.json en production)
        private const string SECRET_KEY = "VotreCleSecrete123!ChangezMoi"; // ⚠️ À CHANGER !

        public FactureController(IFactureService factureService, ILogger<FactureController> logger)
        {
            _factureService = factureService;
            _logger = logger;
        }

        // =========================
        // Liste des factures du client
        // =========================
        [Authorize(Roles = "client")]
        public async Task<IActionResult> Index()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "ClientAccount");
            }

            var factures = await _factureService.GetFacturesByClientEmailAsync(userEmail);

            _logger.LogInformation($"Client {userEmail} a {factures.Count()} facture(s)");

            return View(factures);
        }

        // =========================
        // Détails d'une facture (Client)
        // =========================
        [Authorize(Roles = "client")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "ID de facture manquant.";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation($"Tentative d'accès à la facture ID: {id}");

            var facture = await _factureService.GetByIdAsync(id.Value);

            if (facture == null)
            {
                _logger.LogWarning($"Facture {id} introuvable");
                TempData["Error"] = "Facture introuvable.";
                return RedirectToAction(nameof(Index));
            }

            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            // LOGS DE DÉBOGAGE
            _logger.LogInformation("=== DÉBOGAGE FACTURE ===");
            _logger.LogInformation($"Facture ID: {facture.Id}");
            _logger.LogInformation($"Location null? {facture.Location == null}");
            _logger.LogInformation($"LocationId: {facture.LocationId}");

            if (facture.Location != null)
            {
                _logger.LogInformation($"Location ID: {facture.Location.Id}");
                _logger.LogInformation($"Client null? {facture.Location.Client == null}");

                if (facture.Location.Client != null)
                {
                    _logger.LogInformation($"Email Client: {facture.Location.Client.Email}");
                }
                else
                {
                    _logger.LogWarning("Client est NULL - Relations non chargées!");
                }
            }
            else
            {
                _logger.LogWarning("Location est NULL - Relations non chargées!");
            }

            _logger.LogInformation($"Email User connecté: {userEmail}");
            _logger.LogInformation("========================");

            // Vérifications avec messages clairs
            if (facture.Location == null)
            {
                _logger.LogError($"Location null pour facture {id}");
                TempData["Error"] = "Les informations de location ne sont pas disponibles. Contactez le support.";
                return RedirectToAction(nameof(Index));
            }

            if (facture.Location.Client == null)
            {
                _logger.LogError($"Client null pour location {facture.Location.Id}");
                TempData["Error"] = "Les informations du client ne sont pas disponibles. Contactez le support.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrEmpty(facture.Location.Client.Email))
            {
                _logger.LogError($"Email client vide pour facture {id}");
                TempData["Error"] = "L'email du client est manquant. Contactez le support.";
                return RedirectToAction(nameof(Index));
            }

            if (facture.Location.Client.Email.ToLower() != userEmail.ToLower())
            {
                _logger.LogWarning($"Accès refusé - Email client: {facture.Location.Client.Email}, Email user: {userEmail}");
                TempData["Error"] = $"Vous n'avez pas accès à cette facture. (Votre email: {userEmail})";
                return RedirectToAction(nameof(Index));
            }

            _logger.LogInformation($"Accès autorisé à la facture {id} pour {userEmail}");
            return View(facture);
        }

        // =========================
        // QR Code avec jeton sécurisé
        // =========================
        [Authorize(Roles = "client")]
        public async Task<IActionResult> QrCode(int id)
        {
            var facture = await _factureService.GetByIdAsync(id);
            if (facture == null)
            {
                return NotFound();
            }

            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            if (facture.Location?.Client?.Email?.ToLower() != userEmail?.ToLower())
            {
                return Forbid();
            }

            // Générer un jeton sécurisé avec expiration
            var token = GenerateSecureToken(id);

            string host = Request.Host.Host;
            if (host == "localhost" || host == "127.0.0.1")
            {
                host = "192.168.11.185"; // ⚠️ REMPLACEZ par VOTRE IP
            }

            // URL avec le jeton au lieu de l'authentification
            string url = $"http://192.168.11.185:5048/Facture/DownloadSecure?id={id}&token={token}";

            try
            {
                using var generator = new QRCodeGenerator();
                using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
                using var qr = new QRCode(data);
                using var bitmap = qr.GetGraphic(20);
                using var ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);

                _logger.LogInformation($"QR Code généré avec URL sécurisée: {url}");

                return File(ms.ToArray(), "image/png");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur génération QR Code: {ex.Message}");
                return NotFound();
            }
        }

        // =========================
        // Téléchargement sécurisé SANS authentification (avec jeton)
        // =========================
        [AllowAnonymous] // ✅ Pas besoin d'être connecté !
        public async Task<IActionResult> DownloadSecure(int id, string token)
        {
            // Vérifier le jeton
            if (!ValidateSecureToken(id, token))
            {
                _logger.LogWarning($"Tentative d'accès avec jeton invalide pour facture {id}");
                return Unauthorized("Lien invalide ou expiré");
            }

            var facture = await _factureService.GetByIdAsync(id);

            if (facture == null || string.IsNullOrEmpty(facture.CheminFichier))
            {
                return NotFound("Facture introuvable");
            }

            if (!System.IO.File.Exists(facture.CheminFichier))
            {
                return NotFound("Fichier introuvable sur le serveur");
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(facture.CheminFichier);
            var contentType = facture.Format == "CSV"
                ? "text/csv"
                : "application/pdf";
            var fileName = Path.GetFileName(facture.CheminFichier);

            _logger.LogInformation($"Téléchargement sécurisé de la facture {id} via QR Code");

            return File(fileBytes, contentType, fileName);
        }

        // =========================
        // Télécharger la facture (Client authentifié)
        // =========================
        [Authorize(Roles = "client")]
        public async Task<IActionResult> Download(int id)
        {
            var facture = await _factureService.GetByIdAsync(id);

            if (facture == null || string.IsNullOrEmpty(facture.CheminFichier))
            {
                TempData["Error"] = "Facture introuvable ou fichier manquant.";
                return RedirectToAction(nameof(Index));
            }

            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            if (facture.Location?.Client?.Email?.ToLower() != userEmail?.ToLower())
            {
                TempData["Error"] = "Vous n'avez pas accès à cette facture.";
                return RedirectToAction(nameof(Index));
            }

            if (!System.IO.File.Exists(facture.CheminFichier))
            {
                TempData["Error"] = "Fichier introuvable sur le serveur.";
                return RedirectToAction(nameof(Index));
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(facture.CheminFichier);
            var contentType = facture.Format == "CSV"
                ? "text/csv"
                : "application/pdf";
            var fileName = Path.GetFileName(facture.CheminFichier);

            return File(fileBytes, contentType, fileName);
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _factureService.DeleteAsync(id);
            TempData["Success"] = "Facture supprimée avec succès.";
            return RedirectToAction("Index", "Location");
        }

        // =========================
        // MÉTHODES PRIVÉES POUR LA SÉCURITÉ
        // =========================

        /// <summary>
        /// Génère un jeton sécurisé pour télécharger une facture
        /// Format: {id}:{timestamp}:{hash}
        /// </summary>
        private string GenerateSecureToken(int factureId)
        {
            // Jeton valide pendant 24 heures
            var expirationTime = DateTimeOffset.UtcNow.AddHours(24).ToUnixTimeSeconds();
            var data = $"{factureId}:{expirationTime}";

            // Créer un hash sécurisé
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SECRET_KEY));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            var hashString = Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").TrimEnd('=');

            return $"{factureId}:{expirationTime}:{hashString}";
        }

        /// <summary>
        /// Valide un jeton de téléchargement
        /// </summary>
        private bool ValidateSecureToken(int factureId, string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            try
            {
                var parts = token.Split(':');
                if (parts.Length != 3)
                    return false;

                var tokenFactureId = int.Parse(parts[0]);
                var expirationTime = long.Parse(parts[1]);
                var receivedHash = parts[2];

                // Vérifier l'ID
                if (tokenFactureId != factureId)
                    return false;

                // Vérifier l'expiration
                var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (currentTime > expirationTime)
                {
                    _logger.LogWarning($"Jeton expiré pour facture {factureId}");
                    return false;
                }

                // Recalculer le hash pour vérifier l'intégrité
                var data = $"{tokenFactureId}:{expirationTime}";
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SECRET_KEY));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                var expectedHash = Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").TrimEnd('=');

                return receivedHash == expectedHash;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erreur validation jeton: {ex.Message}");
                return false;
            }
        }
    }
}