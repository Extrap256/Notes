using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Notes.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace Notes.Infrastructure.Tests.Fixtures
{
    public class PostgresFixture : IAsyncLifetime
    {
        // Контейнер для PostgreSql
        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("notes_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
        public string ConnectionString => _container.GetConnectionString();

        public async Task DisposeAsync()
        {
            await _container.DisposeAsync();
        }

        public async Task InitializeAsync()
        {
            await _container.StartAsync();
        }

        public NotesServerDBContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<NotesServerDBContext>()
                .UseNpgsql(ConnectionString)
                .Options;

            return new NotesServerDBContext(options);
        }
    }
}
