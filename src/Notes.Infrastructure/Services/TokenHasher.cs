using System.Security.Cryptography;
using System.Text;
using Notes.Application.Interfaces.Infrastructure;

namespace Notes.Infrastructure.Services
{
    public class TokenHasher : ITokenHasher
    {
        private readonly SHA256 _hasher = SHA256.Create();
        public string Hash(string token)
        {

            byte[] tokenBytes = Encoding.UTF8.GetBytes(token);

            byte[] buffer = _hasher.ComputeHash(tokenBytes);

            return Convert.ToBase64String(buffer);
        }
    }
}
