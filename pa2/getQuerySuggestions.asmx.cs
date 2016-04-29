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

namespace WebRole1 {
    /// <summary>
    /// Summary description for getQuerySuggestions
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [System.Web.Script.Services.ScriptService]

    public class getQuerySuggestions : System.Web.Services.WebService {

        public static Trie titleTrie;
        private PerformanceCounter memProcess = new PerformanceCounter("Memory", "Available MBytes");

        [WebMethod]
        public string downloadWiki() {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                    CloudConfigurationManager.GetSetting("StorageConnectionString"));
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
            string lastWordAdded = "Nothing Added";
            using (System.IO.StreamReader sr = new System.IO.StreamReader(System.IO.Path.GetTempPath() + "\\wiki.txt")) {
                while (sr.EndOfStream == false && (GetAvailableMBytes() > 50)) {
                    lastWordAdded = sr.ReadLine();
                    titleTrie.insert(lastWordAdded);
                }
                sr.Close();
            }
            return lastWordAdded + " Bytes: " + GetAvailableMBytes();
        }

        [WebMethod]
        [ScriptMethod(ResponseFormat = ResponseFormat.Json)]
        public string searchTrie(string prefix) {
            return new JavaScriptSerializer().Serialize(titleTrie.searchPrefix(prefix));
        }

        [WebMethod]
        public float GetAvailableMBytes() {
            float memUsage = memProcess.NextValue();
            return memUsage;
        }
    }
}
