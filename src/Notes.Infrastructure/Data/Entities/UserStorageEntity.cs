namespace Notes.Infrastructure.Data.Entities
{
    public class UserStorageEntity
    {
        public Guid Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        
        public UserStorageEntity(Guid id, string login, string password)
        {
            Id = id;
            Login = login;
            Password = password;
        }
        // Конструтор для EF Core
        private UserStorageEntity()
        { }
    }
}
