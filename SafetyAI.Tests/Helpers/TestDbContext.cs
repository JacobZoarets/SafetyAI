using System;
using System.Data.Entity;
using SafetyAI.Data.Context;

namespace SafetyAI.Tests.Helpers
{
    public class TestDbContext : SafetyAIDbContext
    {
        public TestDbContext() : base()
        {
            Database.SetInitializer(new DropCreateDatabaseAlways<TestDbContext>());
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}