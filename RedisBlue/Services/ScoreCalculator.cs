using RedisBlue.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RedisBlue.Services
{
    internal class ScoreCalculator : IScoreCalculator
    {
        public double Calculate(object value)
        {
            return value switch
            {
                bool => (bool)value ? 1 : 0,
                int or long or decimal or float or double => Convert.ToDouble(value),
                DateTimeOffset => Convert.ToDouble(((DateTimeOffset)value).ToUnixTimeMilliseconds()),
                _ => Get64BitHash(Convert.ToString(value))
            };
        }

        public string Hash(object value)
        {
            return Get128BitHash(Convert.ToString(value));
        }

        private static double Get64BitHash(string strText)
        {
            if (string.IsNullOrEmpty(strText))
                return 0;

            byte[] byteContents = Encoding.Unicode.GetBytes(strText);
            byte[] hashText = MD5.HashData(byteContents);
            var hashCode = BitConverter.ToInt64(hashText, 0);

            for (var start = 8; start < hashText.Length; start += 8)
                hashCode ^= BitConverter.ToInt64(hashText, start);

            return Convert.ToDouble(hashCode);
        }

        private static string Get128BitHash(string strText)
        {
            if (string.IsNullOrEmpty(strText))
                return "null";

            byte[] byteContents = Encoding.Unicode.GetBytes(strText);
            byte[] hashText = MD5.HashData(byteContents);
            return Convert.ToBase64String(hashText);
        }
    }
}
