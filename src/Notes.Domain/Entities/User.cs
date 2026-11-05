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

                field = value;
            }
        }
        public string Password { get; init; }

        public User(string login, string password)
        {
            Id = Guid.NewGuid();
            Login = login;
            Password = password;
        }
    }
}
