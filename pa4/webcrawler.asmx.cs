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

namespace WebRole1 {
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
        private static Dictionary<string, Tuple<string, DateTime>> cache;

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
            createQueues();
            brokenqueue.FetchAttributes();
            int numRows = 0;
            TableOperation getRowCount = TableOperation.Retrieve<rowCount>("rowCount", "totalRows");
            TableResult retrievedRowCount = stattable.Execute(getRowCount);
            if (retrievedRowCount != null) {
                numRows = (((rowCount)retrievedRowCount.Result).count);
            }
            numRows = numRows + (int)brokenqueue.ApproximateMessageCount; //Add broken URLs to crawled count
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
                if (currentState == "resumeCrawl") {
                    returnState = "Crawler is active";
                }
                else if (currentState == "stopCrawl") {
                    returnState = "Crawler is idle";
                }
                else if (currentState == "newCrawl") {
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
            }
            else {
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
            if (queueLength > 10) {
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
        /// Gets the list of all broken URLs the worker attempted to crawl (limit to 100)
        /// </summary>
        [WebMethod]
        public string getBrokenUrls() {
            createQueues();
            List<string> result = new List<string>();
            brokenqueue.FetchAttributes();
            int queueLength = (int)brokenqueue.ApproximateMessageCount;
            if (queueLength > 100) {
                queueLength = 100;
            }
            for (int i = 0; i < queueLength; i++) {
                CloudQueueMessage currentMessage = brokenqueue.GetMessage();
                if (currentMessage != null) {
                    result.Add(currentMessage.AsString);
                    brokenqueue.DeleteMessage(currentMessage);
                    brokenqueue.AddMessage(currentMessage);
                }
            }
            if (queueLength == 0) {
                result.Add("Currently, there are no broken URLs that have been processed");
            }
            return new JavaScriptSerializer().Serialize(result);
        }

        /// <summary>
        /// Returns the count of the number of Wikipedia titles in the trie
        /// </summary>
        [WebMethod]
        public string getTitleCount() {
            createTable();
            int totalTitleCount = 0;
            TableOperation getTitleCount = TableOperation.Retrieve<titleCount>("titleCount", "totalTitles");
            TableResult retrievedTitleCount = stattable.Execute(getTitleCount);
            if (retrievedTitleCount != null) {
                totalTitleCount = (((titleCount)retrievedTitleCount.Result).count);
            }
            return new JavaScriptSerializer().Serialize(totalTitleCount);
        }

        /// <summary>
        /// Returns the last Wikipedia title that was added into the tire
        /// </summary>
        [WebMethod]
        public string getLastTitle() {
            createTable();
            string lastTitleAdded = "No titles added";
            TableOperation getLastTitle = TableOperation.Retrieve<lastTitle>("lastTitle", "added");
            TableResult retrievedLastTitle = stattable.Execute(getLastTitle);
            if (retrievedLastTitle != null) {
                lastTitleAdded = (((lastTitle)retrievedLastTitle.Result).title);
            }
            return new JavaScriptSerializer().Serialize(lastTitleAdded);
        }

        /// <summary>
        /// Creates a local copy of all of the queues
        /// </summary>
        private void createQueues() {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["PA3StorageConnectionString"]);
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
                ConfigurationManager.AppSettings["PA3StorageConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            urltable = tableClient.GetTableReference("urltable");
            stattable = tableClient.GetTableReference("stattable");
            urltable.CreateIfNotExists();
            stattable.CreateIfNotExists();
        }

        /// <summary>
        /// Finds and returns the top 20 search results for a query
        /// </summary>
        /// <param name="lowerSearchQuery">The search query that the user enters into the search box</param>
        /// <returns>A string that contains all search result URLs formatted in JSON</returns>
        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string getSearchResult(string searchQuery) {
            string lowerSearchQuery = searchQuery.ToLower();
            //Limit cache size to 100
            if(cache == null || cache.Count > 100) {
                cache = new Dictionary<string, Tuple<string, DateTime>>();
            }
            //If the URL is in the cache, and under 10 minutes old, use the cached result
            if (cache.ContainsKey(lowerSearchQuery) && (cache[lowerSearchQuery].Item2.AddMinutes(10) > DateTime.UtcNow)) {
                return cache[lowerSearchQuery].Item1;
            }
            Boolean resultFound = false;
            Dictionary<string, int> searchRankings = new Dictionary<string, int>();
            string[] titleWords = lowerSearchQuery.Split(' ');
            createTable();
            foreach(string currentTitleWord in titleWords) {
                TableQuery<urlNode> query = new TableQuery<urlNode>()
                    .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, currentTitleWord));
                int maxTitleCount = 0;
                foreach(urlNode currentNode in urltable.ExecuteQuery(query)) {
                    if(maxTitleCount == 500) {
                        break;
                    }
                    if(searchRankings.ContainsKey(currentNode.url)) {
                        searchRankings[currentNode.url]++;
                    } else {
                        searchRankings[currentNode.url] = 1;
                    }
                    maxTitleCount++;
                    resultFound = true;
                }
            }
            List<string> searchResult = new List<string>();
            if (resultFound) {
                int maxResultCount = 0;
                foreach (KeyValuePair<string, int> currentItem in searchRankings.OrderByDescending(key => key.Value)) {
                    // do something with item.Key and item.Value
                    searchResult.Add(currentItem.Key);
                    maxResultCount++;
                    if (maxResultCount == 20) {
                        break;
                    }
                }
                cache[lowerSearchQuery] = new Tuple<string, DateTime>(new JavaScriptSerializer().Serialize(searchResult), DateTime.UtcNow);
            } else {
                searchResult.Add("No Results Found");
            }
            return new JavaScriptSerializer().Serialize(searchResult);
        }
    }
}
