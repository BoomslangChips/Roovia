using System.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Roovia.Data
{
    /// <summary>
    /// Retries transient SQL Server failures (dropped connections, timeouts, failovers) for the remote database.
    /// The stock retrying strategy refuses to run inside BeginTransaction / TransactionScope, which the services use
    /// throughout. Here, work outside a user transaction is retried, while work inside one fails exactly as before:
    /// a half-applied transaction cannot be safely replayed statement by statement.
    /// </summary>
    public class ResilientSqlServerExecutionStrategy : SqlServerRetryingExecutionStrategy
    {
        /// <param name="maxRetryDelay">Longest wait between attempts; zero keeps EF Core's default.</param>
        public ResilientSqlServerExecutionStrategy(ExecutionStrategyDependencies dependencies, int maxRetryCount, TimeSpan maxRetryDelay)
            : base(dependencies, maxRetryCount, maxRetryDelay > TimeSpan.Zero ? maxRetryDelay : DefaultMaxDelay, errorNumbersToAdd: null)
        {
        }

        protected override void OnFirstExecution()
        {
            if (IsInUserTransaction())
            {
                ExceptionsEncountered.Clear();
                return;
            }

            base.OnFirstExecution();
        }

        protected override bool ShouldRetryOn(Exception exception)
            => !IsInUserTransaction() && base.ShouldRetryOn(exception);

        private bool IsInUserTransaction()
        {
            var database = Dependencies.CurrentContext.Context.Database;

            return database.CurrentTransaction is not null
                || database.GetEnlistedTransaction() is not null
                || Transaction.Current is not null;
        }
    }
}
