using Microsoft.AspNetCore.Identity;
using Notes.Application.Interfaces.Infrastructure;

namespace Notes.Infrastructure.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        private readonly PasswordHasher<string> _hasher = new ();
        public string Hash(string password)
        {
            return _hasher.HashPassword(string.Empty, password);
        }

        public bool Verify(string password, string hash)
        {
            var result = _hasher.VerifyHashedPassword(string.Empty, hash, password);

            return result != PasswordVerificationResult.Failed;
        }
    }
}
