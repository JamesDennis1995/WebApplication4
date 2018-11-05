using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;

namespace WebApplication4
{
    public class Salt
    {
        public static string Encode(string value, byte[] saltBytes)
        {
            if (saltBytes == null)
            {
                Random number = new Random();
                int size = number.Next(4, 8);
                saltBytes = new byte[size];
                RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
                rng.GetNonZeroBytes(saltBytes);
            }
            byte[] valueBytes = Encoding.UTF8.GetBytes(value);
            byte[] valueWithSaltBytes = new byte[valueBytes.Length + saltBytes.Length];
            for (int i = 0; i < valueBytes.Length; i++) valueWithSaltBytes[i] = valueBytes[i];
            for (int i = 0; i < saltBytes.Length; i++) valueWithSaltBytes[valueBytes.Length + i] = saltBytes[i];
            var hash = SHA512.Create();
            byte[] hashBytes = hash.ComputeHash(valueWithSaltBytes);
            byte[] hashWithSaltBytes = new byte[hashBytes.Length + saltBytes.Length];
            for (int i = 0; i < hashBytes.Length; i++) hashWithSaltBytes[i] = hashBytes[i];
            for (int i = 0; i < saltBytes.Length; i++) hashWithSaltBytes[hashBytes.Length + i] = saltBytes[i];
            return Convert.ToBase64String(hashWithSaltBytes);
        }
        public static bool Verify(string value, string hashed)
        {
            byte[] hashWithSaltBytes = Convert.FromBase64String(hashed);
            if (hashWithSaltBytes.Length < 64) return false;
            byte[] saltBytes = new byte[hashWithSaltBytes.Length - 64];
            for (int i = 0; i < saltBytes.Length; i++) saltBytes[i] = hashWithSaltBytes[64 + i];
            string expectedHash = Encode(value, saltBytes);
            return (hashed == expectedHash);
        }
    }
    public class Item
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public float Cost { get; set; }
        public float Total { get; set; }
        public Item ()
        {

        }
    }
}