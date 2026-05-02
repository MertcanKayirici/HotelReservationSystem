using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HotelReservationSystem.Application.Common
{
    /// <summary>
    /// Provides utility methods for generating deterministic hashes
    /// from request objects, typically used for idempotency validation.
    /// </summary>
    public static class RequestHashHelper
    {
        /// <summary>
        /// Computes a SHA256 hash from the serialized representation of an object.
        /// Ensures that identical request payloads produce the same hash value.
        /// </summary>
        /// <param name="obj">The input object to hash.</param>
        /// <returns>Hexadecimal string representation of the computed hash.</returns>
        public static string Compute(object obj)
        {
            // Serialize object into JSON to create a consistent input format
            var json = JsonSerializer.Serialize(obj);

            // Create SHA256 hashing algorithm instance
            using var sha = SHA256.Create();

            // Compute hash from UTF-8 encoded JSON string
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(json));

            // Convert hash bytes into a hexadecimal string representation
            return Convert.ToHexString(bytes); // .NET 5+
        }
    }
}