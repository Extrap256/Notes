using System.Timers;
using Moq;
using Notes.Application.Interfaces.Infrastructure;
using Notes.Application.Models;
using Notes.Application.Services;
using Notes.Domain.Entities;

namespace Notes.Application.Tests
{
    public class TokenServiceTests
    {
        private static readonly DateTime UtcNow = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime AccessExpiresAt = UtcNow.AddMinutes(15);
        private static readonly DateTime RefreshExpiresAt = UtcNow.AddDays(30);

        private const string AccessTokenValue = "access-token";
        private const string OldRawRefresh = "old-raw-refresh-token";
        private const string OldHash = "hash-of-old-raw-refresh-token";
        private const string NewRawRefresh = "new-raw-refresh-token";
        private const string NewHash = "hash-of-new-raw-refresh-token";
        private const string OtherDeviceHash = "other-device-hash";
        private const string StrangerHash = "stranger-hash";

        private readonly Mock<IUserStorage> _userStorage = new();
        private readonly Mock<ITokenStorage> _tokenStorage = new();
        private readonly Mock<ITokenGenerator> _tokenGenerator = new();
        private readonly Mock<ITokenHasher> _tokenHasher = new();
        private readonly Mock<IDateTimeProvider> _dateTimeProvider = new();

        // состояние "базы" токенов: тесты проверяют итог, а не порядок вызовов
        private readonly List<RefreshTokenModel> _tokens = new();

        private readonly TokenService _sut;

        public TokenServiceTests()
        {
            _dateTimeProvider.SetupGet(p => p.UtcNow).Returns(UtcNow);

            _tokenHasher.Setup(h => h.Hash(OldRawRefresh)).Returns(OldHash);
            _tokenHasher.Setup(h => h.Hash(NewRawRefresh)).Returns(NewHash);

            _tokenGenerator
                .Setup(g => g.GenerateAccessToken(It.IsAny<Guid>()))
                .Returns(new AccessTokenModel(AccessTokenValue, AccessExpiresAt));

            _tokenGenerator
                .Setup(g => g.GenerateRefreshToken())
                .Returns(new RefreshTokenIssueModel(NewRawRefresh, RefreshExpiresAt));

            SetupStatefulTokenStorage();

            _sut = new TokenService(
                _userStorage.Object,
                _tokenStorage.Object,
                _tokenGenerator.Object,
                _tokenHasher.Object,
                _dateTimeProvider.Object);
        }

        // ---------- infrastructure ----------

        private void SetupStatefulTokenStorage()
        {
            _tokenStorage
                .Setup(s => s.AddTokenAsync(It.IsAny<RefreshTokenModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((RefreshTokenModel token, CancellationToken _) =>
                {
                    if (_tokens.Any(t => t.Hash == token.Hash))
                        return false;

                    _tokens.Add(token);
                    return true;
                });

            _tokenStorage
                .Setup(s => s.GetTokenByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string hash, CancellationToken _) =>
                    _tokens.FirstOrDefault(t => t.Hash == hash));

            _tokenStorage
                .Setup(s => s.GetAllActiveTokensAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid userId, CancellationToken _) =>
                    _tokens.Where(t => t.UserId == userId && !t.IsRevoked).ToList());

            _tokenStorage
                .Setup(s => s.RevokeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string hash, CancellationToken _) =>
                {
                    var index = _tokens.FindIndex(t => t.Hash == hash);
                    if (index < 0) return (bool?)null;
                    if (_tokens[index].IsRevoked) return (bool?)false;

                    _tokens[index] = _tokens[index] with { IsRevoked = true };
                    return (bool?)true;
                });
        }

        private Guid GivenExistingUser()
        {
            var userId = Guid.NewGuid();
            _userStorage
                .Setup(s => s.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(User.Reconstruct(userId, "login", "password-hash"));
            return userId;
        }

        private void GivenStoredToken(
            Guid userId,
            string hash = OldHash,
            bool isRevoked = false,
            DateTime? expiresAt = null)
        {
            _tokens.Add(new RefreshTokenModel(userId, hash, expiresAt ?? RefreshExpiresAt, isRevoked));
        }

        // ---------- IssueTokensAsync ----------

        [Fact]
        public async Task IssueTokensAsync_ExistingUser_ReturnsPairFromGenerator()
        {
            var userId = GivenExistingUser();

            var result = await _sut.IssueTokensAsync(userId);

            Assert.NotNull(result);
            Assert.Equal(AccessTokenValue, result.AccessToken);
            Assert.Equal(AccessExpiresAt, result.AccessTokenExpiresAt);
            Assert.Equal(NewRawRefresh, result.RefreshToken);
            Assert.Equal(RefreshExpiresAt, result.RefreshTokenExpiresAt);
        }

        [Fact]
        public async Task IssueTokensAsync_ExistingUser_ReturnsRawRefreshAndStoresOnlyHash()
        {
            var userId = GivenExistingUser();

            var result = await _sut.IssueTokensAsync(userId);

            Assert.NotNull(result);
            Assert.Equal(NewRawRefresh, result.RefreshToken);
            Assert.NotEqual(NewHash, result.RefreshToken);

            var stored = Assert.Single(_tokens);
            Assert.Equal(NewHash, stored.Hash);
            Assert.DoesNotContain(_tokens, t => t.Hash == NewRawRefresh);

            _tokenHasher.Verify(h => h.Hash(NewRawRefresh), Times.Once);
        }

        [Fact]
        public async Task IssueTokensAsync_ExistingUser_StoresTokenForSameUserNotRevoked()
        {
            var userId = GivenExistingUser();

            await _sut.IssueTokensAsync(userId);

            var stored = Assert.Single(_tokens);
            Assert.Equal(userId, stored.UserId);
            Assert.False(stored.IsRevoked);
            Assert.Equal(RefreshExpiresAt, stored.ExpiresAt);
        }

        [Fact]
        public async Task IssueTokensAsync_ExistingUser_GeneratesAccessTokenForThatUser()
        {
            var userId = GivenExistingUser();

            await _sut.IssueTokensAsync(userId);

            _tokenGenerator.Verify(g => g.GenerateAccessToken(userId), Times.Once);
        }

        [Fact]
        public async Task IssueTokensAsync_NonExistingUser_ReturnsNull()
        {
            var result = await _sut.IssueTokensAsync(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task IssueTokensAsync_NonExistingUser_DoesNotGenerateOrStoreTokens()
        {
            await _sut.IssueTokensAsync(Guid.NewGuid());

            Assert.Empty(_tokens);
            _tokenGenerator.Verify(g => g.GenerateAccessToken(It.IsAny<Guid>()), Times.Never);
            _tokenGenerator.Verify(g => g.GenerateRefreshToken(), Times.Never);
            _tokenStorage.Verify(
                s => s.AddTokenAsync(It.IsAny<RefreshTokenModel>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task IssueTokensAsync_UserAlreadyHasActiveToken_KeepsOldTokenActive()
        {
            // вход с нового устройства не должен разлогинивать остальные
            var userId = GivenExistingUser();
            GivenStoredToken(userId);

            await _sut.IssueTokensAsync(userId);

            Assert.Equal(2, _tokens.Count);
            Assert.False(_tokens.Single(t => t.Hash == OldHash).IsRevoked);
        }

        [Fact]
        public async Task IssueTokensAsync_StorageRejectsNewToken_ReturnsNull()
        {
            var userId = GivenExistingUser();
            _tokenStorage
                .Setup(s => s.AddTokenAsync(It.IsAny<RefreshTokenModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _sut.IssueTokensAsync(userId);

            Assert.Null(result);
        }

        [Fact]
        public async Task IssueTokensAsync_PassesCancellationTokenToStorage()
        {
            var userId = GivenExistingUser();
            using var cts = new CancellationTokenSource();

            await _sut.IssueTokensAsync(userId, cts.Token);

            _tokenStorage.Verify(
                s => s.AddTokenAsync(It.IsAny<RefreshTokenModel>(), cts.Token),
                Times.Once);
        }

        // ---------- RefreshAsync ----------

        [Fact]
        public async Task RefreshAsync_ValidToken_ReturnsNewPair()
        {
            var userId = GivenExistingUser();
            GivenStoredToken(userId);

            var result = await _sut.RefreshAsync(OldRawRefresh);

            Assert.NotNull(result);
            Assert.Equal(AccessTokenValue, result.AccessToken);
            Assert.Equal(AccessExpiresAt, result.AccessTokenExpiresAt);
            Assert.Equal(NewRawRefresh, result.RefreshToken);
            Assert.Equal(RefreshExpiresAt, result.RefreshTokenExpiresAt);
        }

        [Fact]
        public async Task RefreshAsync_ValidToken_LooksUpTokenByHashNotByRawValue()
        {
            var userId = GivenExistingUser();
            GivenStoredToken(userId);

            await _sut.RefreshAsync(OldRawRefresh);

            _tokenHasher.Verify(h => h.Hash(OldRawRefresh), Times.Once);
            _tokenStorage.Verify(
                s => s.GetTokenByHashAsync(OldHash, It.IsAny<CancellationToken>()),
                Times.Once);
            _tokenStorage.Verify(
                s => s.GetTokenByHashAsync(OldRawRefresh, It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task RefreshAsync_ValidToken_RevokesOldTokenAndStoresNewOne()
        {
            var userId = GivenExistingUser();
            GivenStoredToken(userId);

            await _sut.RefreshAsync(OldRawRefresh);

            Assert.Equal(2, _tokens.Count);
            Assert.True(_tokens.Single(t => t.Hash == OldHash).IsRevoked);

            var stored = _tokens.Single(t => t.Hash == NewHash);
            Assert.Equal(userId, stored.UserId);
            Assert.False(stored.IsRevoked);
            Assert.Equal(RefreshExpiresAt, stored.ExpiresAt);
        }

        [Fact]
        public async Task RefreshAsync_ValidToken_IssuesAccessTokenForTokenOwner()
        {
            var userId = GivenExistingUser();
            GivenStoredToken(userId);

            await _sut.RefreshAsync(OldRawRefresh);

            _tokenGenerator.Verify(g => g.GenerateAccessToken(userId), Times.Once);
        }

        [Fact]
        public async Task RefreshAsync_ValidToken_DoesNotTouchOtherUsersTokens()
        {
            var userId = GivenExistingUser();
            var strangerId = GivenExistingUser();
            GivenStoredToken(userId);
            GivenStoredToken(strangerId, hash: StrangerHash);

            await _sut.RefreshAsync(OldRawRefresh);

            Assert.False(_tokens.Single(t => t.Hash == StrangerHash).IsRevoked);
        }

        [Fact]
        public async Task RefreshAsync_UnknownToken_ReturnsNull()
        {
            var result = await _sut.RefreshAsync(OldRawRefresh);

            Assert.Null(result);
        }

        [Fact]
        public async Task RefreshAsync_UnknownToken_DoesNotGenerateOrStoreTokens()
        {
            await _sut.RefreshAsync(OldRawRefresh);

            Assert.Empty(_tokens);
            _tokenGenerator.Verify(g => g.GenerateAccessToken(It.IsAny<Guid>()), Times.Never);
            _tokenGenerator.Verify(g => g.GenerateRefreshToken(), Times.Never);
        }

        [Fact]
        public async Task RefreshAsync_ExpiredToken_ReturnsNullAndIssuesNothing()
        {
            var userId = GivenExistingUser();
            GivenStoredToken(userId, expiresAt: UtcNow.AddSeconds(-1));

            var result = await _sut.RefreshAsync(OldRawRefresh);

            Assert.Null(result);
            Assert.Single(_tokens);
            _tokenGenerator.Verify(g => g.GenerateRefreshToken(), Times.Never);
        }

        [Fact]
        public async Task RefreshAsync_TokenExpiresExactlyNow_ReturnsNull()
        {
            var userId = GivenExistingUser();
            GivenStoredToken(userId, expiresAt: UtcNow);

            var result = await _sut.RefreshAsync(OldRawRefresh);

            Assert.Null(result);
        }

        [Fact]
        public async Task RefreshAsync_RevokedToken_ReturnsNull()
        {
            var userId = GivenExistingUser();
            GivenStoredToken(userId, isRevoked: true);

            var result = await _sut.RefreshAsync(OldRawRefresh);

            Assert.Null(result);
            Assert.DoesNotContain(_tokens, t => t.Hash == NewHash);
        }

        [Fact]
        public async Task RefreshAsync_RevokedToken_DoesNotRevokeOtherTokensOfThatUser()
        {
            var userId = GivenExistingUser();
            var strangerId = GivenExistingUser();

            // Токен, который уже отозван (например, через logout на другом устройстве)
            GivenStoredToken(userId, isRevoked: true);
            // Активный токен жертвы на другом устройстве
            GivenStoredToken(userId, hash: OtherDeviceHash);
            // Токен постороннего пользователя
            GivenStoredToken(strangerId, hash: StrangerHash);

            var result = await _sut.RefreshAsync(OldRawRefresh);

            Assert.Null(result);

            // Ключевая проверка: активные токены НЕ отозваны
            Assert.False(_tokens.Single(t => t.Hash == OtherDeviceHash).IsRevoked);
            Assert.False(_tokens.Single(t => t.Hash == StrangerHash).IsRevoked);

            // И не было попыток массового отзыва
            _tokenStorage.Verify(
                s => s.GetAllActiveTokensAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task RefreshAsync_TokenOwnerNoLongerExists_ReturnsNull()
        {
            var userId = Guid.NewGuid(); // в IUserStorage не зарегистрирован
            GivenStoredToken(userId);

            var result = await _sut.RefreshAsync(OldRawRefresh);

            Assert.Null(result);
            Assert.DoesNotContain(_tokens, t => t.Hash == NewHash);
        }

        [Fact]
        public async Task RefreshAsync_PassesCancellationTokenToStorage()
        {
            var userId = GivenExistingUser();
            GivenStoredToken(userId);
            using var cts = new CancellationTokenSource();

            await _sut.RefreshAsync(OldRawRefresh, cts.Token);

            _tokenStorage.Verify(s => s.GetTokenByHashAsync(OldHash, cts.Token), Times.Once);
        }
    }
}