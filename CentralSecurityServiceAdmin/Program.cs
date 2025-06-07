using CentralSecurityService.Common.Configuration;
using CentralSecurityServiceAdmin.Configuration;
using CentralSecurityServiceAdmin.Sessions;
using Eadent.Identity.Configuration;
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

            builder.Configuration.AddJsonFile(Path.Combine(CentralSecurityServiceAdminSettings.Instance.Sensitive.Folder, "CentralSecurityServiceAdmin.settings.json"), optional: false, reloadOnChange: false);
            builder.Configuration.AddJsonFile(Path.Combine(CentralSecurityServiceAdminSettings.Instance.Sensitive.Folder, "Eadent.Identity.settings.json"), optional: false, reloadOnChange: false);

            builder.Configuration.GetSection(CentralSecurityServiceCommonSettings.SectionName).Get<CentralSecurityServiceCommonSettings>();
            builder.Configuration.GetSection(EadentIdentitySettings.SectionName).Get<EadentIdentitySettings>();

            Eadent.Identity.Startup.ConfigureServices(services);

            var eadentIdentitySettings = EadentIdentitySettings.Instance;

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
