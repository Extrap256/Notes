namespace Notes.Domain.Entities
{
    public class Note
    {
        private const int _titleMaxLength = 200;
        public Guid Id { get; init; }
        public string Title
        {
            get;
            private set
            {
                if (value.Length == 0)
                    throw new DomainException("Заголовок заметки не должен быть пустым");
                if (value.Length > _titleMaxLength)
                    throw new DomainException($"Длина заголовка заметки не может превышать {_titleMaxLength} сиволов");

                field = value;
            }
        }
        public string Content { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedUp { get; private set; }
        public int Version { get; private set; }

        public User Owner { get; init; }

        public Note(User author, string title, string content, int version = 1, bool isDeleted = false)
        {
            Id = Guid.NewGuid();
            Owner = author;
            Title = title;
            Content = content;
            IsDeleted = isDeleted;
            CreatedAt = DateTime.UtcNow;
            UpdatedUp = DateTime.UtcNow;
            Version = version;
        }

        public void Update(string newTitle, string newContent)
        {
            Title = newTitle;
            Content = newContent;
            UpdatedUp = DateTime.UtcNow;
            Version += 1;
        }
        public void Update(string newTitle, string newContent, bool isDeleted)
        {
            Title = newTitle;
            Content = newContent;
            IsDeleted = isDeleted;
            UpdatedUp = DateTime.UtcNow;
            Version += 1;
        }
    }
}
