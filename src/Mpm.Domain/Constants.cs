namespace Mpm.Domain;

public static class Constants
{
    public static class Roles
    {
        public const string Purchaser = "Purchaser";
        public const string ProjectManager = "ProjectManager";
        public const string ShopManager = "ShopManager";
        public const string Welder = "Welder";
        public const string SawOperator = "SawOperator";
        public const string MagDrillOperator = "MagDrillOperator";
        public const string QA = "QA";
        public const string Admin = "Admin";
        public const string TenantAdmin = "TenantAdmin";
    }

    public static class Currency
    {
        public const string EUR = "EUR";
        public const string USD = "USD";
        public const string GBP = "GBP";
        
        public static readonly string[] ValidCurrencies = 
        {
            EUR, USD, GBP, "AUD", "CAD", "CHF", "CNY", "DKK", "HKD", "JPY", 
            "NOK", "NZD", "PLN", "SEK", "SGD", "ZAR"
        };
        
        public static bool IsValidCurrency(string currency)
        {
            return ValidCurrencies.Contains(currency?.ToUpperInvariant());
        }
    }

    public static class UnitOfMeasure
    {
        public const string Meter = "m";
        public const string Pieces = "pcs";
        public const string Kilogram = "kg";
    }
}
