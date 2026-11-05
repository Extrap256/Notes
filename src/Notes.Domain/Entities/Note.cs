namespace Notes.Domain.Entities
{
    public class Note
    {
        private const int _titleMaxLength = 200;
        public Guid Id { get; init; }
        public string Title
        {
            get;
            set
            {
                if (value.Length == 0)
                    throw new DomainException("Заголовок заметки не должен быть пустым");
                if (value.Length > _titleMaxLength)
                    throw new DomainException($"Длина заголовка заметки не может превышать {_titleMaxLength} сиволов");

                field = value;
            }
        }
        public string Content { get; set; }
        public bool IdDeleted { get; set; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedUp
        {
            get;
            set
            {
                if (value < CreatedAt)
                    throw new DomainException("Время изменения заметки не может быть раньше ее создания");

                field = value;
            }
        }
        public int Version
        {
            get;
            set
            {
                if (value < 1)
                    throw new DomainException("Версия заметки должна быть положительной");

                field = value;
            }
        }

        public User Owner { get; init; }

        public Note(User author, string title, string content, int version = 1, bool isDeleted = false)
        {
            Id = Guid.NewGuid();
            Owner = author;
            Title = title;
            Content = content;
            IdDeleted = isDeleted;
            CreatedAt = DateTime.UtcNow;
            UpdatedUp = DateTime.UtcNow;
            Version = version;
        }
    }
}
