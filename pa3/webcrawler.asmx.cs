using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Services;
using System.Xml;
using System.Xml.Linq;
using pa3class;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Table;
using System.Web.Script.Services;
using System.Web.Script.Serialization;
using System.Diagnostics;

/// <summary>
/// Albert Corsten
/// INFO 344 Programming Assignment 3
/// </summary>

namespace WebRole1 {
    /// <summary>
    /// Summary description for webcrawler
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]
    public class webcrawler : System.Web.Services.WebService {
        private HashSet<string> visitedUrls;
        private List<string> disallowList;
        private List<string> baseSitemap;
        private CloudQueue brokenqueue;
        private CloudQueue xmlqueue;
        private CloudQueue htmlqueue;
        private CloudQueue newestqueue;
        private CloudTable urltable;
        private CloudTable stattable;
        
        /// <summary>
        /// Starts an entirely new webcrawl
        /// </summary>
        [WebMethod]
        public string startCrawl() {
            createTable();
            TableOperation getAdminNode = TableOperation.Retrieve<adminNode>("admin", "command");
            TableResult retrievedAdminNode = stattable.Execute(getAdminNode);
            adminNode newAdminNode = (adminNode)retrievedAdminNode.Result;
            if (newAdminNode != null) {
                newAdminNode.currentCommand = "newCrawl";
                TableOperation updateAdminNode = TableOperation.Replace(newAdminNode);
                stattable.Execute(updateAdminNode);
            }
            return "Starting Crawl...";
        }

        /// <summary>
        /// Stops the webcrawl
        /// </summary>
        [WebMethod]
        public string stopCrawl() {
            createTable();
            TableOperation getAdminNode = TableOperation.Retrieve<adminNode>("admin", "command");
            TableResult retrievedAdminNode = stattable.Execute(getAdminNode);
            adminNode newAdminNode = (adminNode)retrievedAdminNode.Result;
            if (newAdminNode != null) {
                newAdminNode.currentCommand = "stopCrawl";
                TableOperation updateAdminNode = TableOperation.Replace(newAdminNode);
                stattable.Execute(updateAdminNode);
            }
            return "Stopping crawl...";
        }

        /// <summary>
        /// Resumes the webcrawl (call this method after using stopCrawl() to resume the crawl)
        /// </summary>
        [WebMethod]
        public string resumeCrawl() {
            createTable();
            TableOperation getAdminNode = TableOperation.Retrieve<adminNode>("admin", "command");
            TableResult retrievedAdminNode = stattable.Execute(getAdminNode);
            adminNode newAdminNode = (adminNode)retrievedAdminNode.Result;
            if (newAdminNode != null) {
                newAdminNode.currentCommand = "resumeCrawl";
                TableOperation updateAdminNode = TableOperation.Replace(newAdminNode);
                stattable.Execute(updateAdminNode);
            }
            return "Resuming crawl...";
        }

        /// <summary>
        /// Deletes all queues and tables that have been crawled
        /// </summary>
        [WebMethod]
        public string clearCrawl() {
            createQueues();
            createTable();
            stopCrawl();
            brokenqueue.Delete();
            htmlqueue.Delete();
            xmlqueue.Delete();
            newestqueue.Delete();
            urltable.DeleteIfExists();
            rowCount countStart = new rowCount(0);
            TableOperation insertCountStart = TableOperation.InsertOrReplace(countStart);
            stattable.Execute(insertCountStart);
            System.Threading.Thread.Sleep(60000);
            return "Deleted all tables and queues and stopped crawl";
        }

        /// <summary>
        /// Gets the number of messages that need to be processed in the urlqueue
        /// </summary>
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getMessageCount() {
            createQueues();
            htmlqueue.FetchAttributes();
            int count = (int)htmlqueue.ApproximateMessageCount;
            return new JavaScriptSerializer().Serialize(count);
        }

        /// <summary>
        /// Gets the CPU usage and available RAM of the worker role
        /// </summary>
        [WebMethod]
        public string getSystemStats() {
            createTable();
            List<string> systemStats = new List<string>();

            TableOperation getPerformanceNode = TableOperation.Retrieve<performanceNode>("performance", "counter");
            TableResult retrievedPerformanceNode = stattable.Execute(getPerformanceNode);
            if (retrievedPerformanceNode != null) {
                systemStats.Add(((performanceNode)retrievedPerformanceNode.Result).cpu);
                systemStats.Add(((performanceNode)retrievedPerformanceNode.Result).ram);
            }
            return new JavaScriptSerializer().Serialize(systemStats);
        }

        /// <summary>
        /// Gets the amount of URLs that have been crawled
        /// </summary>
        [WebMethod]
        public string getTableCount() {
            createTable();
            int numRows = 0;
            TableOperation getRowCount = TableOperation.Retrieve<rowCount>("rowCount", "totalRows");
            TableResult retrievedRowCount = stattable.Execute(getRowCount);
            if (retrievedRowCount != null) {
                numRows = (((rowCount)retrievedRowCount.Result).count);
            }
            return new JavaScriptSerializer().Serialize(numRows);
        }

        /// <summary>
        /// Gets the state of the worker role (active, idle, or loading)
        /// </summary>
        [WebMethod]
        public string getWorkerState() {
            createTable();
            string returnState = "None";
            TableOperation getAdminNode = TableOperation.Retrieve<adminNode>("admin", "command");
            TableResult retrievedAdminNode = stattable.Execute(getAdminNode);
            if (retrievedAdminNode != null) {
                string currentState = (((adminNode)retrievedAdminNode.Result).currentCommand);
                if(currentState == "resumeCrawl") {
                    returnState = "Crawler is active";
                } else if(currentState == "stopCrawl") {
                    returnState = "Crawler is idle";
                } else if(currentState == "newCrawl") {
                    returnState = "Crawler is loading";
                }
            }
            return new JavaScriptSerializer().Serialize(returnState); 
        }

        /// <summary>
        /// Searches the URLtable for the title of the passed in search URL
        /// </summary>
        [WebMethod]
        public string searchTable(string search) {
            createTable();
            string result;
            string searchHash = storage.calculateHash(search);
            TableOperation getSearch = TableOperation.Retrieve<urlNode>("crawledURL", searchHash);
            TableResult retrievedSearch = urltable.Execute(getSearch);
            if (retrievedSearch.Result != null) {
                result = (((urlNode)retrievedSearch.Result).title);
            } else {
                result = "No URL found";
            }
            return new JavaScriptSerializer().Serialize(result);
        }

        /// <summary>
        /// Grabs the last 10 URLs that were crawled
        /// </summary>
        [WebMethod]
        public string getLast10() {
            createQueues();
            List<string> result = new List<string>();
            newestqueue.FetchAttributes();
            int queueLength = (int)newestqueue.ApproximateMessageCount;
            if(queueLength > 10) {
                queueLength = 10;
            }
            for (int i = 0; i < queueLength; i++) {
                CloudQueueMessage currentMessage = newestqueue.GetMessage();
                if (currentMessage != null) {
                    result.Add(currentMessage.AsString);
                    newestqueue.DeleteMessage(currentMessage);
                    newestqueue.AddMessage(currentMessage);
                }
            }
            return new JavaScriptSerializer().Serialize(result);
        }

        /// <summary>
        /// Gets the list of all broken URLs the worker attempted to crawl
        /// </summary>
        [WebMethod]
        public string getBrokenUrls() {
            createQueues();
            List<string> result = new List<string>();
            brokenqueue.FetchAttributes();
            int queueLength = (int)brokenqueue.ApproximateMessageCount;
            for (int i = 0; i < queueLength; i++) {
                CloudQueueMessage currentMessage = brokenqueue.GetMessage();
                if (currentMessage != null) {
                    result.Add(currentMessage.AsString);
                    brokenqueue.DeleteMessage(currentMessage);
                    brokenqueue.AddMessage(currentMessage);
                }
            }
            if(queueLength == 0) {
                result.Add("Currently, there are no broken URLs that have been processed");
            }
            return new JavaScriptSerializer().Serialize(result);

        }

        /// <summary>
        /// Creates a local copy of all of the queues
        /// </summary>
        private void createQueues() {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            brokenqueue = queueClient.GetQueueReference("brokenqueue");
            xmlqueue = queueClient.GetQueueReference("xmlqueue");
            htmlqueue = queueClient.GetQueueReference("urlqueue");
            newestqueue = queueClient.GetQueueReference("newestqueue");
            brokenqueue.CreateIfNotExists();
            xmlqueue.CreateIfNotExists();
            htmlqueue.CreateIfNotExists();
            newestqueue.CreateIfNotExists();
        }

        /// <summary>
        /// Creates a local copy of all of the tables
        /// </summary>
        private void createTable() {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            urltable = tableClient.GetTableReference("urltable");
            stattable = tableClient.GetTableReference("stattable");
            urltable.CreateIfNotExists();
            stattable.CreateIfNotExists();
        }
    }
}
