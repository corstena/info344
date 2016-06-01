using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pa3class {
    public class urlNode : TableEntity {
        public urlNode(string keyword, string hash, string url, string title, string pubDate) {
            this.PartitionKey = keyword;
            this.RowKey = hash;

            this.url = url;
            this.title = title;
            this.pubDate = pubDate;
        }

        public urlNode() { }

        public string url { get; set; }
        public string title { get; set; }
        public string pubDate { get; set; }
    }
}
