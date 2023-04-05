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
using Microsoft.Extensions.Logging;

namespace TiCRateParser
{
    public interface IDatabaseInsert
    {
        void InsertProviderSection(Provider[] providers);
        void InsertRates(Rate[] rates, Guid reportingEntityId);
        void InsertReportingEntity(ReportingEntity entity);
        void CopyFromStage();
        void TruncateStage();
    }

    public class DatabaseInsert : IDatabaseInsert
    {
        private string connectionString;
        private ILogger logger;
        public DatabaseInsert(ILogger<DatabaseInsert> logger)
        {
            this.logger = logger;
            connectionString = "Server=localhost;Database=UH_POS;Trusted_Connection=True;";
        }

        public DatabaseInsert(ILogger<DatabaseInsert> logger, string connectionString)
        {
            this.logger = logger;
            this.connectionString = connectionString;
        }

        public void InsertReportingEntity(ReportingEntity entity)
        {
            try
            {
                using (var connection = new SqlConnection())
                {
                    connection.ConnectionString = connectionString;
                    if (connection.State != ConnectionState.Open)
                        connection.Open();
                    using (SqlCommand cmd = new SqlCommand("InsertReportingEntityRecord", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@entityName", SqlDbType.VarChar).Value = entity.Name;
                        cmd.Parameters.Add("@entityNameType", SqlDbType.VarChar).Value = entity.Type;
                        cmd.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = entity.Id;
                        cmd.Parameters.Add("@entityDate", SqlDbType.Date).Value = entity.LastUpdatedOn.ToDateTime(TimeOnly.MinValue);
                        cmd.ExecuteNonQuery();
                        logger.LogInformation($"ReportingEntity inserted successfully. ID = {entity.Id}, {entity.Name}, {entity.LastUpdatedOn}");
                        connection.Close();
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error writing Reporting Entity to DB");
            }
        }

        public void InsertProviderSection(Provider[] providers)
        {
            logger.LogInformation($"Inserting batch of {providers.Length} Providers");
            var providerTable = ProviderToDataTable(providers.DistinctBy(x => x.Id));
            BulkInsert(providerTable, "ProviderStage");
            var npiDataTable = NPIToDataTable(providers.DistinctBy(x => x.Id));
            BulkInsert(npiDataTable, "NPIStage");
        }

        public void CopyFromStage()
        {
            logger.LogInformation("Copying from Staging Tables");
            CopyRatesAndProviders();
            TruncateStage();
        }

        public void InsertRates(Rate[] rates, Guid reportingEntityId)
        {
            logger.LogInformation($"Inserting batch of {rates.Length} Rates");
            var table = RatesToDataTable(rates, reportingEntityId);
            BulkInsert(table, "RateStage");
        }

        private void CopyRatesAndProviders()
        {
            using (var connection = new SqlConnection())
            {
                connection.ConnectionString = connectionString;
                if (connection.State != ConnectionState.Open)
                    connection.Open();
                using (SqlCommand cmd = new SqlCommand("CopyRatesAndProviders", connection))
                {
                    cmd.CommandTimeout = 3600;
                    cmd.CommandType = CommandType.StoredProcedure;
                    int result = cmd.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }

        public void TruncateStage()
        {
            using (var connection = new SqlConnection())
            {
                connection.ConnectionString = connectionString;
                if (connection.State != ConnectionState.Open)
                    connection.Open();
                using (SqlCommand cmd = new SqlCommand("TruncateStage", connection))
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
                        logger.LogError(e, $"Error inserting {tableName} to database");
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
            logger.LogDebug("Converting TINs to table");
            DataTable table = new DataTable("ProviderStage");
            table.Columns.Add("Id", typeof(Guid));
            table.Columns.Add("Tin", typeof(string));
            table.Columns.Add("TinType", typeof(string));
            table.Columns.Add("ProviderReference", typeof(string));
            foreach (var p in providers)
            {
                DataRow row = table.NewRow();
                row["Id"] = p.Id;
                row["Tin"] = p.TIN;
                row["TinType"] = p.TinType;
                row["ProviderReference"] = p.ProviderReference;
                table.Rows.Add(row);
            }
            return table;
        }

        private DataTable NPIToDataTable(IEnumerable<Provider> providers)
        {
            logger.LogDebug("Converting NPI to table");
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

        private DataTable RatesToDataTable(Rate[] rates, Guid reportingEntityId)
        {
            DataTable table = new DataTable("RateStage");
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
            table.Columns.Add("ProviderReference", typeof(string));
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
                row["ReportingEntityId"] = reportingEntityId;
                row["ProviderReference"] = rate.ProviderReference;
                table.Rows.Add(row);
            }
            return table;
        }
    }

}
