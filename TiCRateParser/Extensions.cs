using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace TiCRateParser
{
    public static class Extensions
    {
        private static MD5 md5 = MD5.Create();
        public static Guid ComputeHash(this Provider provider)
        {
            var NPIs = string.Join('|', provider.NPIs.OrderBy(x => x));
            var providerString = $"{NPIs},{provider.TIN},{provider.TinType}";
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(providerString));
            return new Guid(hash);
        }

        public static Guid ComputeHash(this IEnumerable<Provider> provider)
        {
            var providerString = string.Join('|', provider.Select(x => x.Id.ToString()).OrderBy(x => x));
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

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
            (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        public static string RemoveWhitespace(this string text)
        {
            return Regex.Replace(text, @"s", "");
        }

        public static bool EndsWithIgnoreCaseAndWhiteSpace(this string text, string comparison)
        {
            var newText = Regex.Replace(text, @"\s", "").ToLower();
            var newComparison = Regex.Replace(comparison, @"\s", "").ToLower();
            return newText.EndsWith(newComparison);
        }
    }
}
