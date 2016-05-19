using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pa3class {
    public class performanceNode : TableEntity{
        public performanceNode(string cpu, string ram) {
            this.PartitionKey = "performance";
            this.RowKey = "counter";
            this.cpu = cpu;
            this.ram = ram;
        }

        public performanceNode() { }

        public string cpu { get; set; }
        public string ram { get; set; }
    }
}
