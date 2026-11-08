using Microsoft.EntityFrameworkCore;
using Notes.Application.DTO;
using Notes.Application.Interfaces.Infrastructure;
using Notes.Infrastructure.Data;
using Notes.Infrastructure.Data.Entities;

namespace Notes.Infrastructure.Services
{
    public class TokenStorage(NotesServerDBContext context) : ITokenStorage
    {
        private readonly NotesServerDBContext _context = context;

        public async Task<bool> AddTokenAsync(RefreshTokenModel token, CancellationToken cancellationToken = default)
        {
            if (await _context.RefreshTokens.AnyAsync(x => x.Hash == token.Hash, cancellationToken))
                return false;

            var tokenStorage = new RefreshTokenStorageEntity(
                Guid.NewGuid(),
                token.UserId,
                token.Hash,
                DateTime.UtcNow,
                token.ExpiresAt,
                token.IsRevoked ? DateTime.UtcNow : null
                );

            await _context.RefreshTokens.AddAsync(tokenStorage, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<List<RefreshTokenModel>> GetAllActiveTokensAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            DateTime now = DateTime.UtcNow;
            var tokens = await _context.RefreshTokens.Where(x => x.UserId == userId && (x.RevokedAt == null && x.ExpiresAt > now)).ToListAsync(cancellationToken);

            return tokens.Select(x => ConvertEntities.ToApplicationEntity(x)).ToList();
        }

        public async Task<List<RefreshTokenModel>> GetAllTokensAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var tokens = await _context.RefreshTokens.Where(x => x.UserId == userId).ToListAsync(cancellationToken);

            return tokens.Select(x => ConvertEntities.ToApplicationEntity(x)).ToList();
        }

        public async Task<bool?> RevokeAsync(RefreshTokenModel token, CancellationToken cancellationToken = default)
        {
            var tokenStorage = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.Hash == token.Hash, cancellationToken);

            if (tokenStorage == null)
                return null;

            if (tokenStorage.RevokedAt != null)
                return false;

            tokenStorage.RevokedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
