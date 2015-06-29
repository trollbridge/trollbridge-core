using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Web;
using Trollbridge.Common.PolicyInfo;

namespace Trollbridge.Common
{
    public static class SasTokenHelper
    {
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
    }
}
