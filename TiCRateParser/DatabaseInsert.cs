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
    public class DatabaseInsert
    {
        private string connectionString;
        public DatabaseInsert()
        {
            this.connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Rates;Integrated Security=True";
        }

        public DatabaseInsert(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public Guid InsertReportingEntity(string entityName, string entityType, DateOnly entityUpdateDate)
        {
            try
            {
                Guid id = Guid.NewGuid();
                using (var connection = new SqlConnection())
                {
                    connection.ConnectionString = connectionString;
                    if(connection.State != ConnectionState.Open)
                        connection.Open();
                    using (SqlCommand cmd = new SqlCommand("UpsertReportingEntityRecord", connection))
                    {
                        SqlParameter existingIdParameter = new SqlParameter("@existingId", SqlDbType.UniqueIdentifier) {Direction = ParameterDirection.Output};
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@entityName", SqlDbType.VarChar).Value = entityName;
                        cmd.Parameters.Add("@entityNameType", SqlDbType.VarChar).Value = entityType;
                        cmd.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = id;
                        cmd.Parameters.Add(existingIdParameter);
                        cmd.Parameters.Add("@entityDate", SqlDbType.Date).Value = entityUpdateDate.ToDateTime(TimeOnly.MinValue);
                        cmd.ExecuteNonQuery();
                        if(existingIdParameter.Value == null)
                        {
                            throw new Exception("Failed to add reporting entity to DB");
                        }
                        id = (Guid)existingIdParameter.Value;
                        Console.WriteLine($"ReportingEntity inserted successfully. ID = {id}");
                        connection.Close();
                    }
                }
                return id;
            }
            catch(Exception e)
            {
                Console.WriteLine("Error writing Reporting Entity to DB" + e.Message);
                throw;
            }
        }

        public void InsertProviderSection(IEnumerable<Provider> providers)
        {
            TruncateProviderStage();
            var providerTable = ProviderToDataTable(providers.DistinctBy(x=>x.Id));
            BulkInsert(providerTable, "ProviderStage");
            var npiDataTable = NPIToDataTable(providers.DistinctBy(x => x.Id));
            BulkInsert(npiDataTable, "NPIStage");
            CopyProviders();
            //TruncateProviderStage();
        }
        
        public void InsertRates(Rate[] rates, Guid entityId)
        {
            Console.WriteLine($"Inserting batch of {rates.Length} Rates");
            var table = RatesToDataTable(rates, entityId);
            BulkInsert(table, "Rates");
        }

        private void CopyProviders()
        {
            using (var connection = new SqlConnection())
            {
                connection.ConnectionString = connectionString;
                if (connection.State != ConnectionState.Open)
                    connection.Open();
                using (SqlCommand cmd = new SqlCommand("CopyProviders", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    int result = cmd.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }

        private void TruncateProviderStage()
        {
            using (var connection = new SqlConnection())
            {
                connection.ConnectionString = connectionString;
                if (connection.State != ConnectionState.Open)
                    connection.Open();
                using (SqlCommand cmd = new SqlCommand("TruncateProviderStage", connection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    int result = cmd.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }

        private void BulkInsert(DataTable table, string tableName)
        {
            using (var connection = new SqlConnection())
            {
                connection.ConnectionString = connectionString;
                if (connection.State != ConnectionState.Open)
                    connection.Open();
                using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection))
                {
                    bulkCopy.DestinationTableName = tableName;
                    bulkCopy.BulkCopyTimeout = 360;
                    try
                    {
                        bulkCopy.WriteToServer(table);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error inserting {tableName} to database: {e.Message}");
                        throw;
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }

        private DataTable ProviderToDataTable(IEnumerable<Provider> providers)
        {
            Console.WriteLine("Converting TINs to table");
            DataTable table = new DataTable("ProviderStage");
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("Tin", typeof(string));
            table.Columns.Add("TinType", typeof(string));
            foreach (var p in providers)
            {
                DataRow row = table.NewRow();
                row["Id"] = p.Id;
                row["Tin"] = p.TIN;
                row["TinType"] = p.TinType;
                table.Rows.Add(row);
            }
            return table;
        }

        private DataTable NPIToDataTable(IEnumerable<Provider> providers)
        {
            Console.WriteLine("Converting NPI to table");
            DataTable table = new DataTable("NPIStage");
            table.Columns.Add("Npi", typeof(int));
            table.Columns.Add("ProviderId", typeof(Guid));
            foreach (var p in providers)
            {
                foreach (var n in p.NPIs)
                {
                    DataRow row = table.NewRow();
                    row["Npi"] = n;
                    row["ProviderId"] = p.Id;
                    table.Rows.Add(row);
                }
            }
            return table;
        }

        private DataTable RatesToDataTable(Rate[] rates, Guid entityID)
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
            table.Columns.Add("ReportingEntityId", typeof(Guid));
            foreach (var rate in rates)
            {
                DataRow row = table.NewRow();
                row["Id"] = Guid.NewGuid();
                row["ProviderId"] = rate.Provider;
                row["BillingCode"] = rate.BillingCode;
                row["BillingCodeType"] = rate.BillingCodeType;
                row["BillingCodeTypeVersion"] = rate.BillingCodeTypeVersion;
                row["NegotiatedType"] = rate.NegotiatedType;
                row["NegotiatedRate"] = rate.NegotiatedRate;
                row["ExpirationDate"] = rate.ExpirationDate;
                row["BillingClass"] = rate.BillingClass;
                row["BillingCodeModifier"] = rate.BillingCodeModifier;
                row["AdditionalInformation"] = rate.AdditionalInformation;
                row["ReportingEntityId"] = entityID;
                table.Rows.Add(row);
            }
            return table;
        }
    }

}
