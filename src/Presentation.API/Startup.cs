namespace Presentation.API
{
    using Application.Services;
    using Application.ServicesInterfaces;
    using Domain.Core;
    using Infrastructure.Gateways;
    using Infrastructure.Repositories;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.OpenApi.Models;
    using MongoDB.Driver;
    using Presentation.API.Middleware;
    using Presentation.API.Settings;
    using System.Text;

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
            //Add data storage settings
            var dbConnectionString = Configuration.GetSection("Database:ConnectionString").Value;
            var dbName = Configuration.GetSection("Database:Name").Value;
            var client = new MongoClient(dbConnectionString);

            services.AddSingleton<IMongoClient>(client);
            services.AddSingleton(client.GetDatabase(dbName));

            //Add authentication settings
            services.Configure<AuthenticationSettings>(Configuration.GetSection("Authentication"));

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
               .AddJwtBearer(options =>
               {
                   options.TokenValidationParameters = new TokenValidationParameters
                   {
                       ValidateIssuer = true,
                       ValidateAudience = true,
                       ValidateLifetime = true,
                       ValidateIssuerSigningKey = true,
                       ValidIssuer = Configuration["Authentication:JwtIssuer"],
                       ValidAudience = Configuration["Authentication:JwtAudience"],
                       IssuerSigningKey = new SymmetricSecurityKey(
                           Encoding.UTF8.GetBytes(Configuration["Authentication:JwtKey"]))
                   };
               });

            //Add payments services
            services.AddScoped<IAuthorizationService, AuthorizationService>();
            services.AddScoped<IVoidService, VoidService>();
            services.AddScoped<ICaptureService, CaptureService>();
            services.AddScoped<IRefundService, RefundService>();

            //Add data repositories
            services.AddScoped<IPaymentRepository, PaymentRepository>();

            //Add data gateway
            services.AddSingleton<IBankGateway, BankGateway>();

            services.AddLogging();

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Payments.API", Version = "v1" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Insert valid JWT Bearer",
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] { }
                    }
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payments.API v1"));
            }

            app.UseExceptionMiddleware();
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
