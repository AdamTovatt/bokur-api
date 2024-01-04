using BokurApi.Helpers;
using BokurApi.RateLimiting;
using Dapper;

namespace BokurApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            EnvironmentHelper.TestMandatoryEnvironmentVariables(); // test that the mandatory environment variables exist, will throw an exception otherwise

            SetupDatabase(); // set up the database

            // Add services to the container.
            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

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