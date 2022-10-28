using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static System.Net.Mime.MediaTypeNames;
using System.Xml;

namespace ConsoleApp3
{
    public static class JsonParser
    {
        public static async Task Produce(ITargetBlock<Rate> target)
        {
            await using FileStream file = File.OpenRead("C:\\Users\\mpelland\\source\\repos\\ConsoleApp3\\CMC_Transplant_MRRF.json");
            IAsyncEnumerable<JsonNode> enumerable = JsonSerializer.DeserializeAsyncEnumerable<JsonNode>(file);
            await foreach (JsonNode node in enumerable)
            {
                var billing_code = node?["billing_code"]?.GetValue<string>()?.Truncate(7);
                var billing_code_type_version_string = node?["billing_code_type_version"]?.GetValue<string>();
                var billing_code_type_version = (string.IsNullOrEmpty(billing_code_type_version_string)) ? 0 : Convert.ToInt32(billing_code_type_version_string);
                var billing_code_type = node?["billing_code_type"]?.GetValue<string>().Truncate(7);
                var negotiated_rates_node = node?["negotiated_rates"];
                if (!string.IsNullOrEmpty(billing_code) && negotiated_rates_node != null)
                {
                    foreach (var rate_node in negotiated_rates_node.AsArray())
                    {
                        var provider_groups = rate_node?["provider_groups"]?.AsArray();
                        if (provider_groups != null)
                        {
                            var tins = provider_groups.Select(x =>
                            new
                            {
                                tin = x["value"]?.GetValue<string>().Truncate(10),
                                tin_type = x["type"]?.GetValue<string>().Truncate(3)
                            });
                            var negotiated_prices = rate_node?["negotiated_prices"]?.AsArray();
                            if (negotiated_prices != null)
                            {
                                var prices = negotiated_prices.Select(x =>
                                new
                                {
                                    negotiated_type = x["negotiated_type"]?.GetValue<string>().Truncate(15),
                                    negotiated_rate = x["negotiated_rate"]?.GetValue<double>(),
                                    expiration_date = x["expiration_date"]?.GetValue<string>().ConvertDate(),
                                    billing_class = x["billing_class"]?.GetValue<string>().Truncate(15)
                                });
                                var rates = tins.SelectMany(t => prices, (t, p) => new Rate
                                {
                                    BillingClass = p.billing_class,
                                    BillingCode = billing_code,
                                    BillingCodeType = billing_code_type,
                                    BillingCodeTypeVersion = billing_code_type_version,
                                    ExpirationDate = p.expiration_date,
                                    NegotiatedRate = p.negotiated_rate,
                                    NegotiatedType = p.negotiated_type,
                                    TIN = t.tin,
                                    TinType = t.tin_type
                                });
                                foreach (Rate rate in rates)
                                {
                                    target.Post(rate);
                                }
                            }
                        }
                    }
                }
            }
            target.Complete();
        }

        public static void Consume(Rate[] source)
        {
            var table = RatesToTable(source);
            using (var connection = new SqlConnection())
            {
                connection.ConnectionString = "Server=(local);Database=testdb;Trusted_Connection=True;";
                connection.Open();
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = "Rates";
                    bulkCopy.BulkCopyTimeout = 360;
                    try
                    {
                        bulkCopy.WriteToServer(table);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }

        public static DataTable RatesToTable(Rate[] rates)
        {
            DataTable table = new DataTable("Rates");
            table.Columns.Add("Tin", typeof(string));
            table.Columns.Add("TinType", typeof(string));
            table.Columns.Add("BillingCode", typeof(string));
            table.Columns.Add("BillingCodeType", typeof(int));
            table.Columns.Add("BillingCodeTypeVersion", typeof(string));
            table.Columns.Add("NegotiatedType", typeof(string));
            table.Columns.Add("NegotiatedRate", typeof(double));
            table.Columns.Add("ExpirationDate", typeof(DateTime));
            table.Columns.Add("BillingClass", typeof(string));
            foreach (var rate in rates)
            {
                DataRow row = table.NewRow();
                row["Tin"] = rate.TIN;
                row["TinType"] = rate.TinType;
                row["BillingCode"] = rate.BillingCode;
                row["BillingCodeType"] = rate.BillingCodeType;
                row["BillingCodeTypeVersion"] = rate.BillingCodeTypeVersion;
                row["NegotiatedType"] = rate.NegotiatedType;
                row["NegotiatedRate"] = rate.NegotiatedRate;
                row["ExpirationDate"] = rate.ExpirationDate;
                row["BillingClass"] = rate.BillingClass;
                table.Rows.Add(row);
            }
            return table;
        }
    }

    public static class Extensions { 
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) { return value; }

            return value.Substring(0, Math.Min(value.Length, maxLength));
        }

        public static DateTime ConvertDate(this string value)
        {
            if (string.IsNullOrEmpty(value)) { return new DateTime(1900,1,1); }
            return Convert.ToDateTime(value);
        }
    }
}
