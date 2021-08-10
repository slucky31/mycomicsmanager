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
using Microsoft.Extensions.FileProviders;
using System.IO;

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
            
            services.Configure<WebserviceSettings>( Configuration.GetSection(nameof(WebserviceSettings)));
            services.AddSingleton<IWebserviceSettings>(sp => sp.GetRequiredService<IOptions<WebserviceSettings>>().Value);
            
            services.AddRazorPages();
            services.AddServerSideBlazor();
            services.AddHttpClient<ComicService>();
            services.AddHttpClient<LibraryService>();
            services.AddMudServices();

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

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Création du répertoire des covers si il n'existe pas
            Directory.CreateDirectory(settings.CoversDirRootPath);

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(settings.CoversDirRootPath),
                RequestPath = "/covers"
            });

            ImageThumbnailOptions options = new ImageThumbnailOptions("covers", "thumbs");            
            app.UseImageThumbnail(options);

            app.UseRouting();

            app.UseSerilogRequestLogging();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
