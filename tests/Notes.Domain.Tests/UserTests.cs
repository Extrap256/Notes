using Notes.Domain.Entities;

namespace Notes.Domain.Tests
{
    public class UserTests
    {
        int loginMinValue = 3;
        [Fact]
        public void Id_CreateUser_SetsGuidV4()
        {
            string login = "login";
            string password = "password";

            var user = new User(login, password);
            Assert.Equal(4, user.Id.Version);
        }

        [Fact]
        public void Login_SetInvalidValueLessMinimal_ThrowsDomainException()
        {
            string login = "Lo";
            string password = "Password";
            var exception = Assert.Throws<DomainException>(() =>
            {
                var user = new User(login, password);
            });
            Assert.Equal($"Число символов логина должно быть не меньше {loginMinValue}", exception.Message);
        }

        [Fact]
        public void Login_SetInvalidValueWithInnerSpaces_ThrowsDomainException()
        {
            string login = "Lo gin";
            string password = "Password";
            var exception = Assert.Throws<DomainException>(() =>
            {
                var user = new User(login, password);
            });
            Assert.Equal("В логине не должно быть пробелов", exception.Message);
        }
        [Fact]
        public void Password_SetEmptyValue_ThrowsDomainException()
        {
            string login = "Login";
            string password = "";
            var exception = Assert.Throws<DomainException>(() =>
            {
                var user = new User(login, password);
            });
            Assert.Equal("Пароль не должен быть пустым", exception.Message);
        }

        [Theory]
        [InlineData("MyNewLogin", "df")]
        [InlineData("Login", "passWord")]
        [InlineData("Log", "passWord")]
        public void Login_SetValidValue_SetsSuccessfully(string validLogin, string validPassword)
        {
            User user = new(validLogin, validPassword);

            Assert.Multiple(
                () => Assert.Equal(validLogin, user.Login),
                () => Assert.Equal(validPassword, user.Password),
                () => Assert.Equal([], user.Notes)
            );
        }
    }
}
