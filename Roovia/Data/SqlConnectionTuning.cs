using Microsoft.Data.SqlClient;
using Roovia.Models.ConnectivityConfigModels;

namespace Roovia.Data
{
    public static class SqlConnectionTuning
    {
        /// <summary>
        /// Applies the configured connect timeout / idle-connection recovery to a connection string.
        /// Only values that are set (&gt; 0) override what the connection string already contains.
        /// </summary>
        public static string Apply(string connectionString, DatabaseResilienceOptions options)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);

            if (options.ConnectTimeoutSeconds > 0)
            {
                builder.ConnectTimeout = options.ConnectTimeoutSeconds;
            }

            if (options.ConnectRetryCount > 0)
            {
                builder.ConnectRetryCount = options.ConnectRetryCount;
            }

            if (options.ConnectRetryIntervalSeconds > 0)
            {
                builder.ConnectRetryInterval = options.ConnectRetryIntervalSeconds;
            }

            if (options.CommandTimeoutSeconds > 0)
            {
                builder.CommandTimeout = options.CommandTimeoutSeconds;
            }

            return builder.ConnectionString;
        }
    }
}
