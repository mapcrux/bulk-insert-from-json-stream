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
    public static class Database
    {
        private static string connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Rates;Integrated Security=True";

        public static Guid InsertReportingEntity(string entityName, string entityType, DateOnly entityUpdateDate)
        {
            Guid id = Guid.NewGuid();
            using (var connection = new SqlConnection())
            {
                connection.ConnectionString = connectionString;
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("UpsertReportingEntityRecord", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@entityName", SqlDbType.VarChar).Value = entityName;
                    cmd.Parameters.Add("@entityNameType", SqlDbType.VarChar).Value =entityType;
                    cmd.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = id;
                    cmd.Parameters.Add("@entityDate", SqlDbType.Date).Value = entityUpdateDate;
                    connection.Open();
                    object o = cmd.ExecuteScalar();
                    if (o != null)
                    {
                        id = (Guid) o;
                        Console.WriteLine($"Record inserted successfully. ID = {id}" );
                    }
                    connection.Close();
                }
            }
            return id;
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
            table.Columns.Add("ProviderId", typeof(Guid));
            foreach (var p in providers)
            {
                DataRow row = table.NewRow();
                row["Id"] = p.Id;
                row["Tin"] = p.TIN;
                row["TinType"] = p.TinType;
                row["ProviderId"] = p.Id;
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
            table.Columns.Add("id", typeof(Guid));
            foreach (var p in providers.DistinctBy(c => c.ProviderID).Select(d => d.ProviderID))
            {
                DataRow row = table.NewRow();
                row["Id"] = p;
                table.Rows.Add(row);
            }
            return table;
        }

        public static DataTable RatesToDataTable(Rate[] rates, Guid EntityID)
        {
            DataTable table = new DataTable("Rates");
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("ProviderId", typeof(Guid));
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

}
