namespace Roovia.Models.ConnectivityConfigModels
{
    /// <summary>
    /// Live-connection settings bound from the "Connectivity" section.
    /// Circuit and Hub sub-sections bind straight onto the framework CircuitOptions / HubOptions.
    /// </summary>
    public class ConnectivityOptions
    {
        public const string SectionName = "Connectivity";
        public const string CircuitSectionName = SectionName + ":Circuit";
        public const string HubSectionName = SectionName + ":Hub";

        /// <summary>
        /// Prerendering runs every page's data load twice (once on the server, once when the circuit starts).
        /// </summary>
        public bool PrerenderPages { get; set; }

        public ClientConnectionOptions Client { get; set; } = new();
    }

    /// <summary>
    /// Settings handed to blazor.web.js when the circuit starts. Zero / empty values fall back to the framework defaults.
    /// </summary>
    public class ClientConnectionOptions
    {
        public int ServerTimeoutMilliseconds { get; set; }

        public int KeepAliveIntervalMilliseconds { get; set; }

        public int MaxRetries { get; set; }

        /// <summary>
        /// Delay before each reconnect attempt; the last value repeats for all remaining attempts.
        /// </summary>
        public int[] RetryIntervalsMilliseconds { get; set; } = [];
    }

    /// <summary>
    /// Resilience settings for the SQL Server link, bound from the "Database" section. Zero values keep the provider defaults.
    /// </summary>
    public class DatabaseResilienceOptions
    {
        public const string SectionName = "Database";

        public int CommandTimeoutSeconds { get; set; }

        public int MaxRetryCount { get; set; }

        public int MaxRetryDelaySeconds { get; set; }

        public int ConnectTimeoutSeconds { get; set; }

        public int ConnectRetryCount { get; set; }

        public int ConnectRetryIntervalSeconds { get; set; }
    }

    /// <summary>
    /// Bound from the "Permissions" section.
    /// </summary>
    public class PermissionCacheOptions
    {
        public const string SectionName = "Permissions";

        /// <summary>
        /// How long a user's effective permission set is reused within one request / circuit before it is reloaded.
        /// </summary>
        public int SnapshotLifetimeSeconds { get; set; }
    }

    /// <summary>
    /// Bound from the "Syncfusion" section. The assets are only loaded by the components that need them.
    /// </summary>
    public class SyncfusionOptions
    {
        public const string SectionName = "Syncfusion";

        public string? LicenseKey { get; set; }

        public string? ScriptPath { get; set; }

        public string? ThemePath { get; set; }
    }
}
