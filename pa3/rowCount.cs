using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pa3class {
    public class rowCount : TableEntity {
        public rowCount(int count) {
            this.PartitionKey = "rowCount";
            this.RowKey = "totalRows";
            this.count = count;
        }

        public rowCount() { }

        public int count{ get; set; }
    }
}
