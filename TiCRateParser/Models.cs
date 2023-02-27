using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace TiCRateParser
{

    public class Provider
    {
        public string TIN { get; set; }
        public string TinType { get; set; }
        public IEnumerable<int> NPIs { get; set; }
        public readonly Guid Id;
        public Provider(string tin, string tintype, IEnumerable<int> npis)
        {
            this.TIN = tin;
            this.TinType = tintype;
            this.NPIs = npis;
            Id = this.ComputeHash();
        }
    }

    public class ProviderGroup
    {
        public IEnumerable<Provider> providers;
        public readonly Guid Id;
        public ProviderGroup(IEnumerable<Provider> providers)
        {
            this.providers = providers;
            Id = providers.ComputeHash();
        }
    }

    public class Rate
    {
        public Guid Provider { get; set; }
        public string BillingCode { get; set; }
        public string BillingCodeType { get; set; }
        public int BillingCodeTypeVersion { get; set; }
        public string NegotiatedType { get; set; }
        public double? NegotiatedRate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string BillingClass { get; set; }
        public string NegotiatedArrangement { get; set; }
        public string AdditionalInformation { get; set; }
        public string BillingCodeModifier { get; set; }

    }
}
