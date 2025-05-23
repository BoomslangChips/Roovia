﻿using Microsoft.EntityFrameworkCore;

namespace Roovia.Data
{
    public class DbContextFactory<TContext> : IDbContextFactory<TContext> where TContext : DbContext
    {
        private readonly DbContextOptions<TContext> _options;

        public DbContextFactory(DbContextOptions<TContext> options)
        {
            _options = options;
        }

        public TContext CreateDbContext()
        {
            return (TContext)Activator.CreateInstance(typeof(TContext), _options);
        }
    }
}