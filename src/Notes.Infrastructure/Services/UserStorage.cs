using Microsoft.EntityFrameworkCore;
using Notes.Application.Interfaces.Infrastructure;
using Notes.Domain.Entities;
using Notes.Infrastructure.Data;

namespace Notes.Infrastructure.Services
{
    class UserStorage (NotesServerDBContext context) : IUserStorage
    {
        private readonly NotesServerDBContext _dbContext = context;

        public async Task<bool> AddUserAsync(User user, CancellationToken cancellationToken = default)
        {
            if (await _dbContext.Users.AnyAsync(x => x.Id == user.Id || x.Login == user.Login, cancellationToken))
                return false;

            var userStorage = ConvertEntities.ToStorageEntity(user);
            await _dbContext.Users.AddAsync(userStorage, cancellationToken);

            return true;
        }

        public async Task<User?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

            if (user == null)
                return null;

            return ConvertEntities.ToDomainEntity(user);
        }

        public Task<bool?> UpdateUserAsync(Guid userId, string passwordHash, CancellationToken cancellationToken = default)
        {
            // Надо переделать доменную сущность
            throw new NotImplementedException();
        }
    }
}
