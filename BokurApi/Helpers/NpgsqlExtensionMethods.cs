using Npgsql;
using System.Data;
using System.Reflection;

namespace BokurApi.Helpers
{
    public static class NpgsqlExtensionMethods
    {
        public static async Task<T?> QuerySingleOrDefaultAsync<T>(this NpgsqlConnection connection, string query, object parameters, Dictionary<string, Func<object, object>> manualParameterLookup)
        {
            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                command.AddParameters(parameters);

                using(NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while(await reader.ReadAsync())
                    {
                        return reader.GetObject<T>(manualParameterLookup);
                    }
                }

                return default(T);
            }
        }

        private static T GetObject<T>(this NpgsqlDataReader reader, Dictionary<string, Func<object, object>> manualParameterLookup)
        {
            T obj = Activator.CreateInstance<T>();

            DataTable? table = reader.GetSchemaTable();

            foreach (PropertyInfo property in obj.GetType().GetProperties())
            {
                string propertyName = property.Name;
                object? propertyValue = reader[propertyName];

                if (manualParameterLookup.ContainsKey(propertyName))
                    propertyValue = manualParameterLookup[propertyName](propertyValue);

                property.SetValue(obj, propertyValue);
            }

            return obj;
        }

        private static NpgsqlCommand AddParameters(this NpgsqlCommand command, object parameters)
        {
            foreach (PropertyInfo property in parameters.GetType().GetProperties())
            {
                string propertyName = property.Name;
                object? propertyValue = property.GetValue(parameters);

                command.Parameters.Add(propertyName, SqlMapper.GetNpgsqlDbType(property.PropertyType)).Value = propertyValue;
            }

            return command;
        }
    }
}
