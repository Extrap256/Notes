using Notes.Domain.Entities;

namespace Notes.Application.Interfaces.Application
{
    public interface INoteSyncService
    {
        /// <summary>
        /// Создает новую заметку
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <param name="title">Заголовок</param>
        /// <param name="content">Содержание заметки</param>
        /// <returns>Объект созданной заметки</returns>
        public Task<Note> CreateNote(Guid userId, string title, string content);

        /// <summary>
        /// Обновляет существующую заметку
        /// </summary>
        /// <param name="noteId">Id заметки</param>
        /// <param name="title">Новый заголовок</param>
        /// <param name="content">Новое содержание заметки</param>
        /// <returns>Обновленный объект заметки. null, если такой заметки не существует</returns>
        public Task<Note?> UpdateNote(Guid noteId, string title, string content);

        /// <summary>
        /// Возвращает список заметок пользователя
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <returns>Лист заметок пользователя или null, если пользователя не существует</returns>
        public Task<List<Note>?> GetAllNotesByUser(Guid userId);

        /// <summary>
        /// Выдает заметку по id
        /// </summary>
        /// <param name="noteId">Id заметки</param>
        /// <returns>Объект заметки или null, если заметки не существует</returns>
        public Task<Note?> GetNote(Guid noteId);

        /// <summary>
        /// Удаляет заметку по id
        /// </summary>
        /// <param name="noteId">Id заметки</param>
        /// <returns>true, если заметка удалена. false, если заметка уже была удалена. null, если заметки не существует</returns>
        public Task<bool?> Delete(Guid noteId);

        /// <summary>
        /// Возвращает список всех удаленных заметок пользователя
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <returns>Список заметок или null, если пользователь не найден</returns>
        public Task<List<Note>?> GetAllDeleted(Guid userId);
    }
}
