using Notes.Infrastructure.Tests.Fixtures;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<PostgresFixture>
{
}