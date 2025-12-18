using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using CarRental.Core.Models;
using CarRental.Core.Interfaces;

namespace CarRental.Web.Controllers
{
    public class ClientAccountController : Controller
    {
        private readonly IClientAuthService _authService;
        private readonly IClientService _clientService;
        private readonly ILogger<ClientAccountController> _logger;

        public ClientAccountController(
            IClientAuthService authService,
            IClientService clientService,
            ILogger<ClientAccountController> logger)
        {
            _authService = authService;
            _clientService = clientService;
            _logger = logger;
        }

        // GET: /ClientAccount/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            _logger.LogInformation("=== GET /ClientAccount/Login ===");
            _logger.LogInformation("ReturnUrl: {ReturnUrl}", returnUrl);
            _logger.LogInformation("User already authenticated: {IsAuth}", User.Identity?.IsAuthenticated);

            // Si l'utilisateur est déjà connecté, le rediriger vers le dashboard
            if (User.Identity?.IsAuthenticated == true)
            {
                _logger.LogInformation("Utilisateur déjà connecté, redirection vers Dashboard");
                return RedirectToAction("Dashboard", "Client");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new ClientLoginViewModel());
        }

        // POST: /ClientAccount/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Login(ClientLoginViewModel model, string? returnUrl = null)
        {
            _logger.LogInformation("=== POST /ClientAccount/Login ===");
            _logger.LogInformation("Email: {Email}", model.Email);
            _logger.LogInformation("RememberMe: {RememberMe}", model.RememberMe);
            _logger.LogInformation("ReturnUrl: {ReturnUrl}", returnUrl);

            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("❌ ModelState invalide");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    _logger.LogWarning("Erreur: {ErrorMessage}", error.ErrorMessage);
                }
                return View(model);
            }

            try
            {
                _logger.LogDebug("Appel à ValidateLoginAsync pour {Email}", model.Email);
                var client = await _authService.ValidateLoginAsync(model.Email, model.Password);

                if (client == null)
                {
                    _logger.LogWarning("❌ Authentification échouée pour {Email}", model.Email);
                    ModelState.AddModelError(string.Empty, "Email ou mot de passe incorrect.");
                    return View(model);
                }

                _logger.LogInformation("✅ Client trouvé: {Nom} {Prenom} (CIN: {CIN})",
                    client.Nom, client.Prenom, client.CIN);

                // Créer les claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, client.CIN),
                    new Claim(ClaimTypes.Email, client.Email),
                    new Claim(ClaimTypes.Name, $"{client.Nom} {client.Prenom}"),
                    new Claim(ClaimTypes.Role, "client"),
                    new Claim("ClientId", client.CIN),
                    new Claim("FullName", $"{client.Nom} {client.Prenom}"),
                    new Claim("CIN", client.CIN)
                };

                _logger.LogInformation("📝 Claims créés: {ClaimsCount}", claims.Count);
                foreach (var claim in claims)
                {
                    _logger.LogDebug("  - {Type}: {Value}", claim.Type, claim.Value);
                }

                var claimsIdentity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe
                        ? DateTimeOffset.UtcNow.AddDays(30)
                        : DateTimeOffset.UtcNow.AddHours(2),
                    AllowRefresh = true,
                    IssuedUtc = DateTimeOffset.UtcNow
                };

                _logger.LogInformation("🔐 Création du cookie d'authentification...");

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("✅ Cookie créé avec succès");
                _logger.LogInformation("User.Identity.IsAuthenticated: {IsAuth}", User.Identity?.IsAuthenticated);
                _logger.LogInformation("User.Identity.Name: {Name}", User.Identity?.Name);

                // Vérifier les claims après SignIn
                var userClaims = User.Claims.Select(c => $"{c.Type}={c.Value}").ToList();
                _logger.LogInformation("Claims dans User après SignIn: {Claims}", string.Join(", ", userClaims));

                // Redirection sécurisée
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    _logger.LogInformation("➡️  Redirection vers returnUrl: {ReturnUrl}", returnUrl);
                    return LocalRedirect(returnUrl);
                }

                // ✅ REDIRECTION VERS LE DASHBOARD
                _logger.LogInformation("➡️  Redirection vers /Client/Dashboard");
                return RedirectToAction("Dashboard", "Client");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERREUR lors de l'authentification pour {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Une erreur technique est survenue. Veuillez réessayer.");
                return View(model);
            }
        }

        // POST: /ClientAccount/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userName = User.Identity?.Name;
            _logger.LogInformation("=== POST /ClientAccount/Logout ===");
            _logger.LogInformation("Déconnexion de: {UserName}", userName);

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            _logger.LogInformation("✅ Déconnexion réussie");
            return RedirectToAction("Login", "ClientAccount");
        }

        // GET: /ClientAccount/Logout (fallback)
        [HttpGet]
        public async Task<IActionResult> LogoutGet()
        {
            _logger.LogInformation("=== GET /ClientAccount/Logout (fallback) ===");
            return await Logout();
        }

        // GET: /ClientAccount/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            _logger.LogWarning("=== ACCÈS REFUSÉ ===");
            _logger.LogWarning("User: {User}", User.Identity?.Name ?? "Anonyme");
            _logger.LogWarning("Path: {Path}", HttpContext.Request.Path);
            _logger.LogWarning("IsAuthenticated: {IsAuth}", User.Identity?.IsAuthenticated);

            return View();
        }

        // GET: /ClientAccount/Profile - Affichage du profil en lecture seule
        [HttpGet]
        [Authorize(Roles = "client")]
        public async Task<IActionResult> Profile()
        {
            _logger.LogInformation("=== GET /ClientAccount/Profile ===");
            _logger.LogInformation("User: {Name}", User.Identity?.Name);

            try
            {
                var cin = User.FindFirstValue("CIN") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(cin))
                {
                    _logger.LogWarning("❌ CIN non trouvé dans les claims");
                    TempData["Error"] = "Informations de profil introuvables.";
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogInformation("🔍 Recherche du client avec CIN: {CIN}", cin);

                // Récupérer les informations du client
                var client = await _clientService.GetClientByCINAsync(cin);

                if (client == null)
                {
                    _logger.LogWarning("❌ Client non trouvé avec CIN: {CIN}", cin);
                    TempData["Error"] = "Profil non trouvé.";
                    return RedirectToAction("Dashboard", "Client");
                }

                _logger.LogInformation("✅ Affichage du profil pour: {Nom} {Prenom}", client.Nom, client.Prenom);
                return View(client);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur lors de la récupération du profil");
                TempData["Error"] = "Une erreur est survenue lors du chargement de votre profil.";
                return RedirectToAction("Dashboard", "Client");
            }
        }

        // ✅ MÉTHODE DE DEBUG - À SUPPRIMER APRÈS TESTS
        [HttpGet]
        [Authorize(Roles = "client")]
        public IActionResult DebugAuth()
        {
            _logger.LogInformation("=== DEBUG AUTH ===");
            _logger.LogInformation("IsAuthenticated: {IsAuth}", User.Identity?.IsAuthenticated);
            _logger.LogInformation("Name: {Name}", User.Identity?.Name);
            _logger.LogInformation("AuthenticationType: {Type}", User.Identity?.AuthenticationType);

            var claims = User.Claims.Select(c => $"{c.Type}: {c.Value}").ToList();
            _logger.LogInformation("Claims count: {Count}", claims.Count);
            foreach (var claim in claims)
            {
                _logger.LogInformation("  - {Claim}", claim);
            }

            var debugInfo = $@"
=== INFORMATIONS D'AUTHENTIFICATION ===

IsAuthenticated: {User.Identity?.IsAuthenticated}
Name: {User.Identity?.Name}
AuthenticationType: {User.Identity?.AuthenticationType}

=== CLAIMS ({claims.Count}) ===
{string.Join("\n", claims)}
";

            return Content(debugInfo, "text/plain");
        }
    }
}