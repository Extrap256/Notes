namespace Notes.Application.Models
{
    public record LoginResult
    {
        public enum ErrorCode
        {
            AllGood,
            UserDoesNotExist,
            IncorrectPassword
        }
        public bool IsSuccess { get; init; }
        public ErrorCode ErrorStatus {  get; init; }
        public Guid? UserId { get; init; }
        public AuthPairTokenModel? Tokens {  get; init; }
        public LoginResult(bool isSuccess, ErrorCode code, Guid? userId, AuthPairTokenModel? tokens = null)
        {
            IsSuccess = isSuccess;
            ErrorStatus = code;
            UserId = userId;
            Tokens = tokens;
        }
    }
}
