using Notes.Application.Interfaces.Application;
using Notes.Application.Interfaces.Infrastructure;
using Notes.Application.Models;
using Notes.Domain.Entities;

namespace Notes.Application.Services
{
    public class AuthService(IUserStorage userStorage, IPasswordHasher passwordHasher, ITokenService tokenService) : IAuthService
    {
        private readonly IUserStorage _userStorage = userStorage;
        private readonly IPasswordHasher _passwordHasher = passwordHasher;
        private readonly ITokenService _tokenService = tokenService;
        public async Task<LoginResult> LoginAsync(string login, string password, CancellationToken cancellationToken = default)
        {
            var storagedUser = await _userStorage.GetUserByLoginAsync(login, cancellationToken);

            if (storagedUser == null)
                return new(false, LoginResult.ErrorCode.UserDoesNotExist, null);

            bool passwordCorrect = _passwordHasher.Verify(password, storagedUser.Password);

            if (!passwordCorrect)
                return new(false, LoginResult.ErrorCode.IncorrectPassword, null);

            AuthPairTokenModel? authPairToken = await _tokenService.IssueTokensAsync(storagedUser.Id, cancellationToken);

            if (authPairToken == null)
                return new(false, LoginResult.ErrorCode.UserDoesNotExist, null);

            return new(true, LoginResult.ErrorCode.AllGood, storagedUser.Id, authPairToken);
        }

        public async Task<RegisterResult> RegisterAsync(string login, string password, CancellationToken cancellationToken = default)
        {
            // Проверяем на наличие логина до хеширования
            var existingUser = await _userStorage.GetUserByLoginAsync(login, cancellationToken);
            if (existingUser != null)
                return new RegisterResult(false, RegisterResult.ErrorCode.UserAlreadyExist);

            string passwordHashed = _passwordHasher.Hash(password);
            User user = new(login, passwordHashed);

            var addingToDBResult = await _userStorage.AddUserAsync(user, cancellationToken);

            RegisterResult result;
            if (addingToDBResult)
                result = new(true, RegisterResult.ErrorCode.AllGood, user.Id);
            else
                result = new(false, RegisterResult.ErrorCode.UserAlreadyExist);

            return result;
        }
    }
}
