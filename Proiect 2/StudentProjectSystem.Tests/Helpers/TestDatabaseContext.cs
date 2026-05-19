using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StudentProjectSystem.Data;

namespace StudentProjectSystem.Tests.Helpers
{
    public class TestDatabaseContext : IDisposable
    {
        private readonly SqliteConnection _connection;
        public AppDbContext DbContext { get; }

        public TestDatabaseContext()
        {
            // Creează conexiunea la SQLite în memorie
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            DbContext = new AppDbContext(options);
            
            // Asigură crearea schemei și rularea datelor de seed
            DbContext.Database.EnsureCreated();
        }

        public void Dispose()
        {
            DbContext.Dispose();
            _connection.Close();
            _connection.Dispose();
        }
    }
}
