﻿using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pa3class {
    public class adminNode : TableEntity {
        public adminNode(string newCommand) {
            this.PartitionKey = "admin";
            this.RowKey = "command";

            this.currentCommand = newCommand;
        }

        public adminNode() { }

        public string currentCommand { get; set; }
        
    }
}
