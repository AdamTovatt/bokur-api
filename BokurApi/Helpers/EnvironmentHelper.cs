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

        private static string GetVariable(string name)
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
        }
    }
}
