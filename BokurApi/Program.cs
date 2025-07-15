using BokurApi.Helpers;
using BokurApi.Models;
using BokurApi.RateLimiting;
using BokurApi.Repositories;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Sakur.WebApiUtilities.TaskScheduling;
using System.Security.Claims;
using BokurApi.Managers.Files;
using BokurApi.Managers.Files.Postgres;

namespace BokurApi
{
    public class Program
    {
        private const string version = "v1";
        private const string authDomain = $"https://sakur.eu.auth0.com/";
        private const string authAudience = "https://sakurapi.se/bokur";

        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            EnvironmentHelper.TestMandatoryEnvironmentVariables(); // test that the mandatory environment variables exist, will throw an exception otherwise

            SetupDatabase(); // set up the database

            // Register database connection factory and repository
            string connectionString = EnvironmentHelper.GetConnectionString();
            builder.Services.AddSingleton<IDbConnectionFactory>(new PostgresConnectionFactory(connectionString));
            builder.Services.AddScoped<IAccountRepository, AccountRepository>();
            builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
            builder.Services.AddScoped<IFileRepository, FileRepository>();
            builder.Services.AddScoped<IFileManager, PostgresFileManager>();
            builder.Services.AddScoped<SummaryHelper>();

            // Add services to the container.
            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddScheduledTasks(typeof(CheckForNewTransactionsTask));

            builder.Services.AddLogging();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.Authority = authDomain;
                    options.Audience = authAudience;

                    // If the access token does not have a `sub` claim, `User.Identity.Name` will be `null`. Map it to a different claim by setting the NameClaimType below.
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = ClaimTypes.NameIdentifier
                    };
                });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("admin", policy => policy.Requirements.Add(new HasScopeRequirement("admin", authDomain)));
            });

            builder.Services.AddSingleton<IAuthorizationHandler, HasScopeHandler>();

            WebApplication app = builder.Build();

            app.UseSwagger();
            app.UseSwaggerUI();

            app.UseHttpsRedirection();

            app.UseCors(options =>
            {
                options.AllowAnyHeader()
                       .AllowAnyMethod()
                       .SetIsOriginAllowed(origin => true) // Allow any origin
                       .AllowCredentials(); // Allow credentials (e.g., cookies)
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.UseRateLimiting();

            app.Run();
        }

        public static void SetupDatabase()
        {
            DatabaseMigrator.PerformMigrations(); // perform database migrations
            DefaultTypeMap.MatchNamesWithUnderscores = true; // set up dapper to match column names with underscore
        }
    }
}