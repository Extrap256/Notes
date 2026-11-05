namespace Notes.Domain.Entities
{
    public class User
    {
        const int _loginMinLength = 3;
        public Guid Id {get; init; }
        public string Login
        {
            get;
            init
            {
                if (value.Length < _loginMinLength)
                    throw new DomainException($"Число символов логина должно быть не меньше {_loginMinLength}");
                if (value.Contains(' '))
                    throw new DomainException("В логине не должно быть пробелов");

                field = value;
            }
        }
        public string Password
        {
            get;
            init
            {
                if (value.Length == 0)
                    throw new DomainException("Пароль не должен быть пустым");

                field = value;
            }
        }
        public List<Note> Notes { get; set; }
        public User(string login, string password)
        {
            Id = Guid.NewGuid();
            Login = login;
            Password = password;
            Notes = new();
        }
    }
}
