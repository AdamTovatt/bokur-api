using Npgsql;
using System.Text;

namespace BokurApi.Helpers
{
    public static partial class EnvironmentHelper
    {
        private const string NordigenIdName = "NORDIGEN_ID";
        private const string NordigenKeyName = "NORDIGEN_KEY";
        private const string BankIdName = "BANK_ID";
        private const string RedirectUrlName = "NORDIGEN_REDIRECT_URL";
        private const string UserLanguageName = "NORDIGEN_USER_LANGUAGE";
        private const string InternalReferenceName = "NORDIGEN_INTERNAL_REFERENCE";
        private const string ConnectionStringName = "DATABASE_URL";
        private const string PostmarkTokenName = "POSTMARK_API_TOKEN";

        private static string? connectionString = null;

        public static string GetNordigenId()
        {
            return GetVariable(NordigenIdName);
        }

        public static string GetNordigenKey()
        {
            return GetVariable(NordigenKeyName);
        }

        public static string GetBankId()
        {
            return GetVariable(BankIdName);
        }

        public static string GetRedirectUrl()
        {
            return GetVariable(RedirectUrlName);
        }

        public static string GetUserLanguage()
        {
            return GetVariable(UserLanguageName);
        }

        public static string GetInternalReference()
        {
            return GetVariable(InternalReferenceName);
        }

        public static string GetPostmarkToken()
        {
            return GetVariable(PostmarkTokenName);
        }

        public static string GetConnectionString()
        {
            connectionString ??= GetConnectionStringFromUrl(GetVariable(ConnectionStringName), SslMode.Prefer);

            return connectionString;
        }

        public static string GetVariable(string name)
        {
            string? variable = Environment.GetEnvironmentVariable(name);

            if (string.IsNullOrEmpty(variable))
                throw new InvalidOperationException($"Missing {name} in environment variables");

            return variable;
        }

        /// <summary>
        /// Will test that the mandatory environment variables exist
        /// </summary>
        public static void TestMandatoryEnvironmentVariables()
        {
            GetNordigenId();
            GetNordigenKey();
            GetBankId();
            GetRedirectUrl();
            GetUserLanguage();
            GetInternalReference();
            GetConnectionString();
            GetPostmarkToken();
        }

        private class ConnectionStringBuilder
        {
            public string? Host { get; set; }

            public int Port { get; set; }

            public string? Username { get; set; }

            public string? Password { get; set; }

            public string? Database { get; set; }

            public SslMode SslMode { get; set; }

            public bool TrustServerCertificate { get; set; }

            public override string ToString()
            {
                StringBuilder stringBuilder = new StringBuilder();
                if (Host != null)
                {
                    stringBuilder.Append("Host=");
                    stringBuilder.Append(Host);
                    stringBuilder.Append(";");
                }

                if (Port != 0)
                {
                    stringBuilder.Append("Port=");
                    stringBuilder.Append(Port);
                    stringBuilder.Append(";");
                }

                if (Username != null)
                {
                    stringBuilder.Append("Username=");
                    stringBuilder.Append(Username);
                    stringBuilder.Append(";");
                }

                if (Password != null)
                {
                    stringBuilder.Append("Password=");
                    stringBuilder.Append(Password);
                    stringBuilder.Append(";");
                }

                if (Database != null)
                {
                    stringBuilder.Append("Database=");
                    stringBuilder.Append(Database);
                    stringBuilder.Append(";");
                }

                stringBuilder.Append("SSL Mode=");
                stringBuilder.Append(SslMode.ToString());
                stringBuilder.Append(";");
                stringBuilder.Append("Trust Server Certificate=");
                stringBuilder.Append(TrustServerCertificate);
                return stringBuilder.ToString();
            }
        }

        public static string GetConnectionStringFromUrl(string url, SslMode sslMode = SslMode.Require, bool trustServerCertificate = true)
        {
            try
            {
                Uri uri = new Uri(url);
                string[] array = uri.UserInfo.Split(':');
                ConnectionStringBuilder connectionStringBuilder = new ConnectionStringBuilder
                {
                    Host = uri.Host,
                    Port = uri.Port,
                    Username = array[0],
                    Password = array[1],
                    Database = uri.LocalPath.TrimStart('/'),
                    SslMode = sslMode,
                    TrustServerCertificate = trustServerCertificate
                };
                return connectionStringBuilder.ToString();
            }
            catch
            {
                throw new Exception("Unknown error when url was being converted to connection string");
            }
        }
    }
}
