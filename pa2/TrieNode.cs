using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1 {
    public class TrieNode {
        public Dictionary<Char, TrieNode> childNodes;
        public Boolean endOfWord;
        public char value;
    }
}