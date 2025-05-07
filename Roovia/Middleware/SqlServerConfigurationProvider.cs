// SqlServerConfigurationProvider.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Roovia.Configuration
{
    public class SqlServerConfigurationProvider : ConfigurationProvider, IDisposable
    {
        private readonly string _connectionString;
        private readonly string _environment;
        private readonly TimeSpan _reloadInterval;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private Task _pollingTask;

        public SqlServerConfigurationProvider(string connectionString, string environment, TimeSpan? reloadInterval = null)
        {
            _connectionString = connectionString;
            _environment = environment;
            _reloadInterval = reloadInterval ?? TimeSpan.FromMinutes(5);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public override void Load()
        {
            LoadSettings();

            // Start polling for changes if not already started
            if (_pollingTask == null)
            {
                _pollingTask = PollForChanges(_cancellationTokenSource.Token);
            }
        }

        private async Task PollForChanges(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_reloadInterval, cancellationToken);
                try
                {
                    LoadSettings();
                    OnReload();
                }
                catch (Exception)
                {
                    // Log the exception but continue polling
                }
            }
        }

        private void LoadSettings()
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = @"
                            SELECT [Key], [Value] 
                            FROM [dbo].[CdnConfigSettings] 
                            WHERE [Environment] = @Environment AND [IsActive] = 1";

                        command.Parameters.AddWithValue("@Environment", _environment);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var key = reader.GetString(0);
                                var value = reader.GetString(1);
                                data[key] = value;
                            }
                        }
                    }
                }

                Data = data;
            }
            catch (Exception)
            {
                // If we can't connect to the database, keep the existing data
                // This prevents crashes if the database is temporarily unavailable
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }

    // Extension methods to make it easy to add the SQL Server provider
    public static class SqlServerConfigurationExtensions
    {
        public static IConfigurationBuilder AddSqlServer(
            this IConfigurationBuilder builder,
            string connectionString,
            string environment = "Production",
            TimeSpan? reloadInterval = null)
        {
            return builder.Add(new SqlServerConfigurationSource(connectionString, environment, reloadInterval));
        }
    }

    public class SqlServerConfigurationSource : IConfigurationSource
    {
        private readonly string _connectionString;
        private readonly string _environment;
        private readonly TimeSpan? _reloadInterval;

        public SqlServerConfigurationSource(string connectionString, string environment, TimeSpan? reloadInterval)
        {
            _connectionString = connectionString;
            _environment = environment;
            _reloadInterval = reloadInterval;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new SqlServerConfigurationProvider(_connectionString, _environment, _reloadInterval);
        }
    }
}