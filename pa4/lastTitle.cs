using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pa3class {
    public class lastTitle : TableEntity {
        public lastTitle (string title) {
            this.PartitionKey = "lastTitle";
            this.RowKey = "added";
            this.title = title;
        }

        public lastTitle() { }

        public string title { get; set; }
    }
}
