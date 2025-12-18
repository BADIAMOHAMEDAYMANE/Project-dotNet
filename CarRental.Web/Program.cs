using CarRental.Core.Interfaces;
using CarRental.Core.Services;
using CarRental.Data.Repositories;
using CarRental.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text.Json.Serialization;

// Créer le dossier logs s'il n'existe pas
Directory.CreateDirectory("logs");

// Configuration de Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/carrental-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}",
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10485760)
    .CreateLogger();

try
{
    Log.Information("=== Démarrage de l'application CarRental ===");

    var builder = WebApplication.CreateBuilder(args);

    // Utiliser Serilog comme provider de logging
    builder.Host.UseSerilog();

    // Services MVC avec configuration JSON
    builder.Services.AddControllersWithViews()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

    // Configuration de la base de données
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrEmpty(connectionString))
    {
        Log.Warning("Aucune chaîne de connexion trouvée dans la configuration");
        if (builder.Environment.IsDevelopment())
        {
            connectionString = "server=localhost;database=carrental;user=root;password=;";
            Log.Information("Utilisation de la chaîne de connexion par défaut pour le développement");
        }
    }
    else
    {
        Log.Information("Configuration de la connexion à MySQL");
    }

    if (!string.IsNullOrEmpty(connectionString))
    {
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString),
                mySqlOptions =>
                {
                    mySqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                })
            .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
            .EnableDetailedErrors(builder.Environment.IsDevelopment()));
    }
    else
    {
        Log.Error("Aucune chaîne de connexion disponible. L'application ne peut pas démarrer.");
        throw new InvalidOperationException("La chaîne de connexion à la base de données est manquante.");
    }

    // ============================================
    // CONFIGURATION DE L'AUTHENTIFICATION
    // ============================================
    Log.Information("Configuration de l'authentification par cookies");

    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/ClientAccount/Login";
            options.LogoutPath = "/ClientAccount/Logout";
            options.AccessDeniedPath = "/ClientAccount/AccessDenied";
            options.Cookie.Name = "CarRental.Auth";
            options.ExpireTimeSpan = TimeSpan.FromHours(24);
            options.SlidingExpiration = true;
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Lax;

            options.Events = new CookieAuthenticationEvents
            {
                OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = 401;
                    }
                    else
                    {
                        context.Response.Redirect(context.RedirectUri);
                    }
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin", "Admin"));
        options.AddPolicy("ClientOnly", policy => policy.RequireRole("client", "Client"));
        options.AddPolicy("Authenticated", policy => policy.RequireAuthenticatedUser());
    });

    // ============================================
    // ENREGISTREMENT DES REPOSITORIES
    // ============================================
    Log.Information("Enregistrement des repositories");

    builder.Services.AddScoped<IClientRepository, ClientRepository>();
    builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
    builder.Services.AddScoped<IVehiculeRepository, VehiculeRepository>();
    builder.Services.AddScoped<ILocationRepository, LocationRepository>();
    builder.Services.AddScoped<IAdminRepository, AdminRepository>();
    builder.Services.AddScoped<IFactureRepository, FactureRepository>(); // ✅ REPOSITORY FACTURE

    // ============================================
    // ENREGISTREMENT DES SERVICES MÉTIER
    // ============================================
    Log.Information("Enregistrement des services métier");

    builder.Services.AddScoped<IClientService, ClientService>();
    builder.Services.AddScoped<IClientAuthService, ClientAuthService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IVehiculeService, VehiculeService>();
    builder.Services.AddScoped<ILocationService, LocationService>();
    builder.Services.AddScoped<IEmployeeService, EmployeeService>();
    builder.Services.AddScoped<IAdminService, AdminService>();
    builder.Services.AddScoped<IFactureService, FactureService>(); // ✅ SERVICE FACTURE - CRUCIAL !

    Log.Information("✅ Service IFactureService enregistré avec succès");

    // Configuration de la session
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.Name = "CarRental.Session";
    });

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "CarRental API",
            Version = "v1",
            Description = "API pour le système de location de voitures",
            Contact = new OpenApiContact
            {
                Name = "CarRental Support",
                Email = "support@carrental.com"
            }
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer"
        });
    });

    // Configuration CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll",
            builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
    });

    var app = builder.Build();

    // Logging du middleware
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestAgent", httpContext.Request.Headers.UserAgent.ToString());

            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                diagnosticContext.Set("User", httpContext.User.Identity.Name);
                diagnosticContext.Set("UserId", httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
            }
        };
    });

    // Configuration de l'environnement
    if (app.Environment.IsDevelopment())
    {
        Log.Information("Mode développement activé");
        app.UseDeveloperExceptionPage();
    }
    else
    {
        Log.Information("Mode production activé");
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    /*app.UseHttpsRedirection();*/
    app.UseStaticFiles();
    app.UseRouting();
    app.UseSession();
    app.UseCors("AllowAll");

    // IMPORTANT: L'ORDRE EST CRUCIAL !
    app.UseAuthentication();
    app.UseAuthorization();

    // ============================================
    // CONFIGURATION DES ROUTES
    // ============================================

    app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "clientAccount",
        pattern: "ClientAccount/{action=Login}/{id?}",
        defaults: new { controller = "ClientAccount" });

    app.MapControllerRoute(
        name: "clientDetails",
        pattern: "Client/Details/{id}",
        defaults: new { controller = "Client", action = "Details" });

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.MapControllers();

    // Swagger
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "CarRental API v1");
            c.RoutePrefix = "swagger";
            c.DocumentTitle = "CarRental API Documentation";
            c.EnableDeepLinking();
            c.DisplayOperationId();
        });
        Log.Information("Swagger disponible sur /swagger");
    }

    // Test de la connexion à la base de données
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();

            if (app.Environment.IsDevelopment())
            {
                Log.Information("Application des migrations de base de données...");
                await dbContext.Database.MigrateAsync();
                Log.Information("✓ Migrations appliquées avec succès");
            }

            var canConnect = await dbContext.Database.CanConnectAsync();
            if (canConnect)
            {
                Log.Information("✓ Connexion à la base de données réussie");
            }
            else
            {
                Log.Warning("⚠ Impossible de se connecter à la base de données");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "✗ Erreur lors de l'initialisation de la base de données");
            if (!app.Environment.IsDevelopment())
            {
                throw;
            }
        }
    }

    // ✅ VÉRIFICATION DES SERVICES CRITIQUES (AJOUT DU SERVICE FACTURE)
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var authService = services.GetService<IClientAuthService>();
            if (authService == null)
            {
                Log.Error("✗ Le service IClientAuthService n'est pas enregistré !");
            }
            else
            {
                Log.Information("✓ Service IClientAuthService correctement enregistré");
            }

            var clientRepo = services.GetService<IClientRepository>();
            if (clientRepo == null)
            {
                Log.Error("✗ Le service IClientRepository n'est pas enregistré !");
            }
            else
            {
                Log.Information("✓ Service IClientRepository correctement enregistré");
            }

            var clientService = services.GetService<IClientService>();
            if (clientService == null)
            {
                Log.Error("✗ Le service IClientService n'est pas enregistré !");
            }
            else
            {
                Log.Information("✓ Service IClientService correctement enregistré");
            }

            // ✅ VÉRIFICATION DU SERVICE FACTURE
            var factureService = services.GetService<IFactureService>();
            if (factureService == null)
            {
                Log.Error("✗ Le service IFactureService n'est pas enregistré !");
            }
            else
            {
                Log.Information("✓ Service IFactureService correctement enregistré");
            }

            var factureRepo = services.GetService<IFactureRepository>();
            if (factureRepo == null)
            {
                Log.Error("✗ Le service IFactureRepository n'est pas enregistré !");
            }
            else
            {
                Log.Information("✓ Service IFactureRepository correctement enregistré");
            }

            var locationService = services.GetService<ILocationService>();
            if (locationService == null)
            {
                Log.Error("✗ Le service ILocationService n'est pas enregistré !");
            }
            else
            {
                Log.Information("✓ Service ILocationService correctement enregistré");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erreur lors de la vérification des services");
        }
    }

    Log.Information("✓ Application démarrée avec succès");
    Log.Information("📍 URLs disponibles: {Urls}", string.Join(", ", app.Urls));
    Log.Information("🔐 Page de login client: /ClientAccount/Login");
    Log.Information("👤 Page de profil client: /Client/Details/{{CIN}}");
    Log.Information("📚 Swagger UI: /swagger (développement uniquement)");
    Log.Information("💰 Service de facturation: ACTIVÉ");
    Log.Information("⌨️  Appuyez sur Ctrl+C pour arrêter l'application");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "L'application a planté au démarrage");
    throw;
}
finally
{
    Log.Information("=== Arrêt de l'application CarRental ===");
    Log.CloseAndFlush();
}