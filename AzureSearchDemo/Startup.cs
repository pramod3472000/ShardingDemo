using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureSearchDemo.Helpers;
using AzureSearchDemo.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace AzureSearchDemo
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
            ICustomConfigurationSettings configSettings = Configuration.Get<CustomConfigurationSettings>();
            services.AddControllers();

            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen();

                        
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                //.Enrich.FromLogContext()
                .Enrich.WithProperty("Version", typeof(Program).Assembly.ImageRuntimeVersion)
                .CreateLogger();

            try
            {
                throw new Exception("Trying to throw an error deliberately");
            }
            catch (Exception ex)
            {

                Log.Logger.Error(ex, "Trying to log my first error message on azure app insights");
            }
            
            Log.Logger.Information("Logging my first message from Serilog");

            services.AddSingleton(Log.Logger);
            services.AddSingleton(configSettings);
            services.AddTransient<IAzureSearchService, AzureSearchService>();
            //services.AddTransient<ICustomConfigurationSettings, CustomConfigurationSettings>();
            //services.AddApplicationInsightsTelemetry(Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"]);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Add this line; you'll need `use Serilog;` in the program.cs
            //This is a middleware. It is used to log the Serilog internal/inbuilt logging. It logs the Controller methods 
            app.UseSerilogRequestLogging();

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Azure Search Demo V1");
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
