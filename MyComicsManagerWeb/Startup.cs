using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MyComicsManagerWeb.Services;
using MyComicsManagerWeb.Models;
using Serilog;
using ImageThumbnail.AspNetCore.Middleware;
using MudBlazor.Services;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using MudBlazor;
using MyComicsManagerWeb.Middleware.ImageThumbnail;
using MyComicsManagerWeb.Settings;

namespace MyComicsManagerWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            
            services.Configure<DatabaseSettings>( Configuration.GetSection(nameof(DatabaseSettings)));
            services.AddSingleton<IDatabaseSettings>(sp => sp.GetRequiredService<IOptions<DatabaseSettings>>().Value);
            var mongoDbSettings  = Configuration.GetSection(nameof(DatabaseSettings)).Get<DatabaseSettings>();
            
            services.Configure<WebserviceSettings>( Configuration.GetSection(nameof(WebserviceSettings)));
            services.AddSingleton<IWebserviceSettings>(sp => sp.GetRequiredService<IOptions<WebserviceSettings>>().Value);
            
            services.AddIdentity<ApplicationUser, ApplicationRole>().AddMongoDbStores<ApplicationUser, ApplicationRole, Guid>
            (
                mongoDbSettings.ConnectionString, mongoDbSettings.Name
            ).AddDefaultTokenProviders();           
            
            services.AddMvc(options => options.EnableEndpointRouting = false);
            
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddControllersWithViews();

            // Service Configuration
            services.AddHttpClient<BookService>();
            services.AddHttpClient<ComicService>();
            services.AddHttpClient<LibraryService>();
            services.AddHttpClient<BookInformationService>();
            
            services.AddSingleton<ThumbnailService>();
            
            // MudBlazor Config
            services.AddMudServices(config =>
            {
                config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopRight;
                config.SnackbarConfiguration.PreventDuplicates = false;
                config.SnackbarConfiguration.NewestOnTop = false;
                config.SnackbarConfiguration.ShowCloseIcon = true;
                config.SnackbarConfiguration.VisibleStateDuration = 10000;
                config.SnackbarConfiguration.HideTransitionDuration = 500;
                config.SnackbarConfiguration.ShowTransitionDuration = 500;
                config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
            });

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IWebserviceSettings settings)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseStaticFiles();

            // Création du répertoire des covers si il n'existe pas
            if (settings?.CoversDirRootPath != null)
            {
                Directory.CreateDirectory(settings.CoversDirRootPath);
            }
            else
            {
                if (settings == null)
                {
                    Log.Fatal("settings is null !");
                    return;
                }
                else if (settings.CoversDirRootPath == null)
                {
                    Log.Fatal("settings.CoversDirRootPath is null !");
                    return;
                }
            }
            
            // Ajout du module Middleware pour gérer les thumbnails à la volée
            // TODO Github#117 : Thumbs et covers doivent être gérés dans le fichier de configuration
            var options = new ImageThumbnailOptions("covers", "thumbs");            
            app.UseImageThumbnail(settings.CoversDirRootPath, options);
            
            // Set up custom content types -associating file extension to MIME type and Add new mappings
            // Source : https://docs.microsoft.com/en-us/aspnet/core/fundamentals/static-files?view=aspnetcore-5.0#fileextensioncontenttypeprovider
            var provider = new FileExtensionContentTypeProvider
            {
                Mappings =
                {
                    [".cbz"] = "application/zip",
                    // Ajout de CBR pour permettre de télécharger les fichiers en erreur
                    [".cbr"] = "application/zip"
                }
            };

            // Permettre de télécharger les fichiers en mode public
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(settings.LibrariesDirRootPath)),
                RequestPath = new PathString("/download"),
                ContentTypeProvider = provider
            });

            app.UseRouting();
            app.UseMvcWithDefaultRoute();
            
            app.UseSerilogRequestLogging();
            
            app.UseAuthentication();
            app.UseAuthorization();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
