using DbUp.Engine;
using DbUp;
using System.Reflection;

namespace BokurApi.Helpers
{
    public class DatabaseMigrator
    {
        public static void PerformMigrations()
        {
            string connectionString = EnvironmentHelper.GetConnectionString();

            UpgradeEngine upgrader =
                DeployChanges.To
                    .PostgresqlDatabase(connectionString)
                    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), (string s) => { return s.Contains("DatabaseMigrations") && s.Split(".").Last() == "sql"; })
                    .LogToConsole()
                    .Build();

            DatabaseUpgradeResult result = upgrader.PerformUpgrade();

            if (!result.Successful)
                throw new Exception($"Error when performing database upgrade, failing on script: {result.ErrorScript?.Name} with error {result.Error}");
        }
    }
}
