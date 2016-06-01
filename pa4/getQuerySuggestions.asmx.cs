using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Services;
using System.Web.Script.Services;
using System.Web.Script.Serialization;
using System.Diagnostics;
using pa3class;
using Microsoft.WindowsAzure.Storage.Table;
using System.Configuration;

namespace WebRole1 {
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]

    public class getQuerySuggestions : System.Web.Services.WebService {

        public static Trie titleTrie;
        private PerformanceCounter memProcess = new PerformanceCounter("Memory", "Available MBytes");
        private CloudTable stattable;

        [WebMethod]
        public string downloadWiki() {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                    CloudConfigurationManager.GetSetting("PA2StorageConnectionString"));
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("programmingassignment2");
            container.CreateIfNotExists();
            CloudBlockBlob blockBlob = container.GetBlockBlobReference("parsed_wikipedia_titles.txt");
            var filePath = System.IO.Path.GetTempPath() + "\\wiki.txt";
            blockBlob.DownloadToFile(filePath, FileMode.Create);

            return "Finished Downloading Blob";
        }

        [WebMethod]
        public string buildTrie() {
            titleTrie = new Trie();
            createTable();
            string lastWordAdded = "Nothing Added";
            int totalTitles = 0;
            using (System.IO.StreamReader sr = new System.IO.StreamReader(System.IO.Path.GetTempPath() + "\\wiki.txt")) {
                while (sr.EndOfStream == false && (GetAvailableMBytes() > 50)) {
                    lastWordAdded = sr.ReadLine();
                    titleTrie.insert(lastWordAdded);
                    totalTitles++;
                }
                sr.Close();
            }
            //Add title count entity
            titleCount finalTitleCount = new titleCount(totalTitles);
            TableOperation insertTitleCount = TableOperation.InsertOrReplace(finalTitleCount);
            stattable.Execute(insertTitleCount);

            //Add last title to table
            lastTitle titleAdd = new lastTitle(lastWordAdded);
            TableOperation insertLastTitle = TableOperation.InsertOrReplace(titleAdd);
            stattable.Execute(insertLastTitle);
            return lastWordAdded + " Bytes: " + GetAvailableMBytes();
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string searchTrie(string prefix) {
            if (titleTrie == null) {
                buildTrie();
            }
            return new JavaScriptSerializer().Serialize(titleTrie.searchPrefix(prefix));
        }

        [WebMethod]
        public float GetAvailableMBytes() {
            float memUsage = memProcess.NextValue();
            return memUsage;
        }

        [WebMethod]
        public void removeTrie() {
            titleTrie = null;
        }

        /// <summary>
        /// Creates a local copy of all of the tables
        /// </summary>
        private void createTable() {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                ConfigurationManager.AppSettings["PA3StorageConnectionString"]);
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            stattable = tableClient.GetTableReference("stattable");
            stattable.CreateIfNotExists();
        }
    }
}
