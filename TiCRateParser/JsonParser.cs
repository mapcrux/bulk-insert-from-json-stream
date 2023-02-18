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

namespace TiCRateParser
{
    public static class JsonParser
    {
        private static string ratesFile = "C:\\temp\\rates.json";
        private static string providersFile = "C:\\temp\\providers.json";
        private static string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Rates;Integrated Security=True";
        private static Dictionary<string, Tuple<string,string>> descs = new Dictionary<string, Tuple<string,string>>();

        public static async Task Produce(ITargetBlock<Rate> target)
        {
            await using FileStream file = File.OpenRead(ratesFile);
            
            IAsyncEnumerable<JsonNode> enumerable = JsonSerializer.DeserializeAsyncEnumerable<JsonNode>(file);
            Console.WriteLine("Beginning Parsing Rates");
            try
            {
                await foreach (JsonNode node in enumerable)
                {
                    var short_desc = node?["name"]?.GetValue<string>()?.Truncate(50);
                    var long_desc = node?["description"]?.GetValue<string>()?.Truncate(300);
                    var negotiated_arrangement = node?["negotiation_arrangement"]?.GetValue<string>()?.Truncate(3);
                    var billing_code = node?["billing_code"]?.GetValue<string>()?.Truncate(7);
                    var billing_code_type_version_string = node?["billing_code_type_version"]?.GetValue<string>();
                    var billing_code_type_version = (string.IsNullOrEmpty(billing_code_type_version_string)) ? 0 : Convert.ToInt32(billing_code_type_version_string);
                    var billing_code_type = node?["billing_code_type"]?.GetValue<string>().Truncate(7);
                    var negotiated_rates_node = node?["negotiated_rates"];
                    if (!string.IsNullOrEmpty(billing_code) && negotiated_rates_node != null)
                    {
                        foreach (var rate_node in negotiated_rates_node.AsArray())
                        {
                            var provider_groups = rate_node?["provider_references"]?.AsArray();
                            if (provider_groups != null)
                            {
                                var pids = provider_groups.Select(x => x.GetValue<int>());
                                var negotiated_prices = rate_node?["negotiated_prices"]?.AsArray();
                                if (negotiated_prices != null)
                                {
                                    var prices = negotiated_prices.Select(x =>
                                    new
                                    {
                                        negotiated_type = x["negotiated_type"]?.GetValue<string>().Truncate(15),
                                        service_code = x["service_code"]?.AsArray().ToJsonString().Truncate(15),
                                        billing_code_modifier = x["billing_code_modifier"]?.AsArray().ToJsonString().Truncate(50),
                                        additional_information = x["additional_information"]?.GetValue<string>().Truncate(50),
                                        negotiated_rate = x["negotiated_rate"]?.GetValue<double>(),
                                        expiration_date = x["expiration_date"]?.GetValue<string>().ConvertDate(),
                                        billing_class = x["billing_class"]?.GetValue<string>().Truncate(15)
                                    });
                                    var rates = pids.SelectMany(t => prices, (t, p) => new Rate
                                    {
                                        BillingClass = p.billing_class,
                                        NegotiatedArrangement = negotiated_arrangement,
                                        BillingCode = billing_code,
                                        BillingCodeType = billing_code_type,
                                        BillingCodeTypeVersion = billing_code_type_version,
                                        ExpirationDate = p.expiration_date,
                                        NegotiatedRate = p.negotiated_rate,
                                        NegotiatedType = p.negotiated_type,
                                        BillingCodeModifier = p.billing_code_modifier,
                                        AdditionalInformation = p.additional_information,
                                        ProviderID = t
                                    });
                                    foreach (Rate rate in rates)
                                    {
                                        target.Post(rate);
                                    }
                                }
                                //descs[billing_code] = Tuple.Create(short_desc, long_desc);
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Encountered Parse Error");
            }
            Console.WriteLine("Finished Parsing Rates");
            target.Complete();
        }

        public static void Consume(Rate[] source)
        {
            Console.WriteLine($"Inserting batch of {source.Length} Rates");
            var table = RatesToDataTable(source);
            BulkInsert(table, "Rates");
        }

        public static void InsertCodes()
        {
            Console.WriteLine($"Inserting batch of Code Descriptions");
            DataTable table = new DataTable("Codes");
            table.Columns.Add("Code", typeof(string));
            table.Columns.Add("ShortDescription", typeof(string));
            table.Columns.Add("LongDescription", typeof(string));
            foreach (var c in descs)
            {
                DataRow row = table.NewRow();
                row["Code"] = c.Key;
                row["ShortDescription"] = c.Value.Item1;
                row["LongDescription"] = c.Value.Item2;
                table.Rows.Add(row);
            }
            BulkInsert(table, "Codes");
        }

        public static void BulkInsert(DataTable table, string tableName)
        {
            using (var connection = new SqlConnection())
            {
                connection.ConnectionString = connectionString;
                connection.Open();
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = tableName;
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

        public static DataTable TINToDataTable(IEnumerable<Provider> providers)
        {
            Console.WriteLine("Converting TINs to table");
            DataTable table = new DataTable("TIN");
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("Tin", typeof(string));
            table.Columns.Add("TinType", typeof(string));
            table.Columns.Add("ProviderId", typeof(int));
            foreach (var p in providers)
            {
                DataRow row = table.NewRow();
                row["Id"] = p.Id;
                row["Tin"] = p.TIN;
                row["TinType"] = p.TinType;
                row["ProviderId"] = p.ProviderID;
                table.Rows.Add(row);
            }
            return table;
        }

        public static DataTable NPIToDataTable(IEnumerable<Provider> providers)
        {
            Console.WriteLine("Converting NPI to table");
            DataTable table = new DataTable("NPI");
            table.Columns.Add("Npi", typeof(int));
            table.Columns.Add("TINId", typeof(Guid));
            foreach (var p in providers)
            {
                foreach (var n in p.NPIs)
                {
                    DataRow row = table.NewRow();
                    row["Npi"] = n;
                    row["TINId"] = p.Id;
                    table.Rows.Add(row);
                }
            }
            return table;
        }

        public static DataTable ProvidersToDataTable(IEnumerable<Provider> providers)
        {
            Console.WriteLine("Converting ProviderIds to table");
            DataTable table = new DataTable("Providers");
            table.Columns.Add("id", typeof(int));
            foreach (var p in providers.DistinctBy(c => c.ProviderID).Select(d => d.ProviderID))
            {
                DataRow row = table.NewRow();
                row["Id"] = p;
                table.Rows.Add(row);
            }
            return table;
        }

        public static DataTable RatesToDataTable(Rate[] rates)
        {
            DataTable table = new DataTable("Rates");
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("ProviderId", typeof(int));
            table.Columns.Add("BillingCode", typeof(string));
            table.Columns.Add("BillingCodeType", typeof(string));
            table.Columns.Add("BillingCodeTypeVersion", typeof(string));
            table.Columns.Add("NegotiatedType", typeof(string));
            table.Columns.Add("NegotiatedRate", typeof(double));
            table.Columns.Add("ExpirationDate", typeof(DateTime));
            table.Columns.Add("BillingClass", typeof(string));
            table.Columns.Add("BillingCodeModifier", typeof(string));
            table.Columns.Add("AdditionalInformation", typeof(string));
            foreach (var rate in rates)
            {
                DataRow row = table.NewRow();
                row["Id"] = Guid.NewGuid();
                row["ProviderId"] = rate.ProviderID;
                row["BillingCode"] = rate.BillingCode;
                row["BillingCodeType"] = rate.BillingCodeType;
                row["BillingCodeTypeVersion"] = rate.BillingCodeTypeVersion;
                row["NegotiatedType"] = rate.NegotiatedType;
                row["NegotiatedRate"] = rate.NegotiatedRate;
                row["ExpirationDate"] = rate.ExpirationDate;
                row["BillingClass"] = rate.BillingClass;
                row["BillingCodeModifier"] = rate.BillingCodeModifier;
                row["AdditionalInformation"] = rate.AdditionalInformation;
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
