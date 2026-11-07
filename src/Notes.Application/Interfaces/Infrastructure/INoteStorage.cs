using Notes.Domain.Entities;

namespace Notes.Application.Interfaces.Infrastructure
{
    public interface INoteStorage
    {
        /// <summary>
        /// Находит заметки конкретного пользователя
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <returns>Список заметок</returns>
        public Task<List<Note>> GetNotesListAsync(Guid userId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Находит заметку по ее Id
        /// </summary>
        /// <param name="noteId">Id заметки</param>
        /// <returns>Объект заметки или null, если ее не существует</returns>
        public Task<Note?> GetNoteAsync(Guid noteId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Добавляет заметку
        /// </summary>
        /// <param name="note">Добавляемая заметка</param>
        public Task AddNoteAsync(Note note, CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновляет заметку
        /// </summary>
        /// <param name="noteId">Id существующей заметки</param>
        /// <param name="title">Новый заголовок</param>
        /// <param name="content">Новое содержимое</param>
        /// <returns>Версия новой заметки или null, если ее не существует</returns>
        public Task<int?> UpdateAsync(Guid noteId, string title, string content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Помечает существующую заметку как удаленную
        /// </summary>
        /// <param name="noteId">Id существующей заметки</param>
        /// <returns>true, если операция успешна. false, если заметка уже была удалена. null, если заметки не существует</returns>
        public Task<bool?> DeleteAsync(Guid noteId, CancellationToken cancellationToken = default);
    }
}
