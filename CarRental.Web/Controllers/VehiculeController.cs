using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CarRental.Data;
using CarRental.Core.Models;
using CarRental.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace CarRental.Web.Controllers
{
    [Authorize]
    public class VehiculeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IVehiculeService _vehiculeService;
        private readonly ILogger<VehiculeController> _logger;

        public VehiculeController(
            ApplicationDbContext context,
            IVehiculeService vehiculeService,
            ILogger<VehiculeController> logger)
        {
            _context = context;
            _vehiculeService = vehiculeService;
            _logger = logger;
        }

        // GET: Vehicule - Accessible par tous les utilisateurs authentifiés
        public async Task<IActionResult> Index()
        {
            try
            {
                _logger.LogInformation("Accès à la liste des véhicules par {User}", User.Identity?.Name);
                var vehicules = await _vehiculeService.GetAllVehiculesAsync();

                // Pour les clients, ne montrer que les véhicules disponibles
                if (User.IsInRole("Client"))
                {
                    vehicules = vehicules.Where(v => v.Statut == "Disponible" && v.EstActif).ToList();
                }

                return View(vehicules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des véhicules");
                TempData["Error"] = "Impossible de récupérer la liste des véhicules.";
                return View(new List<Vehicule>());
            }
        }

        // GET: Vehicule/Details/5 - Accessible par tous les utilisateurs authentifiés
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                _logger.LogInformation("Accès aux détails du véhicule {Id} par {User}", id, User.Identity?.Name);
                var vehicule = await _vehiculeService.GetVehiculeByIdAsync(id.Value);
                if (vehicule == null) return NotFound();

                // Pour les clients, vérifier si le véhicule est disponible
                if (User.IsInRole("Client") && (vehicule.Statut != "Disponible" || !vehicule.EstActif))
                {
                    TempData["Error"] = "Ce véhicule n'est pas disponible pour le moment.";
                    return RedirectToAction(nameof(Index));
                }

                return View(vehicule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du véhicule {Id}", id);
                return NotFound();
            }
        }

        // GET: Vehicule/Create - Accessible par Admin et Employe
        [Authorize(Roles = "admin,employe")]
        public async Task<IActionResult> Create()
        {
            try
            {
                _logger.LogInformation("Accès au formulaire de création de véhicule par {User}", User.Identity?.Name);
                await LoadCategoriesViewData();

                var model = new Vehicule
                {
                    Marque = string.Empty,
                    Modele = string.Empty,
                    Immatriculation = string.Empty,
                    EstActif = true,
                    Statut = "Disponible",
                    PrixParJour = 50.00m,
                    NombrePlaces = 5,
                    Kilometrage = 0
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement du formulaire de création");
                TempData["Error"] = "Erreur lors du chargement du formulaire.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Vehicule/Create - Accessible par Admin et Employe
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,employe")]
        public async Task<IActionResult> Create(Vehicule vehicule,
            string? NewCategoryName,
            string? NewCategoryDescription)
        {
            try
            {
                _logger.LogInformation("Tentative de création de véhicule par {User}", User.Identity?.Name);
                ModelState.Remove("Categorie");

                // Créer une nouvelle catégorie si demandée
                if (!string.IsNullOrWhiteSpace(NewCategoryName))
                {
                    var newCategory = new CategorieVehicule
                    {
                        Nom = NewCategoryName.Trim(),
                        Description = NewCategoryDescription?.Trim()
                    };

                    _context.CategoriesVehicule.Add(newCategory);
                    await _context.SaveChangesAsync();

                    vehicule.CategorieId = newCategory.Id;
                    _logger.LogInformation("Nouvelle catégorie créée: {Id} - {Nom}",
                        newCategory.Id, newCategory.Nom);

                    TempData["Info"] = $"Catégorie '{newCategory.Nom}' créée avec succès.";
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogError("❌ VALIDATION ÉCHOUÉE - Retour au formulaire");
                    foreach (var error in ModelState)
                    {
                        if (error.Value?.Errors.Count > 0)
                        {
                            _logger.LogError("Erreur sur {Key}: {Errors}",
                                error.Key,
                                string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage)));
                        }
                    }

                    await LoadCategoriesViewData();
                    return View(vehicule);
                }

                // Initialiser les valeurs par défaut
                vehicule.EstActif = true;
                vehicule.Statut = "Disponible";

                if (vehicule.PrixParJour <= 0)
                    vehicule.PrixParJour = 50.00m; // Valeur par défaut

                if (vehicule.NombrePlaces <= 0)
                    vehicule.NombrePlaces = 5;

                // Utiliser le service pour créer le véhicule
                var createdVehicule = await _vehiculeService.CreateVehiculeAsync(vehicule);

                _logger.LogInformation("✅ VEHICULE CRÉÉ par {User} - ID: {Id}, Immatriculation: {Immat}, Prix/Jour: {Prix}",
                    User.Identity?.Name, createdVehicule.Id, createdVehicule.Immatriculation, createdVehicule.PrixParJour);

                TempData["Success"] = $"Véhicule {createdVehicule.Marque} {createdVehicule.Modele} créé avec succès.";

                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                // Immatriculation existe déjà
                _logger.LogWarning(ex, "Tentative de création avec immatriculation existante");
                ModelState.AddModelError("Immatriculation", ex.Message);
                TempData["Error"] = ex.Message;
                await LoadCategoriesViewData();
                return View(vehicule);
            }
            catch (ArgumentException ex)
            {
                // Erreurs de validation
                _logger.LogWarning(ex, "Erreur de validation lors de la création");
                ModelState.AddModelError(string.Empty, ex.Message);
                TempData["Error"] = ex.Message;
                await LoadCategoriesViewData();
                return View(vehicule);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "❌ ERREUR BASE DE DONNÉES");
                _logger.LogError("Message: {Message}", dbEx.Message);
                _logger.LogError("InnerException: {Inner}", dbEx.InnerException?.Message ?? "Aucune");

                var errorDetails = dbEx.InnerException?.Message ?? dbEx.Message;

                if (errorDetails.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
                    errorDetails.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("Immatriculation", "Cette immatriculation existe déjà.");
                    TempData["Error"] = "L'immatriculation est déjà utilisée.";
                }
                else if (errorDetails.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Error"] = "Catégorie invalide. Veuillez sélectionner une catégorie valide.";
                }
                else
                {
                    TempData["Error"] = "Erreur lors de l'enregistrement dans la base de données.";
                }

                await LoadCategoriesViewData();
                return View(vehicule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur inattendue lors de la création");
                TempData["Error"] = "Une erreur inattendue s'est produite.";
                await LoadCategoriesViewData();
                return View(vehicule);
            }
        }

        // GET: Vehicule/Edit/5 - Accessible par Admin et Employe
        [Authorize(Roles = "admin,employe")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                _logger.LogInformation("Accès à l'édition du véhicule {Id} par {User}", id, User.Identity?.Name);
                var vehicule = await _vehiculeService.GetVehiculeByIdAsync(id.Value);
                if (vehicule == null) return NotFound();

                await LoadCategoriesViewData();
                return View(vehicule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du véhicule {Id}", id);
                return NotFound();
            }
        }

        // POST: Vehicule/Edit/5 - Accessible par Admin et Employe
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,employe")]
        public async Task<IActionResult> Edit(int id, Vehicule vehicule,
            string? NewCategoryName,
            string? NewCategoryDescription)
        {
            if (id != vehicule.Id)
                return NotFound();

            try
            {
                _logger.LogInformation("Tentative de modification du véhicule {Id} par {User}", id, User.Identity?.Name);
                ModelState.Remove("Categorie");

                // Créer une nouvelle catégorie si demandée
                if (!string.IsNullOrWhiteSpace(NewCategoryName))
                {
                    var newCategory = new CategorieVehicule
                    {
                        Nom = NewCategoryName.Trim(),
                        Description = NewCategoryDescription?.Trim()
                    };

                    _context.CategoriesVehicule.Add(newCategory);
                    await _context.SaveChangesAsync();

                    vehicule.CategorieId = newCategory.Id;
                    _logger.LogInformation("Nouvelle catégorie créée lors de l'édition: {Id} - {Nom}",
                        newCategory.Id, newCategory.Nom);

                    TempData["Info"] = $"Catégorie '{newCategory.Nom}' créée avec succès.";
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogError("Validation échouée pour le véhicule {Id}", id);
                    await LoadCategoriesViewData();
                    return View(vehicule);
                }

                // Récupérer l'ancien véhicule pour conserver certaines valeurs
                var oldVehicule = await _vehiculeService.GetVehiculeByIdAsync(id);
                if (oldVehicule != null)
                {
                    // Conserver les dates si non modifiées
                    if (vehicule.DateAchat == null && oldVehicule.DateAchat != null)
                        vehicule.DateAchat = oldVehicule.DateAchat;

                    if (vehicule.DateDernierEntretien == null && oldVehicule.DateDernierEntretien != null)
                        vehicule.DateDernierEntretien = oldVehicule.DateDernierEntretien;
                }

                // Utiliser le service pour mettre à jour le véhicule
                await _vehiculeService.UpdateVehiculeAsync(vehicule);

                _logger.LogInformation("Véhicule modifié: {Id} par {User} - Nouveau Prix/Jour: {Prix}",
                    id, User.Identity?.Name, vehicule.PrixParJour);

                TempData["Success"] = "Véhicule modifié avec succès.";
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Véhicule non trouvé: {Id}", id);
                TempData["Error"] = ex.Message;
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Immatriculation déjà utilisée");
                ModelState.AddModelError("Immatriculation", ex.Message);
                TempData["Error"] = ex.Message;
                await LoadCategoriesViewData();
                return View(vehicule);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Erreur de validation lors de la modification");
                ModelState.AddModelError(string.Empty, ex.Message);
                TempData["Error"] = ex.Message;
                await LoadCategoriesViewData();
                return View(vehicule);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Erreur base de données lors de la modification");
                var errorDetails = dbEx.InnerException?.Message ?? dbEx.Message;

                if (errorDetails.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
                    errorDetails.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError("Immatriculation", "Cette immatriculation existe déjà.");
                    TempData["Error"] = "L'immatriculation est déjà utilisée.";
                }
                else
                {
                    TempData["Error"] = "Erreur lors de l'enregistrement dans la base de données.";
                }

                await LoadCategoriesViewData();
                return View(vehicule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la modification du véhicule {Id}", id);
                TempData["Error"] = "Erreur lors de la modification.";
                await LoadCategoriesViewData();
                return View(vehicule);
            }
        }

        // GET: Vehicule/Delete/5 - Accessible uniquement par Admin
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            try
            {
                _logger.LogInformation("Accès à la suppression du véhicule {Id} par {User}", id, User.Identity?.Name);
                var vehicule = await _vehiculeService.GetVehiculeByIdAsync(id.Value);
                if (vehicule == null) return NotFound();

                // Vérifier si le véhicule a des locations en cours
                var hasActiveLocations = await _context.Locations
                    .AnyAsync(l => l.VehiculeId == id &&
                                  (l.Statut == "Confirmée" || l.Statut == "En attente" || l.Statut == "En cours"));

                if (hasActiveLocations)
                {
                    TempData["Error"] = "Impossible de supprimer ce véhicule car il a des locations en cours ou en attente.";
                    return RedirectToAction(nameof(Index));
                }

                return View(vehicule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du véhicule {Id}", id);
                return NotFound();
            }
        }

        // POST: Vehicule/Delete/5 - Accessible uniquement par Admin
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                _logger.LogInformation("Suppression du véhicule {Id} par {User}", id, User.Identity?.Name);

                // Vérifier si le véhicule a des locations en cours
                var hasActiveLocations = await _context.Locations
                    .AnyAsync(l => l.VehiculeId == id &&
                                  (l.Statut == "Confirmée" || l.Statut == "En attente" || l.Statut == "En cours"));

                if (hasActiveLocations)
                {
                    TempData["Error"] = "Impossible de supprimer ce véhicule car il a des locations en cours ou en attente.";
                    return RedirectToAction(nameof(Index));
                }

                await _vehiculeService.DeleteVehiculeAsync(id);

                _logger.LogInformation("Véhicule supprimé: {Id}", id);
                TempData["Success"] = "Véhicule supprimé avec succès.";
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Véhicule {Id} n'existe plus", id);
                TempData["Warning"] = "Le véhicule n'existe plus.";
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Impossible de supprimer le véhicule {Id}: {Message}", id, ex.Message);
                TempData["Error"] = ex.Message;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Erreur base de données lors de la suppression");

                var errorDetails = dbEx.InnerException?.Message ?? dbEx.Message;
                if (errorDetails.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Error"] = "Impossible de supprimer ce véhicule car il est référencé dans des locations.";
                }
                else
                {
                    TempData["Error"] = "Erreur lors de la suppression dans la base de données.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du véhicule {Id}", id);
                TempData["Error"] = "Erreur lors de la suppression.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ========== ACTIONS SUPPLÉMENTAIRES ==========

        // POST: Vehicule/ToggleDisponibility/5 - Accessible par Admin et Employe
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin,employe")]
        public async Task<IActionResult> ToggleDisponibility(int id)
        {
            try
            {
                var vehicule = await _vehiculeService.GetVehiculeByIdAsync(id);
                if (vehicule == null)
                {
                    TempData["Error"] = "Véhicule non trouvé.";
                    return RedirectToAction(nameof(Index));
                }

                vehicule.Statut = vehicule.Statut == "Disponible" ? "Indisponible" : "Disponible";
                await _vehiculeService.UpdateVehiculeAsync(vehicule);

                var status = vehicule.Statut;
                _logger.LogInformation("Disponibilité du véhicule {Id} modifiée: {Status}", id, status);
                TempData["Success"] = $"Véhicule marqué comme {status.ToLower()}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du changement de disponibilité du véhicule {Id}", id);
                TempData["Error"] = "Erreur lors de la modification de la disponibilité.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Vehicule/ToggleActif/5 - Accessible par Admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ToggleActif(int id)
        {
            try
            {
                var vehicule = await _vehiculeService.GetVehiculeByIdAsync(id);
                if (vehicule == null)
                {
                    TempData["Error"] = "Véhicule non trouvé.";
                    return RedirectToAction(nameof(Index));
                }

                vehicule.EstActif = !vehicule.EstActif;

                // Si le véhicule est désactivé, il devient automatiquement indisponible
                if (!vehicule.EstActif)
                {
                    vehicule.Statut = "Indisponible";
                }

                await _vehiculeService.UpdateVehiculeAsync(vehicule);

                var status = vehicule.EstActif ? "activé" : "désactivé";
                _logger.LogInformation("Statut du véhicule {Id} modifié: {Status}", id, status);
                TempData["Success"] = $"Véhicule {status}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du changement de statut du véhicule {Id}", id);
                TempData["Error"] = "Erreur lors de la modification du statut.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ========== MÉTHODES PRIVÉES ==========

        // Méthode pour charger les catégories
        private async Task LoadCategoriesViewData()
        {
            try
            {
                var categories = await _context.CategoriesVehicule
                    .OrderBy(c => c.Nom)
                    .ToListAsync();

                if (!categories.Any())
                {
                    TempData["Warning"] = "Aucune catégorie disponible. Vous pouvez en créer une nouvelle directement depuis ce formulaire.";
                }

                ViewData["CategorieId"] = new SelectList(categories, "Id", "Nom");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des catégories");
                ViewData["CategorieId"] = new SelectList(new List<CategorieVehicule>(), "Id", "Nom");
            }
        }
    }
}