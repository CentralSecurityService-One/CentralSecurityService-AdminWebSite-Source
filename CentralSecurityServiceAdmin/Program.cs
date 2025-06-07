using CentralSecurityService.Common.Configuration;
using CentralSecurityServiceAdmin.Configuration;
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
                .CreateLogger();

            builder.Host.UseSerilog();            // Add services to the container.

            var services = builder.Services;

            // Add services to the container.
            services.AddRazorPages();

            builder.Configuration.GetSection(CentralSecurityServiceAdminSettings.SectionName).Get<CentralSecurityServiceAdminSettings>();

            builder.Configuration.AddJsonFile(Path.Combine(CentralSecurityServiceAdminSettings.Instance.Sensitive.Folder, "CentralSecurityServiceAdmin.settings.json"), optional: false, reloadOnChange: false);

            builder.Configuration.GetSection(CentralSecurityServiceCommonSettings.SectionName).Get<CentralSecurityServiceCommonSettings>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            app.Run();
        }
    }
}
