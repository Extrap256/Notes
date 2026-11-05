using Notes.Domain.Entities;

namespace Notes.Domain.Tests
{
    public class NoteTests
    {
        int titleMaxLength = 200;
        [Fact]
        public void Id_CreateNote_SetsGuidVersion4()
        {
            var user = new User("login", "password");

            string title = "MyTitle";
            string content = "Я сегодня позавтракал";
            var note = new Note(user, title, content, version: 1, isDeleted: false);

            Assert.Equal(4, note.Id.Version);
        }

        [Fact]
        public void Title_SetNumptyValue_ThrowDomainException()
        {
            var user = new User("Login", "Password");

            string title = "";
            string content = "Пора в магаз";

            var exception = Assert.Throws<DomainException>(() => new Note(user, title, content));

            Assert.Equal("Заголовок заметки не должен быть пустым", exception.Message);
        }

        [Fact]
        public void Title_SetBigValue_ThrowDomainException()
        {
            var user = new User("Login", "Password");

            string title = new string('a', 1000);
            string content = "Пора в магаз";

            var exception = Assert.Throws<DomainException>(() => new Note(user, title, content));

            Assert.Equal($"Длина заголовка заметки не может превышать {titleMaxLength} сиволов", exception.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData("Я пошел в магаз")]
        [InlineData("сделать новое задание:{ }")]
        [InlineData("сделать новое задание:{\n}")]
        public void Content_SetValidValues_SetSuccessfully(string validContent)
        {
            var user = new User("Login", "Password");
            string title = "Верный заголовок";

            var note = new Note(user, title, validContent);

            Assert.Equal(validContent, note.Content);
        }

        [Fact]
        public void IsDeketed_CreateNote_SetsFalse()
        {
            var user = new User("Login", "Password");

            string title = "Заголовок";
            string content = "Пора в магаз";

            var note = new Note(user, title, content);

            Assert.False(note.IsDeleted);
        }

        [Fact]
        public void CreatedAt_CreateNote_SetsNowUtc()
        {
            var user = new User("Login", "Password");

            string title = "Заголовок";
            string content = "Пора в магаз";

            DateTime start = DateTime.UtcNow;
            var note = new Note(user, title, content);
            DateTime end = DateTime.UtcNow;

            Assert.InRange(note.CreatedAt, start, end);
        }

        [Fact]
        public void UpdateAt_CreateNote_SetsNowUtc()
        {
            var user = new User("Login", "Password");

            string title = "Заголовок";
            string content = "Пора в магаз";

            DateTime start = DateTime.UtcNow;
            var note = new Note(user, title, content);
            DateTime end = DateTime.UtcNow;

            Assert.InRange(note.UpdatedAt, start, end);
        }

        [Fact]
        public void UpdateAt_UpdateNote_SetsNowUtc()
        {
            var user = new User("Login", "Password");

            string title = "Заголовок";
            string content = "Пора в магаз";

            var note = new Note(user, title, content);
            
            DateTime start = DateTime.UtcNow;
            note.Update(title, content);
            DateTime end = DateTime.UtcNow;

            Assert.InRange(note.UpdatedAt, start, end);
        }

        [Fact]
        public void Version_CreateNote_Sets1()
        {
            var user = new User("Login", "Password");

            string title = "Заголовок";
            string content = "Пора в магаз";

            var note = new Note(user, title, content);

            Assert.Equal(1, note.Version);
        }

        [Theory]
        [InlineData("", "Пошел в магаз", "Заголовок заметки не должен быть пустым")]
        [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "", "Длина заголовка заметки не может превышать 200 сиволов")]
        public void Update_UpdateWithInvalidValue_TrowsDomainException(string title, string content, string verdict)
        {
            var user = new User("Login", "Password");

            var note = new Note(user, "Title", "Content");
            var exception = Assert.Throws<DomainException>(() => note.Update(title, content));

            Assert.Equal(verdict, exception.Message);
        }

        [Fact]
        public void Update_UpdateWithValidValue_SetValues()
        {
            var user = new User("Login", "Password");
            string title = "Заголовок";
            string content = "Пошел в магаз";

            var note = new Note(user, "Title", "Content");
            note.Update(title, content);

            Assert.Multiple(
                () => Assert.Equal(title, note.Title),
                () => Assert.Equal(title, note.Title)
                );
        }
    }
}
