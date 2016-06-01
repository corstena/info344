using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pa3class {
    public class titleCount : TableEntity{
        public titleCount(int count) {
            this.PartitionKey = "titleCount";
            this.RowKey = "totalTitles";
            this.count = count;
        }

        public titleCount() { }

        public int count { get; set; }
    }
}
