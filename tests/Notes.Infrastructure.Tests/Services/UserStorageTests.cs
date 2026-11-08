using Notes.Domain.Entities;
using Notes.Infrastructure.Services;
using Notes.Infrastructure.Tests.Fixtures;

namespace Notes.Infrastructure.Tests.Services
{
    public class UserStorageTests : DatabaseTestBase
    {
        private readonly UserStorage _userStorage;

        public UserStorageTests(PostgresFixture fixture) : base(fixture)
        {
            _userStorage = new UserStorage(DbContext);
        }

        [Fact]
        public async Task AddUserAsync_WithNewUser_ShouldReturnTrue()
        {
            var user = new User("test", "test");

            var result = await _userStorage.AddUserAsync(user);

            Assert.True(result);

            var fromDb = await DbContext.Users.FindAsync(user.Id);

            Assert.NotNull(fromDb);
            Assert.Equal("test", fromDb.Login);
        }

        [Fact]
        public async Task AddUserAsync_WithOldUser_ReturnFalse()
        {
            var user1 = new User("duplicate", "test");
            var user2 = new User("duplicate", "test");

            var result1 = await _userStorage.AddUserAsync(user1);
            var result2 = await _userStorage.AddUserAsync(user2);

            var fromDb1 = await DbContext.Users.FindAsync(user1.Id);
            Assert.NotNull(fromDb1);
            Assert.Equal("duplicate", fromDb1.Login);

            var fromDb2 = await DbContext.Users.FindAsync(user2.Id);
            Assert.Null(fromDb2);

            Assert.True(result1);
            Assert.False(result2);
        }

        [Fact]
        public async Task GetUserAsync_WithExistingUser_ReturnUser()
        {
            var user = new User("testLogin", "password");

            await _userStorage.AddUserAsync(user);

            var result = await _userStorage.GetUserAsync(user.Id);

            Assert.NotNull(result);
            Assert.Equal("testLogin", result.Login);
        }

        [Fact]
        public async Task GetUserAsync_WithNotExistingUser_ReturnNull()
        {
            var user = new User("testLoginNotExist", "password");

            var result = await _userStorage.GetUserAsync(user.Id);

            Assert.Null(result);
        }
    }
}
