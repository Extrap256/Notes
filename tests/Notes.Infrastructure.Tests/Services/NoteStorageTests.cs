using Notes.Application.Exceptions;
using Notes.Application.Interfaces.Infrastructure;
using Notes.Domain.Entities;
using Notes.Infrastructure.Services;
using Notes.Infrastructure.Tests.Fixtures;

namespace Notes.Infrastructure.Tests.Services
{
    public class NoteStorageTests : DatabaseTestBase
    {
        private readonly INoteStorage _noteStorage;

        public NoteStorageTests(PostgresFixture fixture) : base(fixture)
        {
            _noteStorage = new NoteStorage(DbContext);
        }

        // helpers

        private async Task<User> CreateUserAsync()
        {
            var user = new User($"user_{Guid.NewGuid():N}", "password");
            await DbContext.Users.AddAsync(ConvertEntities.ToStorageEntity(user));
            await DbContext.SaveChangesAsync();
            DbContext.ChangeTracker.Clear();
            return user;
        }

        private async Task<Note> SeedNoteAsync(
            Guid userId,
            string title = "Заголовок",
            string content = "Содержимое",
            bool isDeleted = false)
        {
            var note = new Note(userId, title, content, isDeleted: isDeleted);
            await DbContext.Notes.AddAsync(ConvertEntities.ToStorageEntity(note));
            await DbContext.SaveChangesAsync();
            DbContext.ChangeTracker.Clear();
            return note;
        }

        // AddNoteAsync

        [Fact]
        public async Task AddNoteAsync_NewNote_PersistsAllFields()
        {
            var user = await CreateUserAsync();
            var note = new Note(user.Id, "Заголовок", "Содержимое");

            await _noteStorage.AddNoteAsync(note);

            DbContext.ChangeTracker.Clear();
            var fromDb = await DbContext.Notes.FindAsync(note.Id);

            Assert.NotNull(fromDb);
            Assert.Equal(note.Id, fromDb.Id);
            Assert.Equal(user.Id, fromDb.UserId);
            Assert.Equal("Заголовок", fromDb.Title);
            Assert.Equal("Содержимое", fromDb.Content);
            Assert.Equal(1, fromDb.Version);
        }

        [Fact]
        public async Task AddNoteAsync_DuplicateId_ThrowsDuplicateException()
        {
            var user = await CreateUserAsync();
            var note = new Note(user.Id, "Заголовок", "Содержимое");

            await _noteStorage.AddNoteAsync(note);
            DbContext.ChangeTracker.Clear();

            await Assert.ThrowsAsync<DuplicateException>(() => _noteStorage.AddNoteAsync(note));
        }

        // GetNoteAsync
        [Fact]
        public async Task GetNoteAsync_ExistingNote_ReturnsNote()
        {
            var user = await CreateUserAsync();
            var seeded = await SeedNoteAsync(user.Id);

            var result = await _noteStorage.GetNoteAsync(user.Id, seeded.Id);

            Assert.NotNull(result);
            Assert.Equal(seeded.Id, result.Id);
            Assert.Equal("Заголовок", result.Title);
            Assert.Equal("Содержимое", result.Content);
            Assert.Equal(user.Id, result.UserId);
            Assert.Equal(1, result.Version);
            Assert.False(result.IsDeleted);
        }

        [Fact]
        public async Task GetNoteAsync_NonExistingNote_ReturnsNull()
        {
            var user = await CreateUserAsync();

            var result = await _noteStorage.GetNoteAsync(user.Id, Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task GetNoteAsync_NoteOfAnotherUser_ReturnsNull()
        {
            var owner = await CreateUserAsync();
            var stranger = await CreateUserAsync();
            var seeded = await SeedNoteAsync(owner.Id);

            var result = await _noteStorage.GetNoteAsync(stranger.Id, seeded.Id);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetNoteAsync_NonExistingUser_ReturnsNull()
        {
            var user = await CreateUserAsync();
            var seeded = await SeedNoteAsync(user.Id);

            var result = await _noteStorage.GetNoteAsync(Guid.NewGuid(), seeded.Id);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetNoteAsync_DeletedNote_ReturnsDeletedNote()
        {
            var user = await CreateUserAsync();
            var seeded = await SeedNoteAsync(user.Id, isDeleted: true);

            var result = await _noteStorage.GetNoteAsync(user.Id, seeded.Id);

            Assert.NotNull(result);
            Assert.True(result.IsDeleted);
        }

        [Fact]
        public async Task GetNoteAsync_ExistingNote_PreservesTimestamps()
        {
            var user = await CreateUserAsync();
            var seeded = await SeedNoteAsync(user.Id);

            var stored = await DbContext.Notes.FindAsync(seeded.Id);
            Assert.NotNull(stored);
            var expectedCreatedAt = stored.CreatedAt;
            var expectedUpdatedAt = stored.UpdatedAt;
            DbContext.ChangeTracker.Clear();

            var result = await _noteStorage.GetNoteAsync(user.Id, seeded.Id);

            Assert.NotNull(result);
            Assert.Equal(expectedCreatedAt, result.CreatedAt);
            Assert.Equal(expectedUpdatedAt, result.UpdatedAt);
        }

        // UpdateAsync

        [Fact]
        public async Task UpdateAsync_ExistingNote_PersistsNewValuesAndVersion()
        {
            var user = await CreateUserAsync();
            var seeded = await SeedNoteAsync(user.Id, "Старый заголовок", "Старое содержимое");

            var note = await _noteStorage.GetNoteAsync(user.Id, seeded.Id);
            Assert.NotNull(note);
            var oldVersion = note.Version;

            note.Update("Новый заголовок", "Новое содержимое");
            var result = await _noteStorage.UpdateAsync(note);

            Assert.True(result);

            DbContext.ChangeTracker.Clear();
            var fromDb = await DbContext.Notes.FindAsync(seeded.Id);

            Assert.NotNull(fromDb);
            Assert.Equal("Новый заголовок", fromDb.Title);
            Assert.Equal("Новое содержимое", fromDb.Content);
            // версию инкрементит домен, хранилище обязано просто сохранить её
            Assert.Equal(oldVersion + 1, fromDb.Version);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingNote_ReturnsFalse()
        {
            var user = await CreateUserAsync();
            var note = new Note(user.Id, "Заголовок", "Содержимое"); // в БД не клали

            var result = await _noteStorage.UpdateAsync(note);

            Assert.False(result);
        }

        [Fact]
        public async Task UpdateAsync_NoteOfAnotherUser_ReturnsFalseAndKeepsOwner()
        {
            var owner = await CreateUserAsync();
            var attacker = await CreateUserAsync();
            var seeded = await SeedNoteAsync(owner.Id, "Заголовок владельца", "Содержимое владельца");

            // подделанная сущность: тот же Id, но чужой UserId
            var forged = Note.Reconstruct(seeded.Id, attacker.Id, "Взлом", "Взлом", DateTime.UtcNow, DateTime.UtcNow, seeded.Version, false);

            var result = await _noteStorage.UpdateAsync(forged);

            Assert.False(result);

            DbContext.ChangeTracker.Clear();
            var ownerNote = await _noteStorage.GetNoteAsync(owner.Id, seeded.Id);
            Assert.NotNull(ownerNote);
            Assert.Equal("Заголовок владельца", ownerNote.Title);
            Assert.Equal(owner.Id, ownerNote.UserId);

            Assert.Null(await _noteStorage.GetNoteAsync(attacker.Id, seeded.Id));
        }

        [Fact]
        public async Task UpdateAsync_DeletedNote_UpdatesAndStaysDeleted()
        {
            var user = await CreateUserAsync();
            var seeded = await SeedNoteAsync(user.Id, isDeleted: true);

            var note = await _noteStorage.GetNoteAsync(user.Id, seeded.Id);
            Assert.NotNull(note);
            note.Update("Обновленный заголовок", "Обновленное содержимое");

            var result = await _noteStorage.UpdateAsync(note);

            Assert.True(result);

            DbContext.ChangeTracker.Clear();
            var fromDb = await DbContext.Notes.FindAsync(seeded.Id);

            Assert.NotNull(fromDb);
            Assert.Equal("Обновленный заголовок", fromDb.Title);
            Assert.Equal("Обновленное содержимое", fromDb.Content);
            Assert.NotNull(fromDb.DeletedAt);
        }

        [Fact]
        public async Task UpdateAsync_MarkAsDeleted_MovesNoteToDeletedList()
        {
            var user = await CreateUserAsync();
            var seeded = await SeedNoteAsync(user.Id);

            var note = await _noteStorage.GetNoteAsync(user.Id, seeded.Id);
            Assert.NotNull(note);
            note.Update(note.Title, note.Content, true);

            var result = await _noteStorage.UpdateAsync(note);

            Assert.True(result);

            DbContext.ChangeTracker.Clear();
            Assert.Empty(await _noteStorage.GetNotesListAsync(user.Id));

            var deleted = await _noteStorage.GetDeletedNotesAsync(user.Id);
            Assert.Single(deleted);
            Assert.Equal(seeded.Id, deleted[0].Id);
            Assert.True(deleted[0].IsDeleted);
        }

        [Fact]
        public async Task UpdateAsync_AlreadyDeletedNote_MarkDeletedAgain_ReturnsTrue()
        {
            // Идемпотентность удаления теперь решает Application,
            // хранилище просто сохраняет новое состояние.
            var user = await CreateUserAsync();
            var seeded = await SeedNoteAsync(user.Id, isDeleted: true);

            var note = await _noteStorage.GetNoteAsync(user.Id, seeded.Id);
            Assert.NotNull(note);
            var oldVersion = note.Version;
            note.Update(note.Title, note.Content, true);

            var result = await _noteStorage.UpdateAsync(note);

            Assert.True(result);

            DbContext.ChangeTracker.Clear();
            var fromDb = await DbContext.Notes.FindAsync(seeded.Id);
            Assert.NotNull(fromDb);
            Assert.NotNull(fromDb.DeletedAt);
            Assert.Equal(oldVersion + 1, fromDb.Version);
        }

        // GetNotesListAsync

        [Fact]
        public async Task GetNotesListAsync_UserHasNotes_ReturnsOnlyOwnNotes()
        {
            var user1 = await CreateUserAsync();
            var user2 = await CreateUserAsync();

            var note1 = await SeedNoteAsync(user1.Id, "Заметка 1", "Содержимое 1");
            var note2 = await SeedNoteAsync(user1.Id, "Заметка 2", "Содержимое 2");
            var foreign = await SeedNoteAsync(user2.Id, "Чужая заметка", "Чужое содержимое");

            var result = await _noteStorage.GetNotesListAsync(user1.Id);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, n => n.Id == note1.Id);
            Assert.Contains(result, n => n.Id == note2.Id);
            Assert.DoesNotContain(result, n => n.Id == foreign.Id);
        }

        [Fact]
        public async Task GetNotesListAsync_UserHasNoNotes_ReturnsEmptyList()
        {
            var user = await CreateUserAsync();

            var result = await _noteStorage.GetNotesListAsync(user.Id);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetNotesListAsync_NonExistingUser_ReturnsEmptyList()
        {
            var result = await _noteStorage.GetNotesListAsync(Guid.NewGuid());

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetNotesListAsync_OnlyDeletedNotes_ReturnsEmptyList()
        {
            var user = await CreateUserAsync();
            await SeedNoteAsync(user.Id, "Удаленная заметка", isDeleted: true);

            var result = await _noteStorage.GetNotesListAsync(user.Id);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetNotesListAsync_MixedNotes_ReturnsOnlyActive()
        {
            var user = await CreateUserAsync();
            var active = await SeedNoteAsync(user.Id, "Активная");
            await SeedNoteAsync(user.Id, "Удаленная", isDeleted: true);

            var result = await _noteStorage.GetNotesListAsync(user.Id);

            Assert.Single(result);
            Assert.Equal(active.Id, result[0].Id);
            Assert.False(result[0].IsDeleted);
        }

        // GetDeletedNotesAsync

        [Fact]
        public async Task GetDeletedNotesAsync_UserHasDeletedNotes_ReturnsOnlyOwnDeleted()
        {
            var user = await CreateUserAsync();
            var stranger = await CreateUserAsync();

            await SeedNoteAsync(user.Id, "Активная");
            var deleted = await SeedNoteAsync(user.Id, "Удаленная", isDeleted: true);
            await SeedNoteAsync(stranger.Id, "Чужая удаленная", isDeleted: true);

            var result = await _noteStorage.GetDeletedNotesAsync(user.Id);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(deleted.Id, result[0].Id);
            Assert.True(result[0].IsDeleted);
        }

        [Fact]
        public async Task GetDeletedNotesAsync_NoDeletedNotes_ReturnsEmptyList()
        {
            var user = await CreateUserAsync();
            await SeedNoteAsync(user.Id);

            var result = await _noteStorage.GetDeletedNotesAsync(user.Id);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetDeletedNotesAsync_NonExistingUser_ReturnsEmptyList()
        {
            var result = await _noteStorage.GetDeletedNotesAsync(Guid.NewGuid());

            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}