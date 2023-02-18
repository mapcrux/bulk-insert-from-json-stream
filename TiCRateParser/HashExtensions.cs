using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TiCRateParser
{
    public static class HashExtensions
    {
        private static MD5 md5 = MD5.Create();
        public static Guid ComputeHash(this Provider provider)
        {
            var NPIs = string.Join('|', provider.NPIs);
            var providerString = $"{NPIs},{provider.TIN},{provider.TinType}";
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(providerString));
            return new Guid(hash);
        }
    }
}
