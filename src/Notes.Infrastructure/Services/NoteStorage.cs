using Notes.Application.Interfaces.Infrastructure;
using Notes.Domain.Entities;
using Notes.Infrastructure.Data;
using Notes.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Notes.Application.Exceptions;

namespace Notes.Infrastructure.Services
{
    public class NoteStorage(NotesServerDBContext dbContext) : INoteStorage
    {
        private readonly NotesServerDBContext _dbContext = dbContext;

        /// <summary>
        /// Проверяет наличие заметки по текущему id
        /// </summary>
        /// <param name="noteId">id проверяемой заметки</param>
        /// <returns>true, если сущестует. false, если отсутствует</returns>
        private async Task<bool> IsExist(Guid noteId, CancellationToken ct = default)
        {
            return await _dbContext.Notes.AnyAsync(x => x.Id == noteId, ct);
        }

        public async Task AddNoteAsync(Note note, CancellationToken cancellationToken = default)
        {
            NoteStorageEntity storageNote = ConvertEntities.ToStorageEntity(note);

            if (await IsExist(storageNote.Id, cancellationToken))
                throw new DuplicateException($"Заметка с id: {storageNote.Id} уже существует");

            _dbContext.Notes.Add(storageNote);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<Note?> GetNoteAsync(Guid userId, Guid noteId, CancellationToken cancellationToken = default)
        {
            NoteStorageEntity? noteStorage =  await _dbContext.Notes.FirstOrDefaultAsync(x => x.Id == noteId && x.UserId == userId, cancellationToken);

            if (noteStorage == null)
                return null;

            var note = ConvertEntities.ToDomainEntity(noteStorage);

            return note;
        }

        public async Task<List<Note>> GetNotesListAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var notes = await _dbContext.Notes
                .Where(x => x.UserId == userId && x.DeletedAt == null)
                .ToListAsync(cancellationToken);

            return notes.Select(x => ConvertEntities.ToDomainEntity(x)).ToList();
        }

        public async Task<bool> UpdateAsync(Note note, CancellationToken cancellationToken = default)
        {
            var noteFromStorage = await _dbContext.Notes.FirstOrDefaultAsync(x => x.Id == note.Id && x.UserId == note.UserId, cancellationToken);

            if (noteFromStorage == null)
                return false;

            ConvertEntities.ApplyChanges(noteFromStorage, note);

            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<List<Note>> GetDeletedNotesAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var notesFromStorage = _dbContext.Notes.Where(x => x.UserId == userId && x.DeletedAt != null);

            return await notesFromStorage.Select(x => ConvertEntities.ToDomainEntity(x))
                                         .ToListAsync(cancellationToken);

        }
    }
}
