using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Notes.Infrastructure.Data;
using Notes.Infrastructure.Tests.Fixtures;

namespace Notes.Infrastructure.Tests
{
    [Collection("Database collection")]
    public abstract class DatabaseTestBase : IAsyncLifetime
    {
        protected readonly PostgresFixture Fixture;
        protected readonly NotesServerDBContext DbContext;

        protected DatabaseTestBase(PostgresFixture fixture)
        {
            Fixture = fixture;
            DbContext = Fixture.CreateDbContext();
        }

        public Task DisposeAsync()
        {
            return DbContext.DisposeAsync().AsTask();
        }

        public async Task InitializeAsync()
        {
            await DbContext.Database.EnsureCreatedAsync();
        }
    }
}
