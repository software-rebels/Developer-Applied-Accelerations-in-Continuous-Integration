using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ForecastBuildTime.MongoDBModels;
using ForecastBuildTime.SqlModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace ForecastBuildTime.WebApp
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
            services.AddControllersWithViews();

#if DEBUG
            services.AddTransient(_ =>
                new MongoClient("mongodb://forecastRead:+3o2;.cIU883%2Fz@localhost:27017/forecastBuildTime"));
#else
            services.AddTransient(_ =>
                new MongoClient("mongodb://forecastRead:+3o2;.cIU883%2Fz@forecast-mongo:27017/forecastBuildTime"));
#endif
            services.AddTransient(service =>
                service.GetRequiredService<MongoClient>()
                    .GetDatabase("forecastBuildTime")
                    .GetCollection<BuildEntry>("cc_builds_2021"));

            services.AddMemoryCache();
            services.AddDbContext<ForecastingContext>(builder =>
#if DEBUG
                builder.UseNpgsql(@"Server=localhost;Port=13339;Database=forecasting;User Id=forecasting;Password=r27sgJNKcdH-_SqT3nEWAMX}TyuNwWh>W8.RR.N7Gg7vjbGe{9PpKF8_xDfQ{b;", options => options.EnableRetryOnFailure()), ServiceLifetime.Transient);
#else
                builder.UseNpgsql(@"Server=forecast-pg;Port=13339;Database=forecasting;User Id=forecasting;Password=r27sgJNKcdH-_SqT3nEWAMX}TyuNwWh>W8.RR.N7Gg7vjbGe{9PpKF8_xDfQ{b;", options => options.EnableRetryOnFailure()), ServiceLifetime.Transient);
#endif
            //services.AddDbContextFactory<ForecastingContext>(builder =>
            //    builder.UseNpgsql(@"Server=forecast-pg;Port=13339;Database=forecasting;User Id=forecasting;Password=123", options => options.EnableRetryOnFailure()));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (!env.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
