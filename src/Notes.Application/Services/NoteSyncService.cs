using Notes.Application.Interfaces.Application;
using Notes.Application.Interfaces.Infrastructure;
using Notes.Application.Models;
using Notes.Domain.Entities;

namespace Notes.Application.Services
{
    public class NoteSyncService(INoteStorage noteStorage, IUserStorage userStorage) : INoteSyncService
    {
        private readonly INoteStorage _noteStorage = noteStorage;
        private readonly IUserStorage _userStorage = userStorage;
        public async Task<Note> CreateNoteAsync(Guid userId, string title, string content, CancellationToken cancellationToken = default)
        {
            Note note = new(userId, title, content);

            await _noteStorage.AddNoteAsync(note, cancellationToken);

            return note;
        }

        public async Task<Note?> DeleteAsync(Guid userId, Guid noteId, CancellationToken cancellationToken = default)
        {
            Note? note = await _noteStorage.GetNoteAsync(userId, noteId, cancellationToken);

            if (note == null)
                return null;

            if (note.IsDeleted)
                return note;

            note.Update(note.Title, note.Content, true);

            var updatResult = await _noteStorage.UpdateAsync(note, cancellationToken);

            if (!updatResult)
                return null;

            var updateNote = await _noteStorage.GetNoteAsync(userId, noteId, cancellationToken);

            if (note == null)
                return null;

            return updateNote;
        }

        public async Task<List<Note>?> GetAllDeletedAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userStorage.GetUserByIdAsync(userId, cancellationToken);

            if (user == null)
                return null;

            var deletedList = await _noteStorage.GetDeletedNotesAsync(userId, cancellationToken);

            return deletedList;
        }

        public async Task<List<Note>?> GetAllNotesByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userStorage.GetUserByIdAsync(userId, cancellationToken);

            if (user == null)
                return null;

            var notes = await _noteStorage.GetNotesListAsync(userId, cancellationToken);

            return notes;
        }

        public async Task<Note?> GetNoteAsync(Guid userId, Guid noteId, CancellationToken cancellationToken = default)
        {
            return await _noteStorage.GetNoteAsync(userId, noteId, cancellationToken);
        }

        public async Task<UpdateNoteResult> UpdateNoteAsync(Guid userId, Guid noteId, string title, string content, int ExpectedVersion, CancellationToken cancellationToken = default)
        {
            Note? note = await _noteStorage.GetNoteAsync(userId, noteId, cancellationToken);

            if (note == null)
                return new UpdateNoteResult() { verdict = UpdateNoteResult.Verdict.NotFound, Note = null};

            if (note.Version != ExpectedVersion)
                return new UpdateNoteResult() { verdict = UpdateNoteResult.Verdict.VersionConflict, Note = note };

            if (note.IsDeleted)
                return new UpdateNoteResult() { verdict = UpdateNoteResult.Verdict.AlreadyDeleted, Note = note };

            note.Update(title, content, false);

            var updateResult = await _noteStorage.UpdateAsync(note, cancellationToken);

            if (!updateResult)
                return new UpdateNoteResult() { verdict = UpdateNoteResult.Verdict.VersionConflict, Note = null };

            return new UpdateNoteResult() { verdict = UpdateNoteResult.Verdict.Success, Note = note };
        }
    }
}
