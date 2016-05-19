using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace pa3class
{
    public class storage {

        public static void createQueues() {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue adminqueue = queueClient.GetQueueReference("adminqueue");
            CloudQueue xmlqueue = queueClient.GetQueueReference("xmlqueue");
            CloudQueue htmlqueue = queueClient.GetQueueReference("urlqueue");
            adminqueue.CreateIfNotExists();
            xmlqueue.CreateIfNotExists();
            htmlqueue.CreateIfNotExists();
        }

        public void createTable() {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("urltable");
            table.CreateIfNotExists();
        }

        public static string calculateHash(string input) {
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < hash.Length; i++) {
                sb.Append(hash[i].ToString("x2"));
            }

            return sb.ToString();
        }

    }
}
