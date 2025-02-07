using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using ParkiAPI.Data;
using ParkiAPI.Repository.IRepository;
using ParkiAPI.ParkiMapper;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.Extensions.Options;
using ParkiAPI.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ParkiAPI
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
            services.AddCors(); //Adding cors service
            services.AddDbContext<ApplicationDbContext>
                (options => options.UseSqlServer(Configuration.GetConnectionString("natParkiConnection")));

            services.AddScoped<INationalParkRepository, NationalParkRepository>();
            services.AddScoped<ITrailRepository, TrailRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            /* ^ after this code you can access the national park repository on any of controllers
                Creates an instance for each incoming web request and 
                uses the same instance for each incoming request, 
                creates a new instance for different web requests. ~Burak */
            services.AddAutoMapper(typeof(ParkiMapping));
            services.AddApiVersioning(options => //for versioning and versioning options
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;

            });
            services.AddVersionedApiExplorer(options => options.GroupNameFormat = "'v'VVV");
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, SwaggerConfigOptions>();
            services.AddSwaggerGen();

            var appSettingsSection = Configuration.GetSection("AppSettings");
            services.Configure<AppSettings>(appSettingsSection);

            var appSettings = appSettingsSection.Get<AppSettings>();
            var key = Encoding.UTF8.GetBytes(appSettings.Secret);
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            });
           /*services.AddSwaggerGen(options => // not needed after versioning
            {
                options.SwaggerDoc
                     (
                        "natParkiOpenAPISpec",
                        new Microsoft.OpenApi.Models.OpenApiInfo()
                        {
                            Title = "natParki API",
                            Version = "1.0",
                            Description = "This rest API developed to understand Web API structure, SOLID principles and design patterns (repository, DTO etc.).",
                            Contact = new Microsoft.OpenApi.Models.OpenApiContact()
                            {
                                Email = "",
                                Name = "Burak G�ler",
                                Url = new Uri("https://www.linkedin.com/in/guler-burak/"),
                            },
                            License = new Microsoft.OpenApi.Models.OpenApiLicense()
                            {
                                Name = "MIT License",
                                Url = new Uri("https://en.wikipedia.org/wiki/MIT_License"),
                            },
                        }
                     );

                //options.SwaggerDoc // not needed after versioning
                //    (
                //       "natParkiOpenAPISpecTrails",
                //       new Microsoft.OpenApi.Models.OpenApiInfo()
                //       {
                //           Title = "natParki API - Trails",
                //           Version = "1.0",
                //           Description = "This rest API developed to understand Web API structure, SOLID principles and design patterns (repository, DTO etc.).",
                //           Contact = new Microsoft.OpenApi.Models.OpenApiContact()
                //           {
                //               Email = "",
                //               Name = "Burak G�ler",
                //               Url = new Uri("https://www.linkedin.com/in/guler-burak/"),
                //           },
                //           License = new Microsoft.OpenApi.Models.OpenApiLicense()
                //           {
                //               Name = "MIT License",
                //               Url = new Uri("https://en.wikipedia.org/wiki/MIT_License"),
                //           },
                //       }
                //    );

              
            }); */
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app, 
            IWebHostEnvironment env,
            IApiVersionDescriptionProvider avdprovider // for middleware
            )
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseSwagger();

            app.UseSwaggerUI(options => //
            {
                foreach (var descrition in avdprovider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint($"/swagger/{descrition.GroupName}/swagger.json",
                        descrition.GroupName.ToUpperInvariant());
                }
                options.RoutePrefix = "";
            });

            //app.UseSwaggerUI(options =>  //old swagger code
            //{
            //    options.SwaggerEndpoint("/swagger/natParkiOpenAPISpec/swagger.json", "natParki API");
            //    //options.SwaggerEndpoint("/swagger/natParkiOpenAPISpecTrails/swagger.json", "natParki API - Trails");

            //    
            //});

            app.UseRouting();
            app.UseCors(cors => cors //adding CORS
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
