using ImageUploader.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace ImageUploader
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(opt => opt.UseSqlite(Configuration.GetConnectionString("Default")));
            services.Configure<Helpers.ImageSettings>(Configuration.GetSection("ImageSettings"));
            services.Configure<Helpers.FtpServerSettings>(Configuration.GetSection("FtpServerSettings"));
            services.Configure<Helpers.FoldersSettings>(Configuration.GetSection("FoldersSettings"));
            services.Configure<Helpers.ApiSettings>(Configuration.GetSection("ApiSettings"));
            services.AddRazorPages();
            services.AddControllers();
            services.AddRouting(opt => opt.LowercaseUrls = true);
            services.AddHttpClient();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseStaticFiles();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });
        }
    }
}
