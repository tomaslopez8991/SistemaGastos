using System.Security.Cryptography;
using System.Text;

namespace SistemaGastos.Application.Helpers;

public static class EmailConfirmationTokenHelper
{
    public static string CreateToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
