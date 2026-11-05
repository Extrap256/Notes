using Notes.Domain.Entities;

namespace Notes.Application.Interfaces
{
    public interface INoteStorage
    {
        /// <summary>
        /// Находит заметки конкретного пользователя
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <returns>Список заметок или null, если пользователь не найден</returns>
        public Task<List<Note>?> GetNotesList(Guid userId);
        
        /// <summary>
        /// Находит заметку по ее Id
        /// </summary>
        /// <param name="noteId">Id заметки</param>
        /// <returns>Объект заметки или null, если ее не существует</returns>
        public Task<Note?> GetNote(Guid noteId);

        /// <summary>
        /// Добавляет заметку
        /// </summary>
        /// <param name="note">Добавляемая заметка</param>
        public Task AddNote(Note note);

        /// <summary>
        /// Обновляет заметку
        /// </summary>
        /// <param name="noteId">Id существующей заметки</param>
        /// <param name="title">Новый заголовок</param>
        /// <param name="content">Новое содержимое</param>
        /// <returns>Версия новой заметки или null, если ее не существует</returns>
        public Task<int?> Update(Guid noteId, string title, string content);

        /// <summary>
        /// Помечает существующую заметку как удаленную
        /// </summary>
        /// <param name="noteId">Id существующей заметки</param>
        /// <returns>true, если операция успешна. false, если заметка уже была удалена. null, если заметки не существует</returns>
        public Task<bool?> Delete(Guid noteId);
    }
}
