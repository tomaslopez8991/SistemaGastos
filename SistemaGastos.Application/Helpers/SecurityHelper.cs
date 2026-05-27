using System.Security.Cryptography;
using System.Text;

namespace SistemaGastos.Application.Helpers;

public static class SecurityHelper
{
    public static string HashPassword(string password)
    {
        using SHA256 sha256Hash = SHA256.Create();
        byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
        StringBuilder sb = new StringBuilder();
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}