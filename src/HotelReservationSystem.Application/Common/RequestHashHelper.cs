using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HotelReservationSystem.Application.Common
{
    public static class RequestHashHelper
    {
        public static string Compute(object obj)
        {
            var json = JsonSerializer.Serialize(obj);
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(json));
            return Convert.ToHexString(bytes); // .NET 5+
        }
    }
}