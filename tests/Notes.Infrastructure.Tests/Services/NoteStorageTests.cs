using Notes.Application.Exceptions;
using Notes.Domain.Entities;
using Notes.Infrastructure.Services;
using Notes.Infrastructure.Tests.Fixtures;

namespace Notes.Infrastructure.Tests.Services
{
    public class NoteStorageTests : DatabaseTestBase
    {
        private readonly NoteStorage _noteStorage;
        public NoteStorageTests(PostgresFixture fixture) : base(fixture)
        {
            _noteStorage = new NoteStorage(DbContext);
        }

        [Fact]
        public async Task AddNoteAsync_AddNewNote_AddNoteSuccessfully()
        {
            User user = new("login1", "password");
            await DbContext.Users.AddAsync(ConvertEntities.ToStorageEntity(user));

            Note note = new(user.Id, "Заголовок", "Содержимое");

            await _noteStorage.AddNoteAsync(note);

            var fromDb = await DbContext.Notes.FindAsync(note.Id);

            Assert.NotNull(fromDb);
        }


        [Fact]
        public async Task AddNoteAsync_AddOldNote_ThrowDuplicateException()
        {
            User user = new("login2", "password");
            await DbContext.Users.AddAsync(ConvertEntities.ToStorageEntity(user));

            Note note = new(user.Id, "Заголовок", "Содержимое");

            await _noteStorage.AddNoteAsync(note);
            var result = await Assert.ThrowsAsync<DuplicateException>(async () => await _noteStorage.AddNoteAsync(note));
        }

        [Fact]
        public async Task DeleteAsync_DeleteOldNote_ReturnTrue()
        {
            User user = new("login3", "password");
            await DbContext.Users.AddAsync(ConvertEntities.ToStorageEntity(user));
            await DbContext.SaveChangesAsync();

            var note = new Note(user.Id, "Заголовок", "Контект");
            var noteStorage = ConvertEntities.ToStorageEntity(note);
            await DbContext.Notes.AddAsync(noteStorage);
            await DbContext.SaveChangesAsync();

            var result = await _noteStorage.DeleteAsync(noteStorage.Id);

            Assert.NotNull(result);
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteAsync_DeleteDeletedNote_ReturnFalse()
        {
            User user = new("login4", "password");
            await DbContext.Users.AddAsync(ConvertEntities.ToStorageEntity(user));
            await DbContext.SaveChangesAsync();

            var note = new Note(user.Id, "Заголовок", "Контект", isDeleted: true);
            var noteStorage = ConvertEntities.ToStorageEntity(note);
            await DbContext.Notes.AddAsync(noteStorage);
            await DbContext.SaveChangesAsync();

            var result = await _noteStorage.DeleteAsync(noteStorage.Id);

            Assert.NotNull(result);
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_DeleteNewNote_ReturnNull()
        {
            User user = new("login5", "password");
            await DbContext.Users.AddAsync(ConvertEntities.ToStorageEntity(user));
            await DbContext.SaveChangesAsync();

            var note = new Note(user.Id, "Заголовок", "Контект", isDeleted: false);

            var result = await _noteStorage.DeleteAsync(note.Id);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetNoteAsync_ExistingNote_ReturnsNote()
        {
            User user = new("login6", "password");
            await DbContext.Users.AddAsync(ConvertEntities.ToStorageEntity(user));
            await DbContext.SaveChangesAsync();

            var note = new Note(user.Id, "Заголовок", "Содержимое");
            var noteStorage = ConvertEntities.ToStorageEntity(note);
            await DbContext.Notes.AddAsync(noteStorage);
            await DbContext.SaveChangesAsync();

            var result = await _noteStorage.GetNoteAsync(noteStorage.Id);

            Assert.NotNull(result);
            Assert.Equal(noteStorage.Id, result.Id);
            Assert.Equal("Заголовок", result.Title);
            Assert.Equal("Содержимое", result.Content);
            Assert.Equal(user.Id, result.UserId);
        }

        [Fact]
        public async Task GetNoteAsync_NonExistingNote_ReturnsNull()
        {
            var nonExistingId = Guid.NewGuid();
            var result = await _noteStorage.GetNoteAsync(nonExistingId);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetNoteAsync_DeletedNote_ReturnsNote()
        {
            User user = new("login7", "password");
            await DbContext.Users.AddAsync(ConvertEntities.ToStorageEntity(user));
            await DbContext.SaveChangesAsync();

            var note = new Note(user.Id, "Заголовок", "Содержимое", isDeleted: true);
            var noteStorage = ConvertEntities.ToStorageEntity(note);
            await DbContext.Notes.AddAsync(noteStorage);
            await DbContext.SaveChangesAsync();

            var result = await _noteStorage.GetNoteAsync(noteStorage.Id);

            Assert.NotNull(result);
            Assert.True(result.IsDeleted);
        }


        // UpdateAsync

        [Fact]
        public async Task UpdateAsync_ExistingNote_UpdatesNoteAndReturnsNewVersion()
        {
            User user = new("login8", "password");
            await DbContext.Users.AddAsync(ConvertEntities.ToStorageEntity(user));
            await DbContext.SaveChangesAsync();

            var note = new Note(user.Id, "Старый заголовок", "Старое содержимое");
            var noteStorage = ConvertEntities.ToStorageEntity(note);
            await DbContext.Notes.AddAsync(noteStorage);
            await DbContext.SaveChangesAsync();

            var oldVersion = noteStorage.Version;
            var result = await _noteStorage.UpdateAsync(noteStorage.Id, "Новый заголовок", "Новое содержимое");

            Assert.NotNull(result);
            Assert.Equal(oldVersion + 1, result);

            var updatedNote = await DbContext.Notes.FindAsync(noteStorage.Id);
            Assert.NotNull(updatedNote);
            Assert.Equal("Новый заголовок", updatedNote.Title);
            Assert.Equal("Новое содержимое", updatedNote.Content);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingNote_ReturnsNull()
        {
            var nonExistingId = Guid.NewGuid();
            var result = await _noteStorage.UpdateAsync(nonExistingId, "Заголовок", "Содержимое");

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateAsync_DeletedNote_UpdatesNote()
        {
            User user = new("login9", "password");
            await DbContext.Users.AddAsync(ConvertEntities.ToStorageEntity(user));
            await DbContext.SaveChangesAsync();

            var note = new Note(user.Id, "Заголовок", "Содержимое", isDeleted: true);
            var noteStorage = ConvertEntities.ToStorageEntity(note);
            await DbContext.Notes.AddAsync(noteStorage);
            await DbContext.SaveChangesAsync();

            var result = await _noteStorage.UpdateAsync(noteStorage.Id, "Обновленный заголовок", "Обновленное содержимое");

            Assert.NotNull(result);

            var updatedNote = await DbContext.Notes.FindAsync(noteStorage.Id);
            Assert.NotNull(updatedNote);
            Assert.Equal("Обновленный заголовок", updatedNote.Title);
            Assert.Equal("Обновленное содержимое", updatedNote.Content);
            Assert.NotNull(updatedNote.DeletedAt);
        }

        // GetNotesListAsync

        [Fact]
        public async Task GetNotesListAsync_UserHasNotes_ReturnsAllUserNotes()
        {
            User user1 = new("login10", "password");
            User user2 = new("login11", "password");
            await DbContext.Users.AddAsync(ConvertEntities.ToStorageEntity(user1));
            await DbContext.Users.AddAsync(ConvertEntities.ToStorageEntity(user2));
            await DbContext.SaveChangesAsync();

            var note1 = new Note(user1.Id, "Заметка 1", "Содержимое 1");
            var note2 = new Note(user1.Id, "Заметка 2", "Содержимое 2");
            var note3 = new Note(user2.Id, "Чужая заметка", "Чужое содержимое");

            await DbContext.Notes.AddAsync(ConvertEntities.ToStorageEntity(note1));
            await DbContext.Notes.AddAsync(ConvertEntities.ToStorageEntity(note2));
            await DbContext.Notes.AddAsync(ConvertEntities.ToStorageEntity(note3));
            await DbContext.SaveChangesAsync();

            var result = await _noteStorage.GetNotesListAsync(user1.Id);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, n => n.Id == note1.Id);
            Assert.Contains(result, n => n.Id == note2.Id);
            Assert.DoesNotContain(result, n => n.Id == note3.Id);
        }

        [Fact]
        public async Task GetNotesListAsync_UserHasNoNotes_ReturnsEmptyList()
        {
            User user = new("login12", "password");
            await DbContext.Users.AddAsync(ConvertEntities.ToStorageEntity(user));
            await DbContext.SaveChangesAsync();

            var result = await _noteStorage.GetNotesListAsync(user.Id);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetNotesListAsync_NonExistingUser_ReturnsEmptyList()
        {
            var nonExistingUserId = Guid.NewGuid();
            var result = await _noteStorage.GetNotesListAsync(nonExistingUserId);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetNotesListAsync_OnlyDeletedNotes_ReturnsEmptyList()
        {
            User user = new("login13", "password");
            await DbContext.Users.AddAsync(ConvertEntities.ToStorageEntity(user));
            await DbContext.SaveChangesAsync();

            var note = new Note(user.Id, "Удаленная заметка", "Содержимое", isDeleted: true);
            await DbContext.Notes.AddAsync(ConvertEntities.ToStorageEntity(note));
            await DbContext.SaveChangesAsync();

            var result = await _noteStorage.GetNotesListAsync(user.Id);

            Assert.NotNull(result);
            Assert.Single(result);
        }

    }
}
