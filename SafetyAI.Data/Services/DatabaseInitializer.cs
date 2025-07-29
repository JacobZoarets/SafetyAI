using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using SafetyAI.Data.Context;
using SafetyAI.Data.Migrations;

namespace SafetyAI.Data.Services
{
    public static class DatabaseInitializer
    {
        private static bool _isInitialized = false;
        private static readonly object _lock = new object();

        public static void Initialize()
        {
            if (_isInitialized) return;

            lock (_lock)
            {
                if (_isInitialized) return;

                try
                {
                    // Set the database initializer
                    Database.SetInitializer(new MigrateDatabaseToLatestVersion<SafetyAIDbContext, Configuration>());

                    // Force initialization
                    using (var context = new SafetyAIDbContext())
                    {
                        context.Database.Initialize(force: false);
                        
                        // Ensure database exists and is up to date
                        if (!context.Database.Exists())
                        {
                            context.Database.Create();
                        }
                        
                        // Run any pending migrations
                        var migrator = new DbMigrator(new Configuration());
                        migrator.Update();
                    }

                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    // Log the error (implement logging in later tasks)
                    System.Diagnostics.Debug.WriteLine($"Database initialization failed: {ex.Message}");
                    throw;
                }
            }
        }

        public static void CreateDatabase()
        {
            using (var context = new SafetyAIDbContext())
            {
                if (!context.Database.Exists())
                {
                    context.Database.Create();
                }
            }
        }

        public static void DropDatabase()
        {
            using (var context = new SafetyAIDbContext())
            {
                if (context.Database.Exists())
                {
                    context.Database.Delete();
                }
            }
        }

        public static bool DatabaseExists()
        {
            using (var context = new SafetyAIDbContext())
            {
                return context.Database.Exists();
            }
        }

        public static void SeedTestData()
        {
            using (var context = new SafetyAIDbContext())
            {
                var configuration = new Configuration();
                configuration.SeedData(context);
            }
        }
    }
}