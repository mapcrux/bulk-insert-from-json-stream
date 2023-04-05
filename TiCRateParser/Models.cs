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
    public class RateSection
    {
        public List<Rate> rates = new List<Rate>();
        public List<Provider> providers = new List<Provider>(); 
    }

    public class ReportingEntity
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public DateOnly LastUpdatedOn { get; set; }
        public Guid Id { get; }
        public ReportingEntity()
        {
            Id = Guid.NewGuid();
        }
    }

    public class Provider
    {
        public string TIN { get; set; }
        public string TinType { get; set; }
        public IEnumerable<int> NPIs { get; set; }
        public string? ProviderReference { get; set; }
        public readonly Guid Id;
        public Provider(string tin, string tintype, IEnumerable<int> npis, string? providerReference = null)
        {
            TIN = tin;
            TinType = tintype;
            NPIs = npis;
            ProviderReference = providerReference;
            Id = this.ComputeHash();
        }
    }

    public class Rate
    {
        public Guid Provider { get; set; }
        public Guid ReportingEntity { get; set; }
        public string BillingCode { get; set; }
        public string BillingCodeType { get; set; }
        public string BillingCodeTypeVersion { get; set; }
        public string NegotiatedType { get; set; }
        public double? NegotiatedRate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string BillingClass { get; set; }
        public string NegotiatedArrangement { get; set; }
        public string AdditionalInformation { get; set; }
        public string BillingCodeModifier { get; set; }
        public string ProviderReference { get; set; }

    }
}
