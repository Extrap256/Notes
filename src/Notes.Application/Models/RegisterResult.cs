namespace Notes.Application.Models
{
    public record RegisterResult
    {
        public enum ErrorCode
        {
            AllGood,
            ValidationError,
            UserAlreadyExist
        }

        public bool IsSuccess {  get; init; }
        public ErrorCode ErrorStatus { get; init; }
        public Guid? UserId { get; init; }

        public RegisterResult(bool isSuccess, ErrorCode code, Guid? userId = null)
        {
            IsSuccess = isSuccess;
            ErrorStatus = code;
            UserId = userId;
        }
    }
}
