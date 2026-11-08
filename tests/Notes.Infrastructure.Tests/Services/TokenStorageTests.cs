using Microsoft.EntityFrameworkCore;
using Notes.Application.DTO;
using Notes.Application.Interfaces.Infrastructure;
using Notes.Domain.Entities;
using Notes.Infrastructure.Services;
using Notes.Infrastructure.Tests.Fixtures;

namespace Notes.Infrastructure.Tests.Services
{
    public class TokenStorageTests : DatabaseTestBase
    {
        private readonly TokenStorage _tokenStorage;
        public TokenStorageTests(PostgresFixture fixture) : base(fixture)
        {
            _tokenStorage = new TokenStorage(DbContext);
        }

        // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===
        private async Task<User> CreateUserAsync(string login = "test_user")
        {
            var user = new User(login, "password_hash");
            await DbContext.Users.AddAsync(ConvertEntities.ToStorageEntity(user));
            await DbContext.SaveChangesAsync();
            return user;
        }

        private RefreshTokenModel CreateTokenModel(Guid userId, string hash, double daysToExpire = 1, bool isRevoked = false)
        {
            return new RefreshTokenModel(
                UserId: userId,
                Hash: hash,
                ExpiresAt: DateTime.UtcNow.AddDays(daysToExpire),
                IsRevoked: isRevoked
            );
        }

        // === ТЕСТИРОВАНИЕ AddTokenAsync ===
        [Fact]
        public async Task AddTokenAsync_AddNewToken_ReturnTrue()
        {
            User user = new("оченьсложныйчтобнеповторял", "password");
            await DbContext.Users.AddAsync(ConvertEntities.ToStorageEntity(user));
            await DbContext.SaveChangesAsync();

            RefreshTokenModel token = new(user.Id, "hashstring", DateTime.UtcNow.AddDays(1), false);

            await _tokenStorage.AddTokenAsync(token);

            var fromDb = await DbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Hash == "hashstring");

            Assert.NotNull(fromDb);
        }

        [Fact]
        public async Task AddTokenAsync_DuplicateHash_ReturnFalse()
        {
            var user = await CreateUserAsync("login_dup_hash");
            var token1 = CreateTokenModel(user.Id, "same_hash");
            var token2 = CreateTokenModel(user.Id, "same_hash"); // Тот же хеш

            await _tokenStorage.AddTokenAsync(token1);

            // Попытка добавить токен с тем же хешем должна вернуть false
            var result = await _tokenStorage.AddTokenAsync(token2);

            Assert.False(result);

            // В БД должна остаться только одна запись
            var count = await DbContext.RefreshTokens.CountAsync(x => x.Hash == "same_hash");
            Assert.Equal(1, count);
        }

        // === ТЕСТИРОВАНИЕ GetAllActiveTokensAsync ===

        [Fact]
        public async Task GetAllActiveTokensAsync_OnlyActiveTokens_ReturnsAll()
        {
            var user = await CreateUserAsync("login_active_only");
            var token1 = CreateTokenModel(user.Id, "hash_active_1", daysToExpire: 1, isRevoked: false);
            var token2 = CreateTokenModel(user.Id, "hash_active_2", daysToExpire: 2, isRevoked: false);

            await _tokenStorage.AddTokenAsync(token1);
            await _tokenStorage.AddTokenAsync(token2);

            var result = await _tokenStorage.GetAllActiveTokensAsync(user.Id);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, t => Assert.False(t.IsRevoked));
        }

        [Fact]
        public async Task GetAllActiveTokensAsync_ExcludesRevokedAndExpired_ReturnsOnlyActive()
        {
            var user = await CreateUserAsync("login_mixed");

            var activeToken = CreateTokenModel(user.Id, "hash_active1", daysToExpire: 1, isRevoked: false);
            var revokedToken = CreateTokenModel(user.Id, "hash_revoked1", daysToExpire: 1, isRevoked: true);
            var expiredToken = CreateTokenModel(user.Id, "hash_expired1", daysToExpire: -1, isRevoked: false); // Просрочен

            await _tokenStorage.AddTokenAsync(activeToken);
            await _tokenStorage.AddTokenAsync(revokedToken);
            await _tokenStorage.AddTokenAsync(expiredToken);

            var result = await _tokenStorage.GetAllActiveTokensAsync(user.Id);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("hash_active1", result[0].Hash);
        }

        [Fact]
        public async Task GetAllActiveTokensAsync_NoTokens_ReturnsEmptyList()
        {
            var user = await CreateUserAsync("login_no_tokens");
            var result = await _tokenStorage.GetAllActiveTokensAsync(user.Id);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        // === ТЕСТИРОВАНИЕ GetAllTokensAsync ===

        [Fact]
        public async Task GetAllTokensAsync_MixedTokens_ReturnsAll()
        {
            var user = await CreateUserAsync("login_get_all");

            var activeToken = CreateTokenModel(user.Id, "hash_active", daysToExpire: 1, isRevoked: false);
            var revokedToken = CreateTokenModel(user.Id, "hash_revoked", daysToExpire: 1, isRevoked: true);

            await _tokenStorage.AddTokenAsync(activeToken);
            await _tokenStorage.AddTokenAsync(revokedToken);

            var result = await _tokenStorage.GetAllTokensAsync(user.Id);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, t => t.Hash == "hash_active");
            Assert.Contains(result, t => t.Hash == "hash_revoked");
        }

        [Fact]
        public async Task GetAllTokensAsync_NoTokens_ReturnsEmptyList()
        {
            var user = await CreateUserAsync("login_get_all_empty");
            var result = await _tokenStorage.GetAllTokensAsync(user.Id);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        // === ТЕСТИРОВАНИЕ RevokeAsync ===

        [Fact]
        public async Task RevokeAsync_ValidToken_ReturnsTrueAndUpdatesDb()
        {
            var user = await CreateUserAsync("login_revoke_valid");
            var token = CreateTokenModel(user.Id, "hash_to_revoke", daysToExpire: 1, isRevoked: false);

            await _tokenStorage.AddTokenAsync(token);

            var result = await _tokenStorage.RevokeAsync(token);

            Assert.True(result);

            // Проверяем, что в БД токен действительно отозван
            var fromDb = await DbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Hash == "hash_to_revoke");
            Assert.NotNull(fromDb);
            Assert.NotNull(fromDb.RevokedAt); // Дата отзыва должна быть установлена

            // Проверяем через интерфейс получения
            var activeTokens = await _tokenStorage.GetAllActiveTokensAsync(user.Id);
            Assert.DoesNotContain(activeTokens, t => t.Hash == "hash_to_revoke");
        }

        [Fact]
        public async Task RevokeAsync_AlreadyRevokedToken_ReturnsFalse()
        {
            var user = await CreateUserAsync("login_revoke_already");
            var token = CreateTokenModel(user.Id, "hash_already_revoked", daysToExpire: 1, isRevoked: true);

            // Добавляем сразу отозванный (или отзываем его перед тестом)
            await _tokenStorage.AddTokenAsync(token);

            // Для чистоты эксперимента, если AddTokenAsync игнорирует isRevoked, отзовем его явно через БД
            var entity = await DbContext.RefreshTokens.FirstAsync(x => x.Hash == "hash_already_revoked");
            entity.RevokedAt = DateTime.UtcNow;
            await DbContext.SaveChangesAsync();

            var result = await _tokenStorage.RevokeAsync(token);

            Assert.False(result);
        }

        [Fact]
        public async Task RevokeAsync_NonExistingToken_ReturnsNull()
        {
            var user = await CreateUserAsync("login_revoke_null");
            var fakeToken = CreateTokenModel(user.Id, "hash_does_not_exist");

            var result = await _tokenStorage.RevokeAsync(fakeToken);

            Assert.Null(result);
        }
    }
}
