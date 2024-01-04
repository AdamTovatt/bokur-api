﻿using Npgsql;
using System.Data;
using System.Reflection;

namespace BokurApi.Helpers
{
    public static class NpgsqlExtensionMethods
    {
        public static async Task<List<T>> QueryAsync<T>(this NpgsqlConnection connection, string query, object? parameters, Dictionary<string, Func<object?, Task<object?>>>? manualParameterLookup = null)
        {
            using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
            {
                if (parameters != null)
                    command.AddParameters(parameters);

                using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                    return await reader.GetObjectsAsync<T>(manualParameterLookup);
            }
        }

        public static async Task<T?> QuerySingleOrDefaultAsync<T>(this NpgsqlConnection connection, string query, object? parameters, Dictionary<string, Func<object?, Task<object?>>> manualParameterLookup = null)
        {
            return (await connection.QueryAsync<T>(query, parameters, manualParameterLookup)).FirstOrDefault();
        }

        private static async Task<List<T>> GetObjectsAsync<T>(this NpgsqlDataReader reader, Dictionary<string, Func<object?, Task<object?>>>? manualParameterLookup)
        {
            if (!await reader.ReadAsync())
                return new List<T>();

            DataTable? table = reader.GetSchemaTable();

            if (table == null)
                throw new Exception("Could not get schema table from reader");

            List<string> columns = table.Rows.Cast<DataRow>().Select(row => row["ColumnName"].ToString()).Where(x => !string.IsNullOrEmpty(x)).ToList()!;

            List<T> result = new List<T>();

            if (columns.Count == 1 && (typeof(T) == typeof(string) || typeof(T) == typeof(Decimal) || typeof(T).IsPrimitive)) // these types we will cast directly if only one column
            {
                string columnName = columns[0];
                do
                {
                    result.Add((T)reader[columnName]);
                }
                while (await reader.ReadAsync());

                return result;
            }
            else
            {
                List<object?> parameters = new List<object?>();
                ConstructorInfo constructor = typeof(T).GetConstructor(columns);
                Dictionary<string, Func<object?, Task<object?>>?> caseCorrectedLookup = new Dictionary<string, Func<object?, Task<object?>>?>();

                foreach (string column in columns)
                    caseCorrectedLookup.Add(column, manualParameterLookup.GetManualParameterFunction(column));

                do
                {
                    foreach (string column in columns)
                    {
                        object? value = reader[column];

                        if (value.GetType() == typeof(DBNull))
                            value = null;

                        Func<object?, Task<object?>>? manualParameterFunction = caseCorrectedLookup[column];

                        if (manualParameterFunction != null)
                            value = await manualParameterFunction(value);

                        parameters.Add(value);
                    }

                    object constructorResult = constructor.Invoke(parameters.ToArray());
                    parameters.Clear();
                    result.Add((T)constructorResult);
                }
                while (await reader.ReadAsync());

                return result;
            }
        }

        private static ConstructorInfo GetConstructor(this Type type, List<string> columns)
        {
            ConstructorInfo[] constructors = type.GetConstructors();

            foreach (ConstructorInfo constructor in constructors)
            {
                ParameterInfo[] parameters = constructor.GetParameters();

                if (parameters.Length != columns.Count)
                    continue;

                bool allMatch = true;

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].Name?.ToPascalCase() != columns[i].ToPascalCase())
                    {
                        allMatch = false;
                        break;
                    }
                }

                if (allMatch)
                    return constructor;
            }

            throw new Exception($"Could not find a constructor for type {type.Name} with the following parameters: {string.Join(", ", columns)}");
        }

        private static Func<object?, Task<object?>>? GetManualParameterFunction(this Dictionary<string, Func<object?, Task<object?>>>? manualParameterLookup, string columnName)
        {
            if (manualParameterLookup == null || manualParameterLookup.Count == 0) return null;

            if (manualParameterLookup.ContainsKey(columnName))
                return manualParameterLookup[columnName];

            string pascalCase = columnName.ToPascalCase();
            if (manualParameterLookup.ContainsKey(pascalCase))
                return manualParameterLookup[pascalCase];

            string camelCase = char.ToLower(pascalCase[0]) + pascalCase.Substring(1);
            if (manualParameterLookup.ContainsKey(camelCase))
                return manualParameterLookup[camelCase];

            return null;
        }

        private static NpgsqlCommand AddParameters(this NpgsqlCommand command, object parameters)
        {
            foreach (PropertyInfo property in parameters.GetType().GetProperties())
            {
                string propertyName = property.Name;
                object? propertyValue = property.GetValue(parameters);

                command.Parameters.Add(propertyName, SqlMapper.GetNpgsqlDbType(property.PropertyType)).Value = (propertyValue == null ? DBNull.Value : propertyValue);
            }

            return command;
        }

        public static string ToCamelCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            string pascalCase = input.ToPascalCase();

            return char.ToLower(pascalCase[0]) + pascalCase.Substring(1);
        }

        private static string ToPascalCase(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            string[] words = input.Split('_');

            for (int i = 0; i < words.Length; i++)
            {
                if (!string.IsNullOrEmpty(words[i]))
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
                }
            }

            return string.Join(string.Empty, words);
        }
    }
}
