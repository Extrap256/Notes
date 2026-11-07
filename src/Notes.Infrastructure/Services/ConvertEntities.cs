using Notes.Application.DTO;
using Notes.Domain.Entities;
using Notes.Infrastructure.Data.Entities;

namespace Notes.Infrastructure.Services
{
    public class ConvertEntities
    {
        /// <summary>
        /// Преобразует сущность хранения заметки в доменную
        /// </summary>
        /// <param name="storageEntity">Исходная сущность хранения</param>
        /// <returns>Восстановленный Note</returns>
        public static Note ToDomainEntity(NoteStorageEntity storageEntity)
        {
            var restoreNote = Note.Reconstruct(storageEntity.Id,
                storageEntity.UserId,
                storageEntity.Title,
                storageEntity.Content,
                storageEntity.Version,
                storageEntity.DeletedAt != null);

            return restoreNote;
        }
        
        /// <summary>
        /// Преобразует доменную сущность заметки в сущность для хранения
        /// </summary>
        /// <param name="note">Доменная сущность заметок</param>
        /// <returns>Сущность для хранения заметки</returns>
        public static NoteStorageEntity ToStorageEntity(Note note)
        {
            DateTime? deletedAt = note.IsDeleted ? note.UpdatedAt : null;
            var storageNote = new NoteStorageEntity(note.Id,
                note.Title,
                note.Content,
                note.CreatedAt,
                note.UpdatedAt,
                deletedAt,
                note.Version,
                note.UserId);

            return storageNote;
        }

        public static void ApplyChanges(NoteStorageEntity target, Note sorce)
        {
            target.Id = sorce.Id;
            target.Title = sorce.Title;
            target.Content = sorce.Content;
            target.CreatedAt = sorce.CreatedAt;
            target.UpdatedAt = sorce.UpdatedAt;
            target.Version = sorce.Version;
            target.DeletedAt = sorce.IsDeleted ? sorce.UpdatedAt : null;
        }


        /// <summary>
        /// Преобразует сущность хранения пользователя в доменную
        /// </summary>
        /// <param name="storageEntity"></param>
        /// <returns>Восстановленный User</returns>
        public static User ToDomainEntity(UserStorageEntity storageEntity)
        {
            var restoreEntity = User.Reconstruct(storageEntity.Id, storageEntity.Login, storageEntity.Password);

            return restoreEntity;
        }

        /// <summary>
        /// Преобразует доменную сущность пользователя в сущность для хранения
        /// </summary>
        /// <param name="user">Доменная сущность</param>
        /// <returns>Сущность для хранения заметки</returns>
        public static UserStorageEntity ToStorageEntity(User user)
        {
            var storageUser = new UserStorageEntity(user.Id, user.Login, user.Password);

            return storageUser;
        }



        /// <summary>
        /// Преобразует сущность хранения пользователя в доменную
        /// </summary>
        /// <param name="storageEntity">Модель токена для хранения</param>
        /// <returns>Восстановленный refresh токен</returns>
        public static RefreshTokenModel ToApplicationEntity(RefreshTokenStorageEntity storageEntity)
        {
            bool isRevoked = storageEntity.RevokedAt != null;
            var restoreEntity = new RefreshTokenModel(storageEntity.UserId, storageEntity.Hash, storageEntity.ExpiresAt, isRevoked);

            return restoreEntity;
        }
    }
}
