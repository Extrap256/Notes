using Moq;
using Notes.Application.Interfaces.Application;
using Notes.Application.Interfaces.Infrastructure;
using Notes.Application.Models;
using Notes.Application.Services;
using Notes.Domain.Entities;

namespace Notes.Application.Tests
{
    public class AuthServiceTests
    {
        private const string Login = "testuser";
        private const string Password = "password123";
        private const string PasswordHash = "hashed-password";
        private const string AccessToken = "access-token";
        private const string RefreshToken = "refresh-token";
        private static readonly DateTime AccessExpiresAt = new(2026, 1, 1, 12, 15, 0, DateTimeKind.Utc);
        private static readonly DateTime RefreshExpiresAt = new(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);

        private readonly Mock<IUserStorage> _userStorage = new();
        private readonly Mock<IPasswordHasher> _passwordHasher = new();
        private readonly Mock<ITokenService> _tokenService = new();

        private readonly AuthService _sut;

        public AuthServiceTests()
        {
            _sut = new AuthService(_userStorage.Object, _passwordHasher.Object, _tokenService.Object);
        }

        // ---------- helpers ----------

        private Guid GivenExistingUser(string login = Login, string passwordHash = PasswordHash)
        {
            var userId = Guid.NewGuid();
            _userStorage.Setup(s => s.GetUserByLoginAsync(login, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(User.Reconstruct(userId, login, passwordHash));
            return userId;
        }

        private void GivenUserNotFound(string login = Login)
        {
            _userStorage.Setup(s => s.GetUserByLoginAsync(login, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((User?)null);
        }

        private void GivenHasherReturns(string hash = PasswordHash)
        {
            _passwordHasher.Setup(h => h.Hash(Password)).Returns(hash);
        }

        private void GivenVerifyReturns(bool result)
        {
            _passwordHasher.Setup(h => h.Verify(Password, It.IsAny<string>())).Returns(result);
        }

        private AuthPairTokenModel GivenTokensPair()
        {
            var pair = new AuthPairTokenModel(AccessToken, AccessExpiresAt, RefreshToken, RefreshExpiresAt);
            _tokenService.Setup(s => s.IssueTokensAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(pair);
            return pair;
        }

        // ---------- RegisterAsync ----------

        [Fact]
        public async Task RegisterAsync_NewUser_ReturnsSuccessWithUserId()
        {
            GivenUserNotFound();
            GivenHasherReturns();
            _userStorage.Setup(s => s.AddUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            var result = await _sut.RegisterAsync(Login, Password);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(RegisterResult.ErrorCode.AllGood, result.ErrorStatus);
            Assert.NotNull(result.UserId);
            Assert.NotEqual(Guid.Empty, result.UserId);
        }

        [Fact]
        public async Task RegisterAsync_NewUser_StoresHashedPasswordNotRaw()
        {
            GivenUserNotFound();
            GivenHasherReturns();

            User? captured = null;
            _userStorage.Setup(s => s.AddUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((User u, CancellationToken _) => { captured = u; return true; });

            await _sut.RegisterAsync(Login, Password);

            Assert.NotNull(captured);
            Assert.Equal(PasswordHash, captured.Password);
            Assert.NotEqual(Password, captured.Password);
        }

        [Fact]
        public async Task RegisterAsync_NewUser_DoesNotIssueTokens()
        {
            GivenUserNotFound();
            GivenHasherReturns();
            _userStorage.Setup(s => s.AddUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

            await _sut.RegisterAsync(Login, Password);

            _tokenService.Verify(s => s.IssueTokensAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_LoginAlreadyExists_ReturnsUserAlreadyExist()
        {
            GivenExistingUser();

            var result = await _sut.RegisterAsync(Login, Password);

            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(RegisterResult.ErrorCode.UserAlreadyExist, result.ErrorStatus);
        }

        [Fact]
        public async Task RegisterAsync_LoginAlreadyExists_DoesNotCallAddUser()
        {
            GivenExistingUser();

            await _sut.RegisterAsync(Login, Password);

            _userStorage.Verify(s => s.AddUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_AddUserReturnsFalse_ReturnsUserAlreadyExist()
        {
            // Race condition: между GetUserByLoginAsync и AddUserAsync логин заняли
            GivenUserNotFound();
            GivenHasherReturns();
            _userStorage.Setup(s => s.AddUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

            var result = await _sut.RegisterAsync(Login, Password);

            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(RegisterResult.ErrorCode.UserAlreadyExist, result.ErrorStatus);
        }

        [Fact]
        public async Task RegisterAsync_PassesCancellationTokenToStorage()
        {
            GivenUserNotFound();
            GivenHasherReturns();
            _userStorage.Setup(s => s.AddUserAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);
            using var cts = new CancellationTokenSource();

            await _sut.RegisterAsync(Login, Password, cts.Token);

            _userStorage.Verify(s => s.GetUserByLoginAsync(Login, cts.Token), Times.Once);
            _userStorage.Verify(s => s.AddUserAsync(It.IsAny<User>(), cts.Token), Times.Once);
        }

        // ---------- LoginAsync ----------

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsSuccessWithTokens()
        {
            var userId = GivenExistingUser();
            GivenVerifyReturns(true);
            GivenTokensPair();

            var result = await _sut.LoginAsync(Login, Password);

            Assert.NotNull(result);
            Assert.True(result.IsSuccess);
            Assert.Equal(LoginResult.ErrorCode.AllGood, result.ErrorStatus);
            Assert.Equal(userId, result.UserId);
            Assert.NotNull(result.Tokens);
            Assert.Equal(AccessToken, result.Tokens.AccessToken);
            Assert.Equal(RefreshToken, result.Tokens.RefreshToken);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_IssuesTokensForCorrectUser()
        {
            var userId = GivenExistingUser();
            GivenVerifyReturns(true);
            GivenTokensPair();

            await _sut.LoginAsync(Login, Password);

            _tokenService.Verify(s => s.IssueTokensAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_VerifyCalledWithRawPasswordAndStoredHash()
        {
            GivenExistingUser(passwordHash: PasswordHash);
            GivenVerifyReturns(true);
            GivenTokensPair();

            await _sut.LoginAsync(Login, Password);

            _passwordHasher.Verify(h => h.Verify(Password, PasswordHash), Times.Once);
        }

        [Fact]
        public async Task LoginAsync_UserNotFound_ReturnsUserDoesNotExist()
        {
            GivenUserNotFound();

            var result = await _sut.LoginAsync(Login, Password);

            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(LoginResult.ErrorCode.UserDoesNotExist, result.ErrorStatus);
            Assert.Null(result.UserId);
            Assert.Null(result.Tokens);
        }

        [Fact]
        public async Task LoginAsync_UserNotFound_DoesNotVerifyPassword()
        {
            GivenUserNotFound();

            await _sut.LoginAsync(Login, Password);

            _passwordHasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_UserNotFound_DoesNotIssueTokens()
        {
            GivenUserNotFound();

            await _sut.LoginAsync(Login, Password);

            _tokenService.Verify(s => s.IssueTokensAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_IncorrectPassword_ReturnsIncorrectPassword()
        {
            GivenExistingUser();
            GivenVerifyReturns(false);

            var result = await _sut.LoginAsync(Login, Password);

            Assert.NotNull(result);
            Assert.False(result.IsSuccess);
            Assert.Equal(LoginResult.ErrorCode.IncorrectPassword, result.ErrorStatus);
            Assert.Null(result.UserId);
            Assert.Null(result.Tokens);
        }

        [Fact]
        public async Task LoginAsync_IncorrectPassword_DoesNotIssueTokens()
        {
            GivenExistingUser();
            GivenVerifyReturns(false);

            await _sut.LoginAsync(Login, Password);

            _tokenService.Verify(s => s.IssueTokensAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_PassesCancellationTokenToStorage()
        {
            var userId = GivenExistingUser();
            GivenVerifyReturns(true);
            GivenTokensPair();
            using var cts = new CancellationTokenSource();

            await _sut.LoginAsync(Login, Password, cts.Token);

            _userStorage.Verify(s => s.GetUserByLoginAsync(Login, cts.Token), Times.Once);
            _tokenService.Verify(s => s.IssueTokensAsync(userId, cts.Token), Times.Once);
        }
    }
}