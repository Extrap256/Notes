using Moq;
using Notes.Application.Interfaces.Infrastructure;
using Notes.Application.Models;
using Notes.Application.Services;
using Notes.Domain;
using Notes.Domain.Entities;

namespace Notes.Application.Tests
{
    public class NoteSyncServiceTests
    {
        private const string Title = "Заголовок";
        private const string Content = "Содержимое";
        private const string NewTitle = "Новый заголовок";
        private const string NewContent = "Новое содержимое";

        private readonly Mock<INoteStorage> _noteStorage = new();
        private readonly Mock<IUserStorage> _userStorage = new();

        private readonly List<Note> _notes = new();

        private readonly NoteSyncService _sut;

        public NoteSyncServiceTests()
        {
            SetupStatefulNoteStorage();
            _sut = new NoteSyncService(_noteStorage.Object, _userStorage.Object);
        }

        // ---------- infrastructure ----------

        private void SetupStatefulNoteStorage()
        {
            _noteStorage
                .Setup(s => s.GetNoteAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid userId, Guid noteId, CancellationToken _) =>
                    _notes.FirstOrDefault(n => n.Id == noteId && n.UserId == userId));

            _noteStorage
                .Setup(s => s.GetNotesListAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid userId, CancellationToken _) =>
                    _notes.Where(n => n.UserId == userId && !n.IsDeleted).ToList());

            _noteStorage
                .Setup(s => s.GetDeletedNotesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid userId, CancellationToken _) =>
                    _notes.Where(n => n.UserId == userId && n.IsDeleted).ToList());

            _noteStorage
                .Setup(s => s.AddNoteAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()))
                .Callback((Note note, CancellationToken _) => _notes.Add(note))
                .Returns(Task.CompletedTask);

            _noteStorage
                .Setup(s => s.UpdateAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Note note, CancellationToken _) =>
                {
                    var index = _notes.FindIndex(n => n.Id == note.Id && n.UserId == note.UserId);
                    if (index < 0) return false;

                    _notes[index] = note;
                    return true;
                });
        }

        private Guid GivenExistingUser()
        {
            var user = new User("login", "password-hash");
            _userStorage
                .Setup(s => s.GetUserByIdAsync(user.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);
            return user.Id;
        }

        private Note GivenNote(
            Guid userId,
            string title = Title,
            string content = Content,
            int version = 1,
            bool isDeleted = false)
        {
            var note = new Note(userId, title, content, version, isDeleted);
            _notes.Add(note);
            return note;
        }

        private void GivenUpdateAlwaysFails()
        {
            _noteStorage
                .Setup(s => s.UpdateAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        }

        // ---------- CreateNoteAsync ----------

        [Fact]
        public async Task CreateNoteAsync_ValidData_ReturnsNoteWithVersionOne()
        {
            var userId = GivenExistingUser();

            var result = await _sut.CreateNoteAsync(userId, Title, Content);

            Assert.NotNull(result);
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(Title, result.Title);
            Assert.Equal(Content, result.Content);
            Assert.Equal(1, result.Version);
            Assert.False(result.IsDeleted);
        }

        [Fact]
        public async Task CreateNoteAsync_ValidData_PersistsNoteInStorage()
        {
            var userId = GivenExistingUser();

            var result = await _sut.CreateNoteAsync(userId, Title, Content);

            var stored = Assert.Single(_notes);
            Assert.Equal(result.Id, stored.Id);
            Assert.Equal(userId, stored.UserId);
            _noteStorage.Verify(
                s => s.AddNoteAsync(It.Is<Note>(n => n.Id == result.Id), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateNoteAsync_EmptyTitle_ThrowsDomainExceptionAndPersistsNothing()
        {
            var userId = GivenExistingUser();

            await Assert.ThrowsAsync<DomainException>(() => _sut.CreateNoteAsync(userId, "", Content));

            Assert.Empty(_notes);
            _noteStorage.Verify(
                s => s.AddNoteAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task CreateNoteAsync_PassesCancellationTokenToStorage()
        {
            var userId = GivenExistingUser();
            using var cts = new CancellationTokenSource();

            await _sut.CreateNoteAsync(userId, Title, Content, cts.Token);

            _noteStorage.Verify(
                s => s.AddNoteAsync(It.IsAny<Note>(), cts.Token),
                Times.Once);
        }

        // ---------- GetNoteAsync ----------

        [Fact]
        public async Task GetNoteAsync_ExistingNote_ReturnsNote()
        {
            var userId = GivenExistingUser();
            var seeded = GivenNote(userId);

            var result = await _sut.GetNoteAsync(userId, seeded.Id);

            Assert.NotNull(result);
            Assert.Equal(seeded.Id, result.Id);
            Assert.Equal(userId, result.UserId);
            Assert.Equal(Title, result.Title);
            Assert.Equal(Content, result.Content);
        }

        [Fact]
        public async Task GetNoteAsync_NonExistingNote_ReturnsNull()
        {
            var userId = GivenExistingUser();

            var result = await _sut.GetNoteAsync(userId, Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task GetNoteAsync_ForeignNote_ReturnsNull()
        {
            var owner = GivenExistingUser();
            var requester = GivenExistingUser();
            var seeded = GivenNote(owner);

            var result = await _sut.GetNoteAsync(requester, seeded.Id);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetNoteAsync_DeletedNote_ReturnsNote()
        {
            // сервис отдаёт заметку как есть, решение 404/409 принимает Api
            var userId = GivenExistingUser();
            var seeded = GivenNote(userId, isDeleted: true);

            var result = await _sut.GetNoteAsync(userId, seeded.Id);

            Assert.NotNull(result);
            Assert.True(result.IsDeleted);
        }

        [Fact]
        public async Task GetNoteAsync_PassesBothIdsAndCancellationTokenToStorage()
        {
            var userId = GivenExistingUser();
            var seeded = GivenNote(userId);
            using var cts = new CancellationTokenSource();

            await _sut.GetNoteAsync(userId, seeded.Id, cts.Token);

            _noteStorage.Verify(s => s.GetNoteAsync(userId, seeded.Id, cts.Token), Times.Once);
        }

        // ---------- UpdateNoteAsync ----------

        [Fact]
        public async Task UpdateNoteAsync_CorrectVersion_ReturnsSuccessWithUpdatedNote()
        {
            var userId = GivenExistingUser();
            var seeded = GivenNote(userId, version: 3);

            var result = await _sut.UpdateNoteAsync(userId, seeded.Id, NewTitle, NewContent, 3);

            Assert.NotNull(result);
            Assert.Equal(UpdateNoteResult.Verdict.Success, result.verdict);
            Assert.NotNull(result.Note);
            Assert.Equal(seeded.Id, result.Note.Id);
            Assert.Equal(NewTitle, result.Note.Title);
            Assert.Equal(NewContent, result.Note.Content);
            Assert.Equal(4, result.Note.Version);
            Assert.False(result.Note.IsDeleted);
        }

        [Fact]
        public async Task UpdateNoteAsync_CorrectVersion_PersistsNewValuesAndVersion()
        {
            var userId = GivenExistingUser();
            var seeded = GivenNote(userId, version: 3);

            await _sut.UpdateNoteAsync(userId, seeded.Id, NewTitle, NewContent, 3);

            var stored = Assert.Single(_notes);
            Assert.Equal(NewTitle, stored.Title);
            Assert.Equal(NewContent, stored.Content);
            Assert.Equal(4, stored.Version);
            Assert.Equal(userId, stored.UserId);
        }

        [Fact]
        public async Task UpdateNoteAsync_NonExistingNote_ReturnsNotFound()
        {
            var userId = GivenExistingUser();

            var result = await _sut.UpdateNoteAsync(userId, Guid.NewGuid(), NewTitle, NewContent, 1);

            Assert.Equal(UpdateNoteResult.Verdict.NotFound, result.verdict);
            Assert.Null(result.Note);
            _noteStorage.Verify(
                s => s.UpdateAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateNoteAsync_ForeignNote_ReturnsNotFoundAndKeepsOwnerNote()
        {
            var owner = GivenExistingUser();
            var attacker = GivenExistingUser();
            var seeded = GivenNote(owner, version: 1);

            var result = await _sut.UpdateNoteAsync(attacker, seeded.Id, NewTitle, NewContent, 1);

            Assert.Equal(UpdateNoteResult.Verdict.NotFound, result.verdict);
            Assert.Null(result.Note);

            var stored = Assert.Single(_notes);
            Assert.Equal(Title, stored.Title);
            Assert.Equal(owner, stored.UserId);
            Assert.Equal(1, stored.Version);
        }

        [Fact]
        public async Task UpdateNoteAsync_WrongVersion_ReturnsVersionConflictWithActualNote()
        {
            var userId = GivenExistingUser();
            var seeded = GivenNote(userId, version: 5);

            var result = await _sut.UpdateNoteAsync(userId, seeded.Id, NewTitle, NewContent, 3);

            Assert.Equal(UpdateNoteResult.Verdict.VersionConflict, result.verdict);

            // клиенту нужна актуальная серверная версия, чтобы разрешить конфликт
            Assert.NotNull(result.Note);
            Assert.Equal(5, result.Note.Version);
            Assert.Equal(Title, result.Note.Title);
            Assert.Equal(Content, result.Note.Content);

            _noteStorage.Verify(
                s => s.UpdateAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateNoteAsync_DeletedNote_ReturnsAlreadyDeleted()
        {
            var userId = GivenExistingUser();
            var seeded = GivenNote(userId, version: 2, isDeleted: true);

            var result = await _sut.UpdateNoteAsync(userId, seeded.Id, NewTitle, NewContent, 2);

            Assert.Equal(UpdateNoteResult.Verdict.AlreadyDeleted, result.verdict);
            Assert.NotNull(result.Note);
            Assert.True(result.Note.IsDeleted);
            Assert.Equal(2, result.Note.Version);
            Assert.Equal(Title, result.Note.Title);

            _noteStorage.Verify(
                s => s.UpdateAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateNoteAsync_DeletedNoteWithWrongVersion_ReturnsVersionConflict()
        {
            // Проверка версии — первый гейт (оптимистичная конкурентность).
            // Состояние "удалена" оценивается только если версия совпала.
            var userId = GivenExistingUser();
            var seeded = GivenNote(userId, version: 7, isDeleted: true);

            var result = await _sut.UpdateNoteAsync(userId, seeded.Id, NewTitle, NewContent, 1);

            Assert.Equal(UpdateNoteResult.Verdict.VersionConflict, result.verdict);

            // клиент всё равно видит, что заметка удалена — из возвращённой заметки
            Assert.NotNull(result.Note);
            Assert.True(result.Note.IsDeleted);
            Assert.Equal(7, result.Note.Version);
            Assert.Equal(Title, result.Note.Title);

            _noteStorage.Verify(
                s => s.UpdateAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateNoteAsync_StorageRejectsUpdate_ReturnsVersionConflict()
        {
            // между чтением и записью заметку изменил другой запрос
            var userId = GivenExistingUser();
            var seeded = GivenNote(userId, version: 1);
            GivenUpdateAlwaysFails();

            var result = await _sut.UpdateNoteAsync(userId, seeded.Id, NewTitle, NewContent, 1);

            Assert.Equal(UpdateNoteResult.Verdict.VersionConflict, result.verdict);
        }

        [Fact]
        public async Task UpdateNoteAsync_EmptyTitle_ThrowsDomainExceptionAndPersistsNothing()
        {
            var userId = GivenExistingUser();
            var seeded = GivenNote(userId, version: 1);

            await Assert.ThrowsAsync<DomainException>(
                () => _sut.UpdateNoteAsync(userId, seeded.Id, "", NewContent, 1));

            var stored = Assert.Single(_notes);
            Assert.Equal(Title, stored.Title);
            Assert.Equal(Content, stored.Content);
            Assert.Equal(1, stored.Version);

            _noteStorage.Verify(
                s => s.UpdateAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task UpdateNoteAsync_PassesCancellationTokenToStorage()
        {
            var userId = GivenExistingUser();
            var seeded = GivenNote(userId, version: 1);
            using var cts = new CancellationTokenSource();

            await _sut.UpdateNoteAsync(userId, seeded.Id, NewTitle, NewContent, 1, cts.Token);

            _noteStorage.Verify(s => s.GetNoteAsync(userId, seeded.Id, cts.Token), Times.Once);
            _noteStorage.Verify(s => s.UpdateAsync(It.IsAny<Note>(), cts.Token), Times.Once);
        }

        // ---------- DeleteAsync ----------

        [Fact]
        public async Task DeleteAsync_ExistingNote_ReturnsDeletedNoteWithBumpedVersion()
        {
            var userId = GivenExistingUser();
            var seeded = GivenNote(userId, version: 2);

            var result = await _sut.DeleteAsync(userId, seeded.Id);

            Assert.NotNull(result);
            Assert.Equal(seeded.Id, result.Id);
            Assert.True(result.IsDeleted);
            Assert.Equal(3, result.Version);
            Assert.Equal(Title, result.Title);
            Assert.Equal(Content, result.Content);
        }

        [Fact]
        public async Task DeleteAsync_ExistingNote_PersistsDeletionFlag()
        {
            var userId = GivenExistingUser();
            var seeded = GivenNote(userId);

            await _sut.DeleteAsync(userId, seeded.Id);

            var stored = Assert.Single(_notes);
            Assert.Equal(seeded.Id, stored.Id);
            Assert.True(stored.IsDeleted);

            _noteStorage.Verify(
                s => s.UpdateAsync(
                    It.Is<Note>(n => n.Id == seeded.Id && n.IsDeleted),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingNote_ReturnsNull()
        {
            var userId = GivenExistingUser();

            var result = await _sut.DeleteAsync(userId, Guid.NewGuid());

            Assert.Null(result);
            _noteStorage.Verify(
                s => s.UpdateAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ForeignNote_ReturnsNullAndKeepsOwnerNote()
        {
            var owner = GivenExistingUser();
            var attacker = GivenExistingUser();
            var seeded = GivenNote(owner);

            var result = await _sut.DeleteAsync(attacker, seeded.Id);

            Assert.Null(result);

            var stored = Assert.Single(_notes);
            Assert.False(stored.IsDeleted);
            Assert.Equal(owner, stored.UserId);
        }

        [Fact]
        public async Task DeleteAsync_AlreadyDeletedNote_ReturnsItUnchanged()
        {
            // идемпотентность: повторное удаление не меняет состояние и не бампает версию
            var userId = GivenExistingUser();
            var seeded = GivenNote(userId, version: 4, isDeleted: true);

            var result = await _sut.DeleteAsync(userId, seeded.Id);

            Assert.NotNull(result);
            Assert.True(result.IsDeleted);
            Assert.Equal(4, result.Version);

            _noteStorage.Verify(
                s => s.UpdateAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_StorageRejectsUpdate_ReturnsNull()
        {
            var userId = GivenExistingUser();
            var seeded = GivenNote(userId);
            GivenUpdateAlwaysFails();

            var result = await _sut.DeleteAsync(userId, seeded.Id);

            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteAsync_PassesCancellationTokenToStorage()
        {
            var userId = GivenExistingUser();
            var seeded = GivenNote(userId);
            using var cts = new CancellationTokenSource();

            await _sut.DeleteAsync(userId, seeded.Id, cts.Token);

            // сколько раз сервис читает заметку — его дело (Get → Update → Get)
            _noteStorage.Verify(
                s => s.GetNoteAsync(userId, seeded.Id, cts.Token),
                Times.AtLeastOnce);

            // а вот вызовов с чужим/пустым токеном быть не должно
            _noteStorage.Verify(
                s => s.GetNoteAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.Is<CancellationToken>(t => t != cts.Token)),
                Times.Never);

            _noteStorage.Verify(s => s.UpdateAsync(It.IsAny<Note>(), cts.Token), Times.Once);
        }

        // ---------- GetAllNotesByUserAsync ----------

        [Fact]
        public async Task GetAllNotesByUserAsync_ExistingUser_ReturnsOnlyOwnActiveNotes()
        {
            var userId = GivenExistingUser();
            var strangerId = GivenExistingUser();

            var active1 = GivenNote(userId, "Заметка 1");
            var active2 = GivenNote(userId, "Заметка 2");
            GivenNote(userId, "Удаленная", isDeleted: true);
            var foreign = GivenNote(strangerId, "Чужая");

            var result = await _sut.GetAllNotesByUserAsync(userId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, n => n.Id == active1.Id);
            Assert.Contains(result, n => n.Id == active2.Id);
            Assert.DoesNotContain(result, n => n.Id == foreign.Id);
        }

        [Fact]
        public async Task GetAllNotesByUserAsync_ExistingUserWithoutNotes_ReturnsEmptyList()
        {
            var userId = GivenExistingUser();

            var result = await _sut.GetAllNotesByUserAsync(userId);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllNotesByUserAsync_NonExistingUser_ReturnsNull()
        {
            var result = await _sut.GetAllNotesByUserAsync(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllNotesByUserAsync_NonExistingUser_DoesNotQueryNoteStorage()
        {
            await _sut.GetAllNotesByUserAsync(Guid.NewGuid());

            _noteStorage.Verify(
                s => s.GetNotesListAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task GetAllNotesByUserAsync_PassesCancellationTokenToStorages()
        {
            var userId = GivenExistingUser();
            using var cts = new CancellationTokenSource();

            await _sut.GetAllNotesByUserAsync(userId, cts.Token);

            _userStorage.Verify(s => s.GetUserByIdAsync(userId, cts.Token), Times.Once);
            _noteStorage.Verify(s => s.GetNotesListAsync(userId, cts.Token), Times.Once);
        }

        // ---------- GetAllDeletedAsync ----------

        [Fact]
        public async Task GetAllDeletedAsync_ExistingUser_ReturnsOnlyOwnDeletedNotes()
        {
            var userId = GivenExistingUser();
            var strangerId = GivenExistingUser();

            GivenNote(userId, "Активная");
            var deleted = GivenNote(userId, "Удаленная", isDeleted: true);
            GivenNote(strangerId, "Чужая удаленная", isDeleted: true);

            var result = await _sut.GetAllDeletedAsync(userId);

            Assert.NotNull(result);
            var single = Assert.Single(result);
            Assert.Equal(deleted.Id, single.Id);
            Assert.True(single.IsDeleted);
        }

        [Fact]
        public async Task GetAllDeletedAsync_ExistingUserWithoutDeletedNotes_ReturnsEmptyList()
        {
            var userId = GivenExistingUser();
            GivenNote(userId);

            var result = await _sut.GetAllDeletedAsync(userId);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllDeletedAsync_NonExistingUser_ReturnsNull()
        {
            var result = await _sut.GetAllDeletedAsync(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllDeletedAsync_PassesCancellationTokenToStorages()
        {
            var userId = GivenExistingUser();
            using var cts = new CancellationTokenSource();

            await _sut.GetAllDeletedAsync(userId, cts.Token);

            _userStorage.Verify(s => s.GetUserByIdAsync(userId, cts.Token), Times.Once);
            _noteStorage.Verify(s => s.GetDeletedNotesAsync(userId, cts.Token), Times.Once);
        }
    }
}