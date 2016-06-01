using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pa3class {
    public class TrieNode {
        public Dictionary<Char, TrieNode> childNodes;
        public Boolean endOfWord;
        public char value;
    }
}
