using CentralSecurityService.Common.Configuration;
using CentralSecurityService.Common.DataAccess.CentralSecurityService.Databases;
using CentralSecurityService.Common.DataAccess.CentralSecurityService.Repositories;
using CentralSecurityServiceAdmin.Configuration;
using CentralSecurityServiceAdmin.Sessions;
using Eadent.Identity.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog;

namespace CentralSecurityServiceAdmin
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure Serilog using appsettings.json
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            builder.Host.UseSerilog();            // Add services to the container.

            var services = builder.Services;

            services.AddDistributedMemoryCache();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(20);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            services.AddHttpContextAccessor();

            services.AddTransient<IUserSession, UserSession>();

            // Add services to the container.
            services.AddRazorPages();

            builder.Configuration.GetSection(CentralSecurityServiceAdminSettings.SectionName).Get<CentralSecurityServiceAdminSettings>();

            builder.Configuration.AddJsonFile(Path.Combine(CentralSecurityServiceAdminSettings.Instance.Sensitive.Folder, "CentralSecurityServiceCommon.settings.json"), optional: false, reloadOnChange: false);
            builder.Configuration.AddJsonFile(Path.Combine(CentralSecurityServiceAdminSettings.Instance.Sensitive.Folder, "CentralSecurityServiceAdminSensitive.settings.json"), optional: false, reloadOnChange: false);
            builder.Configuration.AddJsonFile(Path.Combine(CentralSecurityServiceAdminSettings.Instance.Sensitive.Folder, "Eadent.Identity.settings.json"), optional: false, reloadOnChange: false);

            builder.Configuration.GetSection(CentralSecurityServiceCommonSettings.SectionName).Get<CentralSecurityServiceCommonSettings>();
            builder.Configuration.GetSection(CentralSecurityServiceAdminSensitiveSettings.SectionName).Get<CentralSecurityServiceAdminSensitiveSettings>();
            builder.Configuration.GetSection(EadentIdentitySettings.SectionName).Get<EadentIdentitySettings>();

            Eadent.Identity.Startup.ConfigureServices(services);

            // TODO: Remove the following lines.
            var commonSettings = CentralSecurityServiceCommonSettings.Instance;
            var sensitiveSettings = CentralSecurityServiceAdminSensitiveSettings.Instance;
            var eadentIdentitySettings = EadentIdentitySettings.Instance;

            var connectionString = CentralSecurityServiceCommonSettings.Instance.Database.ConnectionString;

            services.AddDbContext<CentralSecurityServiceDatabase>(options => options.UseSqlServer(connectionString));

            services.AddScoped<ICentralSecurityServiceDatabase, CentralSecurityServiceDatabase>();

            services.AddTransient<IReferencesRepository, ReferencesRepository>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseSession();

            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            app.Run();
        }
    }
}
