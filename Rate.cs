using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace BulkInsertFromJsonStream
{
    public class Rate
    {
        public string TIN { get; set; }
        public string TinType { get; set; }
        public string BillingCode { get; set; }
        public string BillingCodeType { get; set; }
        public int BillingCodeTypeVersion { get; set; }
        public string NegotiatedType { get; set; }
        public double? NegotiatedRate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string BillingClass { get; set; }

    }
}
