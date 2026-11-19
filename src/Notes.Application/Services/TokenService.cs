using Notes.Application.Interfaces.Application;
using Notes.Application.Interfaces.Infrastructure;
using Notes.Application.Models;
using Notes.Domain.Entities;

namespace Notes.Application.Services
{
    public class TokenService(IUserStorage userStorage, ITokenStorage tokenStorage, ITokenGenerator tokenGenerator, ITokenHasher tokenHasher, IDateTimeProvider dateTimeProvider) : ITokenService
    {
        private readonly IUserStorage _userStorage = userStorage;
        private readonly ITokenStorage _tokenStorage = tokenStorage;
        private readonly ITokenGenerator _tokenGenerator = tokenGenerator;
        private readonly ITokenHasher _tokenHasher = tokenHasher;
        private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

        public async Task<AuthPairTokenModel?> IssueTokensAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            // Получаем пользователя
            var user = await _userStorage.GetUserByIdAsync(userId, cancellationToken);

            if (user == null)
                return null;

            // Генерируем новые токены
            RefreshTokenIssueModel refresh = _tokenGenerator.GenerateRefreshToken();
            AccessTokenModel access = _tokenGenerator.GenerateAccessToken(userId);

            RefreshTokenModel refreshToken = new(userId, _tokenHasher.Hash(refresh.Token), refresh.ExpiresAt, false);

            // Запись нового токена в бд
            bool result = await _tokenStorage.AddTokenAsync(refreshToken, cancellationToken);

            if (!result)
                return null;

            return new AuthPairTokenModel(access.token, access.ExpiresAt, refresh.Token, refresh.ExpiresAt);
        }

        public async Task<AuthPairTokenModel?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            // Старый токен
            var oldRefreshToken = await _tokenStorage.GetTokenByHashAsync(_tokenHasher.Hash(refreshToken), cancellationToken);

            if (oldRefreshToken == null)
                return null;

            if (oldRefreshToken.ExpiresAt <= _dateTimeProvider.UtcNow || oldRefreshToken.IsRevoked)
                return null;

            // Проверяем, что пользователь токена есть в системе (на случай кеширования и т.п.)
            var user = await _userStorage.GetUserByIdAsync(oldRefreshToken.UserId, cancellationToken);
            if (user == null)
                return null;

            // Отзываем старый
            var revokeResult = await _tokenStorage.RevokeAsync(oldRefreshToken.Hash, cancellationToken);

            if (revokeResult == null)
                return null;

            // Генерируем новые
            RefreshTokenIssueModel newRefresh = _tokenGenerator.GenerateRefreshToken();
            AccessTokenModel access = _tokenGenerator.GenerateAccessToken(oldRefreshToken.UserId);

            RefreshTokenModel newRefreshModel = new(oldRefreshToken.UserId, _tokenHasher.Hash(newRefresh.Token), newRefresh.ExpiresAt, false);

            // Добавляем новый в бд
            bool result = await _tokenStorage.AddTokenAsync(newRefreshModel, cancellationToken);

            if (!result)
                return null;

            return new AuthPairTokenModel(access.token, access.ExpiresAt, newRefresh.Token, newRefresh.ExpiresAt);
        }
    }
}
