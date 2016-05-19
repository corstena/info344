using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using System.Configuration;
using Microsoft.WindowsAzure.Storage.Table;
using System.Xml;
using System.Xml.Linq;
using HtmlAgilityPack;
using System.Security.Cryptography;
using System.Text;
using pa3class;

/// <summary>
/// Albert Corsten
/// INFO 344 Programming Assignment 3
/// </summary>

namespace WorkerRole1 {
    public class WorkerRole : RoleEntryPoint {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
        private CloudQueue brokenqueue;
        private CloudQueue xmlqueue;
        private CloudQueue htmlqueue;
        private CloudQueue newestqueue;
        private CloudTable urltable;
        private CloudTable stattable;
        private HashSet<string> visitedUrls;
        private List<string> disallowList;
        private List<string> baseSitemap;


        public override void Run() {
            createQueues();
            createTable();
            initializeCounters();

            while (checkAdminStatus() == "stopCrawl") {
                System.Threading.Thread.Sleep(1000);
            }

            visitedUrls = new HashSet<string>();
            disallowList = new List<string>();
            baseSitemap = new List<string>();
            readRobots("http://www.cnn.com/robots.txt");
            readRobots("http://bleacherreport.com/robots.txt");
            
            while (true) {
                System.Threading.Thread.Sleep(50);
                if(checkAdminStatus() == "newCrawl") {
                    createQueues();
                    createTable();
                    initializeCounters();
                    foreach (string baseSitemapUrl in baseSitemap) {
                        parseXML(baseSitemapUrl);
                    }
                    TableOperation getAdminNode = TableOperation.Retrieve<adminNode>("admin", "command");
                    TableResult retrievedAdminNode = stattable.Execute(getAdminNode);
                    adminNode newAdminNode = (adminNode)retrievedAdminNode.Result;
                    if (newAdminNode != null) {
                        newAdminNode.currentCommand = "resumeCrawl";
                        TableOperation updateAdminNode = TableOperation.Replace(newAdminNode);
                        stattable.Execute(updateAdminNode);
                    }
                }
                if(checkAdminStatus() == "resumeCrawl") {
                    CloudQueueMessage currentXmlUrl = xmlqueue.GetMessage();
                    if (currentXmlUrl != null) {
                        parseXML(currentXmlUrl.AsString);
                        xmlqueue.DeleteMessage(currentXmlUrl);
                    }
                    try {
                        CloudQueueMessage currentHtmlUrl = htmlqueue.GetMessage();
                        if (currentHtmlUrl != null) {
                            crawlUrl(currentHtmlUrl.AsString);
                            htmlqueue.DeleteMessage(currentHtmlUrl);
                        }
                    } catch(Exception e) {

                    }
                    
                }
                updatePerformanceCounter();
            }
            

        }

        public override bool OnStart() {
            // Set the maximum number of concurrent connections
            ServicePointManager.DefaultConnectionLimit = 12;

            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            bool result = base.OnStart();

            Trace.TraceInformation("WorkerRole1 has been started");

            return result;
        }

        public override void OnStop() {
            Trace.TraceInformation("WorkerRole1 is stopping");

            this.cancellationTokenSource.Cancel();
            this.runCompleteEvent.WaitOne();

            base.OnStop();

            Trace.TraceInformation("WorkerRole1 has stopped");
        }

        private async Task RunAsync(CancellationToken cancellationToken) {
            // TODO: Replace the following with your own logic.
            while (!cancellationToken.IsCancellationRequested) {
                Trace.TraceInformation("Working");
                await Task.Delay(1000);
            }
        }

        /// <summary>
        /// Setup the table counters used for gathering statistics
        /// </summary>
        public void initializeCounters() {
            //Initialize row counter
            int currentRows = 0;
            TableOperation getRowCount = TableOperation.Retrieve<rowCount>("rowCount", "totalRows");
            TableResult retrievedRowCount = stattable.Execute(getRowCount);
            if (retrievedRowCount.Result != null) {
                currentRows = (((rowCount)retrievedRowCount.Result).count);
            }
            rowCount countStart = new rowCount(currentRows);
            TableOperation insertCountStart = TableOperation.InsertOrReplace(countStart);
            stattable.Execute(insertCountStart);

            //Initialize crawler to stop
            adminNode stopCrawl = new adminNode("stopCrawl");
            TableOperation insertAdminNode = TableOperation.InsertOrReplace(stopCrawl);
            stattable.Execute(insertAdminNode);

            //Initialize preformance counters
            performanceNode newPerformance = new performanceNode("None", "None");
            TableOperation insertPerformanceNode = TableOperation.InsertOrReplace(newPerformance);
            stattable.Execute(insertPerformanceNode);
        }

        /// <summary>
        /// Checks the current admin status of the crawler (crawling, loading, stopped)
        /// </summary>
        /// <returns>Returns status as a string</returns>
        public string checkAdminStatus() {
            TableOperation getAdminNode = TableOperation.Retrieve<adminNode>("admin", "command");
            TableResult retrievedAdminNode = stattable.Execute(getAdminNode);
            if (retrievedAdminNode != null) {
                return (((adminNode)retrievedAdminNode.Result).currentCommand);
            } else {
                return "No Command";
            }
        }

        /// <summary>
        /// Creates a local instance of the queues
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
        /// Creates a local instance of the tables
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

        /// <summary>
        /// Updates the performance counter table counters for both processor usage and memory usage
        /// </summary>
        private void updatePerformanceCounter() {
            PerformanceCounter cpuCounter = new PerformanceCounter();
            PerformanceCounter memProcess = new PerformanceCounter("Memory", "Available MBytes");
            cpuCounter.CategoryName = "Processor";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = "_Total";
            string cpu = (getCurrentCpuUsage(cpuCounter));
            string ram = (getAvailableMBytes(memProcess));

            TableOperation getPerformanceNode = TableOperation.Retrieve<performanceNode>("performance", "counter");
            TableResult retrievedPerformanceNode = stattable.Execute(getPerformanceNode);
            performanceNode newPerformanceNode = (performanceNode)retrievedPerformanceNode.Result;
            if (newPerformanceNode != null) {
                newPerformanceNode.cpu = cpu;
                newPerformanceNode.ram = ram;
                TableOperation updatePerformanceNode = TableOperation.Replace(newPerformanceNode);
                stattable.Execute(updatePerformanceNode);
            }
        }

        /// <summary>
        /// Gets the available RAM
        /// </summary>
        /// <returns>Returns the available system ram in MB</returns>
        public string getAvailableMBytes(PerformanceCounter process) {
            return process.NextValue() + "MB";
        }

        /// <summary>
        /// Gets the current CPU usage
        /// </summary>
        /// <returns>Returns the current CPU usage in %</returns>
        public string getCurrentCpuUsage(PerformanceCounter process) {
            return process.NextValue() + "%";
        }

        /// <summary>
        /// Downloads, parses, and reads the passed in .xml URL and parses it for both XML and HTML links
        /// </summary>
        /// <param name="xmlurl">The base XML URL to read from</param>
        public void parseXML(string xmlurl) {
            var document = XDocument.Load(xmlurl);
            string root = document.Root.Name.ToString().Split('}')[1];
            XNamespace ns;
            if (xmlurl.Contains("www.cnn.com")) {
                ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            }
            else {
                ns = "http://www.google.com/schemas/sitemap/0.9";
            }
            int counter = 0;
            var lastModDates = document.Descendants(ns + "lastmod").ToArray();
            foreach (var loc in document.Descendants(ns + "loc")) {
                string currentUrl = loc.Value;
                string urlLastMod = null;
                if (lastModDates.Length > 0) {
                    urlLastMod = lastModDates[counter].Value;
                }
                if (checkURL(currentUrl)) {
                    if (root == "sitemapindex" && checkCnnDate(currentUrl, urlLastMod)) {
                        CloudQueueMessage newxmlurl = new CloudQueueMessage(currentUrl);
                        xmlqueue.AddMessage(newxmlurl);
                    }
                    else if (root == "urlset" && (currentUrl.Contains(".html") || currentUrl.Contains(".htm"))) {
                        CloudQueueMessage newhtmlurl = new CloudQueueMessage(currentUrl);
                        htmlqueue.AddMessage(newhtmlurl);
                    } else if(root == "urlset" && currentUrl.StartsWith("http://bleacherreport.com/")) {
                        CloudQueueMessage newhtmlurl = new CloudQueueMessage(currentUrl);
                        htmlqueue.AddMessage(newhtmlurl);
                    }
                    visitedUrls.Add(currentUrl);
                    counter++;
                }

            }
        }

        /// <summary>
        /// Checks whether the URL is from either CNN or bleacher report and a valid URL
        /// </summary>
        /// <param name="url"></param>
        /// <returns>Returns true if it is a valid URL, false if not</returns>
        public Boolean checkURL(string url) {
            if ((url.StartsWith("http://www.cnn.com") || url.StartsWith("http://bleacherreport.com/")) && !visitedUrls.Contains(url)) {
                foreach (string disallowedURL in disallowList) {
                    if (url.Contains(disallowedURL)) {
                        return false;
                    }
                }
                return true;
            }
            else {
                return false;
            }
        }

        /// <summary>
        /// Checks to see if the URL is contains any of the disallows from robots.txt
        /// </summary>
        /// <param name="url"></param>
        /// <returns>Returns true if it is a disallowed link (bad link!) and false if it is an allowed link</returns>
        public Boolean disallowedLink(string url) {
            foreach (string disallowedURL in disallowList) {
                if (url.Contains(disallowedURL)) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Parses the passed in URL's robot.txt and updates the basesitmap and disallowed list accordingly
        /// </summary>
        /// <param name="url">The URL that contains a website's robot.txt</param>
        public void readRobots(string url) {
            string robots = new WebClient().DownloadString(url);
            string[] robotsLines = robots.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (string currentLine in robotsLines) {
                if (currentLine.Contains("Sitemap:")) {
                    if (currentLine.Contains("http://www.cnn.com") || currentLine.Contains("http://bleacherreport.com/sitemap/nba.xml")) {
                        baseSitemap.Add(currentLine.Split(' ')[1]);
                    }
                }
                else if (currentLine.Contains("Disallow:")) {
                    disallowList.Add(currentLine.Split(' ')[1]);
                }
            }
        }

        /// <summary>
        /// Checks to see if only sitemaps for CNN are from the past 2 months
        /// </summary>
        /// <param name="url">The URL for the sitemap</param>
        /// <param name="date">The last modified date for the sitemap</param>
        /// <returns></returns>
        public Boolean checkCnnDate(string url, string date) {
            if (url.StartsWith("http://bleacherreport.com")) {
                return false;
            }
            else if (date == null) {
                return false;
            }
            string yearString = date.Split('-')[0];
            string monthString = date.Split('-')[1];
            int year = Int32.Parse(yearString);
            int month = Int32.Parse(monthString);
            if ((year >= 2016) && month >= 3) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Crawls the passed in URL for all html links and adds them into the urlqueue. Adds the passed in
        /// URL to the urltable once it is done crawling.
        /// </summary>
        /// <param name="htmlurl">The URL that you wish to crawl</param>
        public void crawlUrl(string htmlurl) {
            try {
                HtmlWeb web = new HtmlWeb();
                HtmlDocument doc = web.Load(htmlurl);
                //Check the webpage for any errors (such as 404)
                if (web.StatusCode == HttpStatusCode.OK) {
                    if (htmlurl.StartsWith("http://bleacherreport.com") || htmlurl.StartsWith("http://www.cnn.com")) {
                        string pageTitle = "No page title";
                        string pagePubDate = "No pub date found";
                        if (doc != null && doc.DocumentNode != null) {
                            pageTitle = (doc.DocumentNode.SelectSingleNode("//head/title").InnerText);
                            if (pageTitle != "Error") {
                                var dateTest = (doc.DocumentNode.SelectSingleNode("//meta[@name='pubdate']"));
                                if (dateTest != null) {
                                    pagePubDate = (doc.DocumentNode.SelectSingleNode("//meta[@name='pubdate']").GetAttributeValue("content", string.Empty));
                                }
                                var hrefTest = doc.DocumentNode.SelectNodes("//a[@href]");
                                if (hrefTest != null) {
                                    foreach (HtmlNode link in doc.DocumentNode.SelectNodes("//a[@href]")) {
                                        string hrefValue = link.GetAttributeValue("href", string.Empty);
                                        string newUrl = "None";
                                        if (hrefValue.StartsWith("http://bleacherreport.com") && !disallowedLink(hrefValue)) {
                                            newUrl = hrefValue;
                                        }
                                        else {
                                            newUrl = getCrawledUrl(htmlurl, hrefValue);
                                        }
                                        if (newUrl != null & !visitedUrls.Contains(newUrl) && (newUrl.StartsWith("http://bleacherreport.com/articles") || newUrl.StartsWith("http://www.cnn.com"))) {
                                            visitedUrls.Add(newUrl);
                                            CloudQueueMessage newhtmlurl = new CloudQueueMessage(newUrl);
                                            htmlqueue.AddMessage(newhtmlurl);
                                        }
                                    }
                                }
                                //Add the current crawled htmlurl to the queue of 10 recently added urls
                                CloudQueueMessage newestUrl = new CloudQueueMessage(htmlurl);
                                newestqueue.FetchAttributes();
                                int queueLength = (int)newestqueue.ApproximateMessageCount;
                                if (queueLength < 10) {
                                    newestqueue.AddMessage(newestUrl);
                                }
                                else {
                                    CloudQueueMessage lastMessage = newestqueue.GetMessage();
                                    if (lastMessage != null) {
                                        newestqueue.DeleteMessage(lastMessage);
                                    }
                                    newestqueue.AddMessage(newestUrl);
                                }
                                //Add the current crawled htmlurl to the table
                                string urlHash = storage.calculateHash(htmlurl.ToLower());
                                urlNode tableUrl = new urlNode(urlHash, htmlurl, pageTitle, pagePubDate);
                                TableOperation insertOperation = TableOperation.Insert(tableUrl);
                                urltable.Execute(insertOperation);
                            }
                            else { //Webpages that end up in errors will be sent here
                                visitedUrls.Add(htmlurl);
                                CloudQueueMessage badurl = new CloudQueueMessage(htmlurl);
                                brokenqueue.AddMessage(badurl);
                            }
                        }
                        //Update table row count entitiy
                        TableOperation getRowCount = TableOperation.Retrieve<rowCount>("rowCount", "totalRows");
                        TableResult retrievedRowCount = stattable.Execute(getRowCount);
                        rowCount newRowCount = (rowCount)retrievedRowCount.Result;
                        if (newRowCount != null) {
                            int newCount = (((rowCount)retrievedRowCount.Result).count) + 1;
                            newRowCount.count = newCount;
                            TableOperation updateRowCount = TableOperation.Replace(newRowCount);
                            stattable.Execute(updateRowCount);
                        }
                    }

                }
            } catch(Exception e) {
            }
        }

        /// <summary>
        /// Transfers the passed in Href into a proper URL
        /// </summary>
        /// <param name="htmlurl">The base URL that you wish to trasnfer</param>
        /// <param name="href">The Href that you wish to get a URL for</param>
        /// <returns>Returns a proper URL for the Href as a string</returns>
        public string getCrawledUrl(string htmlurl, string href) {
            Uri baseUri;
            if (htmlurl.StartsWith("http://www.cnn.com")) {
                baseUri = new Uri("http://www.cnn.com");
            }
            else if (htmlurl.StartsWith("http://bleacherreport.com")) {
                baseUri = new Uri("http://bleacherreport.com");
            }
            else {
                baseUri = new Uri("badhtmlurl");
            }
            //Check for URIs with proper labeling, if they are bad, set them to /badlink
            Uri newUri;
            if (href.StartsWith("/") && !href.StartsWith("//")) {
                newUri = new Uri(baseUri, href);
            }
            else {
                newUri = new Uri(baseUri, "/badlink");
            }
            //Convert newUri to a string and return it in the case that it is a good url
            string newUriString = newUri.ToString();
            if (!disallowedLink(newUriString) && !(newUriString.StartsWith("badhtmlurl") || newUriString.EndsWith("/badlink"))) {
                return newUriString;
            }
            else {
                return null;
            }
        }
    }
}
