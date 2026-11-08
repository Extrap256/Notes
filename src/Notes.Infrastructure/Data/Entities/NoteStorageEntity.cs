namespace Notes.Infrastructure.Data.Entities
{
    public class NoteStorageEntity
    {
        public Guid Id {  get; set; }
        public string Title {  get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt {  get; set; }
        public int Version { get; set; }
        public Guid UserId { get; set; }

        public NoteStorageEntity(Guid id, string title, string content, DateTime createdAt, DateTime updateAt, DateTime? deletedAt, int version, Guid userId)
        {
            Id = id;
            Title = title;
            Content = content;
            CreatedAt = createdAt;
            UpdatedAt = updateAt;
            DeletedAt = deletedAt;
            Version = version;
            UserId = userId;
        }


        // Конструктор для EF Core
        private NoteStorageEntity()
        {

        }
    }
}
