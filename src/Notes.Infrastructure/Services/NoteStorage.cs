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

        public async Task<bool?> DeleteAsync(Guid noteId, CancellationToken cancellationToken = default)
        {
            var note = await _dbContext.Notes.FirstOrDefaultAsync(x => x.Id == noteId, cancellationToken);

            if (note == null)
                return null;

            if (note.DeletedAt != null)
                return false;

            var domainNote = ConvertEntities.ToDomainEntity(note);
            domainNote.Update(domainNote.Title, domainNote.Content, true);
            ConvertEntities.ApplyChanges(note, domainNote);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<Note?> GetNoteAsync(Guid noteId, CancellationToken cancellationToken = default)
        {
            NoteStorageEntity? noteStorage =  await _dbContext.Notes.FirstOrDefaultAsync(x => x.Id == noteId, cancellationToken);

            if (noteStorage == null)
                return null;

            var note = ConvertEntities.ToDomainEntity(noteStorage);

            return note;
        }

        public async Task<List<Note>> GetNotesListAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var notes = await _dbContext.Notes
                .Where(x => x.UserId == userId)
                .ToListAsync(cancellationToken);

            return notes.Select(x => ConvertEntities.ToDomainEntity(x)).ToList();
        }

        public async Task<int?> UpdateAsync(Guid noteId, string title, string content, CancellationToken cancellationToken = default)
        {
            var note = await _dbContext.Notes.FirstOrDefaultAsync(x => x.Id == noteId, cancellationToken);

            if (note == null)
                return null;

            var noteDomain = ConvertEntities.ToDomainEntity(note);
            noteDomain.Update(title, content);
            ConvertEntities.ApplyChanges(note, noteDomain);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return note.Version;
        }
    }
}
