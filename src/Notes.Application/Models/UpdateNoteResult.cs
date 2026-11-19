using Notes.Domain.Entities;

namespace Notes.Application.Models
{
	public record UpdateNoteResult
	{
        public enum Verdict
        {
            Success,
            NotFound,
            VersionConflict,
            AlreadyDeleted
        }
        public Verdict verdict;
        public Note? Note;
    }
	
}
