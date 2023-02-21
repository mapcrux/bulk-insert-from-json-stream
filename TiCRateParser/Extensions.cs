using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TiCRateParser
{
    public static class Extensions
    {
        private static MD5 md5 = MD5.Create();
        public static Guid ComputeHash(this Provider provider)
        {
            var NPIs = string.Join('|', provider.NPIs);
            var providerString = $"{NPIs},{provider.TIN},{provider.TinType}";
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(providerString));
            return new Guid(hash);
        }

        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) { return value; }

            return value.Substring(0, Math.Min(value.Length, maxLength));
        }

        public static DateTime ConvertDate(this string value)
        {
            if (string.IsNullOrEmpty(value)) { return new DateTime(1900, 1, 1); }
            return Convert.ToDateTime(value);
        }
    }
}
