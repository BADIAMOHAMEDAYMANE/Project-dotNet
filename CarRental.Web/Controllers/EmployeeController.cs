using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CarRental.Core.Interfaces;
using CarRental.Core.Models;
using System.Security.Claims;

namespace CarRental.Web.Controllers
{
    [Authorize(Roles = "Employe,employee,employé,employe")]
    public class EmployeeController : Controller
    {
        private readonly IEmployeeService _employeeService;
        private readonly ILocationService _locationService;
        private readonly IVehiculeService _vehiculeService;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(
            IEmployeeService employeeService,
            ILocationService locationService,
            IVehiculeService vehiculeService,
            ILogger<EmployeeController> logger)
        {
            _employeeService = employeeService;
            _locationService = locationService;
            _vehiculeService = vehiculeService;
            _logger = logger;
        }

        // GET: /Employee/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Récupérer l'ID de l'employé connecté
                var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (employeeIdClaim == null || !int.TryParse(employeeIdClaim.Value, out int employeeId))
                {
                    TempData["Error"] = "Employé non identifié.";
                    return RedirectToAction("Login", "Account");
                }

                // Récupérer les informations de l'employé
                var employee = await _employeeService.GetEmployeeByIdAsync(employeeId);
                if (employee == null)
                {
                    TempData["Error"] = "Employé non trouvé.";
                    return RedirectToAction("Login", "Account");
                }

                // Récupérer les données pour le dashboard
                var pendingLocations = await _locationService.GetPendingLocationsAsync();
                var todayLocations = await _locationService.GetTodayLocationsAsync();

                ViewBag.Employee = employee;
                ViewBag.PendingLocations = pendingLocations;
                ViewBag.TodayLocations = todayLocations;
                ViewBag.TotalPending = pendingLocations.Count();
                ViewBag.TotalToday = todayLocations.Count();

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement du dashboard employé");
                TempData["Error"] = "Erreur lors du chargement du dashboard.";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /Employee/Profile
        public async Task<IActionResult> Profile()
        {
            try
            {
                var employeeIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (employeeIdClaim == null || !int.TryParse(employeeIdClaim.Value, out int employeeId))
                {
                    TempData["Error"] = "Employé non identifié.";
                    return RedirectToAction("Login", "Account");
                }

                var employee = await _employeeService.GetEmployeeByIdAsync(employeeId);
                if (employee == null)
                {
                    TempData["Error"] = "Employé non trouvé.";
                    return RedirectToAction("Dashboard");
                }

                return View(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement du profil");
                TempData["Error"] = "Erreur lors du chargement du profil.";
                return RedirectToAction("Dashboard");
            }
        }

        // POST: /Employee/ConfirmLocation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmLocation(int id)
        {
            try
            {
                await _locationService.ConfirmLocationAsync(id);
                TempData["Success"] = "Location confirmée avec succès.";
                _logger.LogInformation("Location #{LocationId} confirmée par l'employé {EmployeeId}",
                    id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la confirmation de la location #{Id}", id);
                TempData["Error"] = "Erreur lors de la confirmation.";
            }

            return RedirectToAction("Dashboard");
        }

        // POST: /Employee/RejectLocation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectLocation(int id)
        {
            try
            {
                await _locationService.CancelLocationAsync(id);
                TempData["Success"] = "Location rejetée.";
                _logger.LogInformation("Location #{LocationId} rejetée par l'employé {EmployeeId}",
                    id, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du rejet de la location #{Id}", id);
                TempData["Error"] = "Erreur lors du rejet.";
            }

            return RedirectToAction("Dashboard");
        }

        // POST: /Employee/StartLocation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartLocation(int id)
        {
            try
            {
                await _locationService.StartLocationAsync(id);
                TempData["Success"] = "Location démarrée.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du démarrage de la location #{Id}", id);
                TempData["Error"] = "Erreur lors du démarrage.";
            }

            return RedirectToAction("Dashboard");
        }

        // POST: /Employee/CompleteLocation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteLocation(int id)
        {
            try
            {
                var montant = await _locationService.CalculateLocationCostAsync(id);
                await _locationService.CompleteLocationAsync(id);
                TempData["Success"] = $"Location terminée. Montant : {montant:C}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la finalisation de la location #{Id}", id);
                TempData["Error"] = "Erreur lors de la finalisation.";
            }

            return RedirectToAction("Dashboard");
        }
    }
}