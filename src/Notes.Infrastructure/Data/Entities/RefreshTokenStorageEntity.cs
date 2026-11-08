namespace Notes.Infrastructure.Data.Entities
{
    public class RefreshTokenStorageEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Hash { get; set; }
        public DateTime CreatedAt {  get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt {  get; set; }

        public RefreshTokenStorageEntity(Guid id, Guid userId, string hash, DateTime createdAt, DateTime expiresAt, DateTime? revokedAt)
        {
            Id = id;
            UserId = userId;
            Hash = hash;
            CreatedAt = createdAt;
            ExpiresAt = expiresAt;
            RevokedAt = revokedAt;
        }

        // Конструктор для EF Core
        private RefreshTokenStorageEntity()
        { }
    }
}
