using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using CarRental.Core.Interfaces;
using CarRental.Core.Models;

namespace CarRental.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IAuthService authService,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        // GET: /Account/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            // Si déjà connecté, rediriger vers la page appropriée selon le rôle
            if (User.Identity?.IsAuthenticated == true)
            {
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                if (role == "admin")
                    return RedirectToAction("Dashboard", "Admin");
                else if (role == "Employe" || role == "employee" || role == "employé" || role == "employe")
                    return RedirectToAction("Dashboard", "Employee");
                else
                    return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Valider les identifiants
                var employee = await _authService.ValidateLoginAsync(model.Email, model.Password);

                if (employee == null)
                {
                    ModelState.AddModelError(string.Empty, "Email ou mot de passe incorrect");
                    _logger.LogWarning("Tentative de connexion échouée pour l'email: {Email}", model.Email);
                    return View(model);
                }

                // Créer les claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, employee.ID.ToString()),
                    new Claim(ClaimTypes.Name, employee.NomComplet),
                    new Claim(ClaimTypes.Email, employee.Email),
                    new Claim(ClaimTypes.Role, employee.Role),
                    new Claim("EmployeeId", employee.ID.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe
                        ? DateTimeOffset.UtcNow.AddDays(30)
                        : DateTimeOffset.UtcNow.AddHours(24),
                    AllowRefresh = true
                };

                // Créer la session
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("Connexion réussie pour {Nom} (Role: {Role})",
                    employee.NomComplet, employee.Role);

                // Redirection selon le ReturnUrl ou le rôle
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                else
                {
                    // Redirection selon le rôle
                    var role = employee.Role.ToLower();

                    if (role == "admin")
                    {
                        return RedirectToAction("Dashboard", "Admin");
                    }
                    else if (role == "employe" || role == "employee" || role == "employé" || role == "staff")
                    {
                        return RedirectToAction("Dashboard", "Employee");
                    }
                    else
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la connexion");
                ModelState.AddModelError(string.Empty, "Une erreur s'est produite lors de la connexion");
                return View(model);
            }
        }

        // GET: /Account/Register
        [HttpGet]
        [Authorize(Roles = "admin")]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [Authorize(Roles = "admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var employee = new Employee
                {
                    Nom = model.Nom,
                    Prenom = model.Prenom,
                    Email = model.Email,
                    Role = model.Role
                };

                await _authService.RegisterEmployeeAsync(employee, model.Password);

                _logger.LogInformation("Nouvel employé enregistré: {Email}", model.Email);

                TempData["Success"] = "Compte créé avec succès !";
                return RedirectToAction("Dashboard", "Admin");
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'enregistrement");
                ModelState.AddModelError(string.Empty, "Une erreur s'est produite lors de l'enregistrement");
                return View(model);
            }
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userName = User.Identity?.Name ?? "Utilisateur inconnu";
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            _logger.LogInformation("Déconnexion de {UserName} (Rôle: {Role})", userName, userRole);

            TempData["Info"] = "Vous avez été déconnecté avec succès";
            return RedirectToAction("Login");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            var userName = User.Identity?.Name ?? "Anonyme";
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            _logger.LogWarning("Accès refusé pour l'utilisateur: {User} (Rôle: {Role})", userName, userRole);

            return View();
        }

        // GET: /Account/Profile - Redirige vers le bon profil selon le rôle
        [Authorize]
        [HttpGet]
        public IActionResult Profile()
        {
            _logger.LogInformation("=== GET /Account/Profile ===");
            _logger.LogInformation("User: {Name}", User.Identity?.Name);
            _logger.LogInformation("IsAuthenticated: {IsAuth}", User.Identity?.IsAuthenticated);

            // Récupérer le rôle de l'utilisateur
            var userRole = User.FindFirstValue(ClaimTypes.Role);

            _logger.LogInformation("Rôle de l'utilisateur: {Role}", userRole);

            if (string.IsNullOrEmpty(userRole))
            {
                _logger.LogWarning("❌ Aucun rôle trouvé pour l'utilisateur");
                TempData["Error"] = "Impossible de déterminer votre type de compte.";
                return RedirectToAction("Index", "Home");
            }

            // Normaliser le rôle pour comparaison
            var normalizedRole = userRole.ToLower().Trim();

            // Redirection vers le profil approprié selon le rôle
            if (normalizedRole == "admin")
            {
                _logger.LogInformation("➡️  Redirection vers /Admin/Profile (rôle: admin)");
                return RedirectToAction("Profile", "Admin");
            }
            else if (normalizedRole == "employe" || normalizedRole == "employee" ||
                     normalizedRole == "employé" || normalizedRole == "staff")
            {
                _logger.LogInformation("➡️  Redirection vers /Employee/Profile (rôle: {Role})", userRole);
                return RedirectToAction("Profile", "Employee");
            }
            else if (normalizedRole == "client")
            {
                _logger.LogInformation("➡️  Redirection vers /ClientAccount/Profile (rôle: client)");
                return RedirectToAction("Profile", "ClientAccount");
            }
            else
            {
                _logger.LogWarning("❌ Rôle non reconnu: {Role}", userRole);
                TempData["Error"] = $"Type de compte non reconnu: {userRole}";
                return RedirectToAction("Index", "Home");
            }
        }
    }
}