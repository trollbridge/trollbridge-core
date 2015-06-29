using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;
using Trollbridge.Core.PolicyInfo;

namespace Trollbridge.Core
{
    public static class SasTokenHelper
    {
        private static bool _firstTime = true;

        public static List<PolicyInfoBlob> BlobPolicies = new List<PolicyInfoBlob>();
        public static List<PolicyInfoQueue> QueuePolicies = new List<PolicyInfoQueue>();
        public static List<PolicyInfoEventHub> EventHubPolicies = new List<PolicyInfoEventHub>();

        public static void RefreshCache()
        {
            string connStr = connStr = Utilities.GetAppSettingsStringValue("DatabaseConnectionString");

            try
            {
                Retry.ExecuteRetryAction(() =>
                {
                    using (SqlConnection con = new SqlConnection(connStr))
                    {
                        con.Open();
                        SqlCommand cmd = new SqlCommand("dbo.GetAzureStoragePolicies", con);
                        cmd.CommandType = CommandType.StoredProcedure;

                        SqlDataReader sdr = cmd.ExecuteReader();

                        BlobPolicies.Clear();
                        QueuePolicies.Clear();
                        EventHubPolicies.Clear();

                        // First Recordset if for Azure Blob Policies
                        // PolicyId, PolicyIdentifier, ContainerName, AzureStorageConnectionStr, Write, Read, Delete, MinToExpire
                        while (sdr.Read())
                        {
                            PolicyInfoBlob info = new PolicyInfoBlob();
                            info.CustomerId = sdr.GetInt32(sdr.GetOrdinal("CustomerId"));
                            info.SiteId = sdr.GetInt32(sdr.GetOrdinal("SiteId"));
                            info.PolicyId = sdr.GetInt32(sdr.GetOrdinal("PolicyId"));
                            info.PolicyIdentifier = sdr.GetString(sdr.GetOrdinal("PolicyIdentifier"));
                            info.ContainerName = sdr.GetString(sdr.GetOrdinal("ContainerName"));
                            info.StorageConnectionString = sdr.GetString(sdr.GetOrdinal("AzureStorageConnectionStr"));
                            info.DateExpires = DateTime.UtcNow.AddMinutes(sdr.GetInt32(sdr.GetOrdinal("MinToExpire")));

                            bool canWrite = sdr.GetBoolean(sdr.GetOrdinal("Write"));
                            bool canRead = sdr.GetBoolean(sdr.GetOrdinal("Read"));
                            bool canDelete = sdr.GetBoolean(sdr.GetOrdinal("Delete"));

                            info.BlobPermissions = SharedAccessBlobPermissions.None;
                            if (canWrite) info.BlobPermissions = SharedAccessBlobPermissions.Write | info.BlobPermissions;
                            if (canRead) info.BlobPermissions = SharedAccessBlobPermissions.Read | info.BlobPermissions;
                            if (canDelete) info.BlobPermissions = SharedAccessBlobPermissions.Delete | info.BlobPermissions;

                            if (SharedAccessBlobPermissions.None == info.BlobPermissions) continue;
                            BlobPolicies.Add(info);
                        }
                        sdr.NextResult();

                        // PolicyId, PolicyIdentifier, QueueName, AzureStorageConnectionStr, Add, Read, Process, Update, MinToExpire
                        while (sdr.Read())
                        {
                            PolicyInfoQueue info = new PolicyInfoQueue();
                            info.CustomerId = sdr.GetInt32(sdr.GetOrdinal("CustomerId"));
                            info.SiteId = sdr.GetInt32(sdr.GetOrdinal("SiteId"));
                            info.PolicyId = sdr.GetInt32(sdr.GetOrdinal("PolicyId"));
                            info.PolicyIdentifier = sdr.GetString(sdr.GetOrdinal("PolicyIdentifier"));
                            info.QueueName = sdr.GetString(sdr.GetOrdinal("QueueName"));
                            info.StorageConnectionString = sdr.GetString(sdr.GetOrdinal("AzureStorageConnectionStr"));
                            info.DateExpires = DateTime.UtcNow.AddMinutes(sdr.GetInt32(sdr.GetOrdinal("MinToExpire")));

                            bool canAdd = sdr.GetBoolean(sdr.GetOrdinal("Add"));
                            bool canRead = sdr.GetBoolean(sdr.GetOrdinal("Read"));
                            bool canProcess = sdr.GetBoolean(sdr.GetOrdinal("Process"));
                            bool canUpdate = sdr.GetBoolean(sdr.GetOrdinal("Update"));

                            info.SAQueuePermissions = SharedAccessQueuePermissions.None;

                            if (canAdd) info.SAQueuePermissions = SharedAccessQueuePermissions.Add | info.SAQueuePermissions;
                            if (canRead) info.SAQueuePermissions = SharedAccessQueuePermissions.Read | info.SAQueuePermissions;
                            if (canProcess) info.SAQueuePermissions = SharedAccessQueuePermissions.ProcessMessages | info.SAQueuePermissions;
                            if (canUpdate) info.SAQueuePermissions = SharedAccessQueuePermissions.Update | info.SAQueuePermissions;

                            if (SharedAccessQueuePermissions.None == info.SAQueuePermissions) continue;
                            QueuePolicies.Add(info);
                        }
                        sdr.NextResult();

                        // CustomerId, SiteId, PolicyId, PolicyIdentifier, EventHubConnectionStr, Manage, Send, Listen, MinToExpire
                        while (sdr.Read())
                        {
                            PolicyInfoEventHub info = new PolicyInfoEventHub();
                            info.CustomerId = sdr.GetInt32(sdr.GetOrdinal("CustomerId"));
                            info.SiteId = sdr.GetInt32(sdr.GetOrdinal("SiteId"));
                            info.PolicyId = sdr.GetInt32(sdr.GetOrdinal("PolicyId"));
                            info.EventHubName = sdr.GetString(sdr.GetOrdinal("EventHubName"));
                            info.PolicyIdentifier = sdr.GetString(sdr.GetOrdinal("PolicyIdentifier"));
                            info.EventHubConnectionStr = sdr.GetString(sdr.GetOrdinal("EventHubConnectionStr"));
                            info.MinToExpire = sdr.GetInt32(sdr.GetOrdinal("MinToExpire"));
                            info.DateExpires = DateTime.UtcNow.AddMinutes(info.MinToExpire);

                            info.Manage = sdr.GetBoolean(sdr.GetOrdinal("Manage"));
                            info.Send = sdr.GetBoolean(sdr.GetOrdinal("Send"));
                            info.Listen = sdr.GetBoolean(sdr.GetOrdinal("Listen"));

                            //info.SAQueuePermissions = SharedAccessQueuePermissions.None;

                            //if (canAdd) info.SAQueuePermissions = SharedAccessQueuePermissions.Add | info.SAQueuePermissions;
                            //if (canRead) info.SAQueuePermissions = SharedAccessQueuePermissions.Read | info.SAQueuePermissions;
                            //if (canProcess) info.SAQueuePermissions = SharedAccessQueuePermissions.ProcessMessages | info.SAQueuePermissions;
                            //if (canUpdate) info.SAQueuePermissions = SharedAccessQueuePermissions.Update | info.SAQueuePermissions;

                            //if (SharedAccessQueuePermissions.None == info.SAQueuePermissions) continue;

                            if (!info.Manage && !info.Send && !info.Listen) continue;
                            EventHubPolicies.Add(info);
                        }

                        sdr.Close();
                    }
                });
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(Process.GetCurrentProcess().ProcessName, ex.ToString(), EventLogEntryType.Error);
            }
        }

        public static TokenHolder GetTokensUncompressed(string deviceIdentification)
        {
            StringBuilder results = new StringBuilder();

            string compressed = Utilities.CompressString(deviceIdentification);
            TokenHolder tokenHolder = GetTokensCompressed(compressed);

            foreach (var queue in tokenHolder.AzureQueues)
            {
                queue.SasToken = Utilities.DecompressString(queue.SasToken);
            }

            foreach (var storage in tokenHolder.AzureStorage)
            {
                storage.SasToken = Utilities.DecompressString(storage.SasToken);
            }

            foreach (var eventHub in tokenHolder.AzureEventHubs)
            {
                eventHub.SasToken = Utilities.DecompressString(eventHub.SasToken);
            }

            return tokenHolder;
        }

        public static TokenHolder GetTokensCompressed(string deviceIdentification)
        {
            if (_firstTime)
            {
                RefreshCache();
                _firstTime = false;
            }

            string[] args = Utilities.DecompressString(deviceIdentification).Split('_');
            int customerId = Convert.ToInt32(args[0]);
            int siteId = Convert.ToInt32(args[1]);
            string macAddress = args[2];

            TokenHolder holder = new TokenHolder();
            holder.AzureStorage = new List<ShareAccessToken>();
            holder.AzureQueues = new List<ShareAccessToken>();
            holder.AzureEventHubs = new List<ShareAccessToken>();
            DateTime start = holder.EarliestTokenExpires = DateTime.Now;

            List<int> blobPolicies = new List<int>();
            List<int> queuePolicies = new List<int>();
            List<int> eventHubPolicies = new List<int>();

            string connStr = Utilities.GetAppSettingsStringValue("DatabaseConnectionString");

            try
            {
                Retry.ExecuteRetryAction(() =>
                {
                    using (SqlConnection con = new SqlConnection(connStr))
                    {
                        con.Open();
                        SqlCommand cmd = new SqlCommand("dbo.GetStoragePolicyMapForServer", con);
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add(Utilities.GetSqlParameter("@CustomerId", customerId, SqlDbType.Int));
                        cmd.Parameters.Add(Utilities.GetSqlParameter("@SiteId", siteId, SqlDbType.Int));
                        cmd.Parameters.Add(Utilities.GetSqlParameter("@MacAddress", macAddress, SqlDbType.NVarChar));

                        SqlDataReader sdr = cmd.ExecuteReader();

                        // First Recordset is for Azure Blob Policies
                        while (sdr.Read())
                        {
                            int id = sdr.GetInt32(sdr.GetOrdinal("PolicyId"));
                            blobPolicies.Add(id);
                        }
                        sdr.NextResult();

                        // Second Recordset is for Azure Queues
                        while (sdr.Read())
                        {
                            int id = sdr.GetInt32(sdr.GetOrdinal("PolicyId"));
                            queuePolicies.Add(id);
                        }
                        sdr.NextResult();

                        // Third Recordset is for Azure Event Hubs
                        while (sdr.Read())
                        {
                            int id = sdr.GetInt32(sdr.GetOrdinal("PolicyId"));
                            eventHubPolicies.Add(id);
                        }
                        sdr.Close();
                        con.Close();
                    }
                });
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry(Process.GetCurrentProcess().ProcessName, ex.ToString(), EventLogEntryType.Error);
            }

            foreach (int id in blobPolicies)
            {
                foreach (PolicyInfoBlob info in BlobPolicies)
                {
                    if (id == info.PolicyId && customerId == info.CustomerId && siteId == info.SiteId)
                    {
                        ShareAccessToken token = new ShareAccessToken();
                        token.SasToken = info.SASToken;
                        token.DateExpires = info.DateExpires;
                        token.Uri = info.ContainerUri;
                        token.Name = info.ContainerName;
                        if (holder.EarliestTokenExpires > info.DateExpires || holder.EarliestTokenExpires == start) holder.EarliestTokenExpires = info.DateExpires;
                        holder.AzureStorage.Add(token);
                        break;
                    }
                }
            }

            foreach (int id in queuePolicies)
            {
                foreach (PolicyInfoQueue info in QueuePolicies)
                {
                    if (id == info.PolicyId && customerId == info.CustomerId && siteId == info.SiteId)
                    {
                        ShareAccessToken token = new ShareAccessToken();
                        token.SasToken = info.SASToken;
                        token.DateExpires = info.DateExpires;
                        token.Uri = info.QueueUri;
                        token.Name = info.QueueName;
                        if (holder.EarliestTokenExpires > info.DateExpires || holder.EarliestTokenExpires == start) holder.EarliestTokenExpires = info.DateExpires;
                        holder.AzureQueues.Add(token);
                        break;
                    }
                }
            }

            foreach (int id in eventHubPolicies)
            {
                foreach (PolicyInfoEventHub info in EventHubPolicies)
                {
                    if (id == info.PolicyId && customerId == info.CustomerId && siteId == info.SiteId)
                    {
                        ShareAccessToken token = new ShareAccessToken();
                        token.SasToken = info.SASToken;
                        token.DateExpires = info.DateExpires;
                        token.Uri = info.EventHubUri;
                        token.Name = info.EventHubName;
                        if (holder.EarliestTokenExpires > info.DateExpires || holder.EarliestTokenExpires == start) holder.EarliestTokenExpires = info.DateExpires;
                        holder.AzureEventHubs.Add(token);
                        break;
                    }
                }
            }

            return holder;
        }
    }
}
