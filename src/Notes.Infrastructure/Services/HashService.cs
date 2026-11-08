using System.Security.Cryptography;
using System.Text;
using Notes.Application.Interfaces.Infrastructure;

namespace Notes.Infrastructure.Services
{
    public class HashService : IHashService
    {
        public string Hash(string payload)
        {
            byte[] source = Encoding.UTF8.GetBytes(payload);

            byte[] hash = SHA256.HashData(source);

            return Convert.ToHexString(hash);
        }
    }
}
