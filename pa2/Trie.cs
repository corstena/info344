using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1 {
    public class Trie {
        private TrieNode rootNode;

        public Trie() {
            rootNode = new TrieNode();
        }

        /// <summary>
        /// Inserts a the newWord into the Trie
        /// </summary>
        /// <param name="newWord">The word to be added into the Trie</param>
        public void insert(String newWord) {
            TrieNode currentNode = rootNode;
            newWord = newWord.ToLower();
            for (int i = 0; i < newWord.Length; i++) {
                char currentCharacter = newWord[i];

                if (currentNode.childNodes == null) {
                    currentNode.childNodes = new Dictionary<Char, TrieNode>();
                }

                if (!currentNode.childNodes.ContainsKey(currentCharacter)) {
                    TrieNode tempNode = new TrieNode();
                    currentNode.childNodes[currentCharacter] = tempNode;
                }
                currentNode = currentNode.childNodes[currentCharacter];
            }
            currentNode.endOfWord = true;
        }

        /// <summary>
        /// Returns a list of strings of up to 10 words that match the prefix
        /// </summary>
        /// <param name="prefix">The prefix that the user is searching for</param>
        /// <returns>A list of strings with the result words</returns>
        public List<String> searchPrefix(String prefix) {
            TrieNode startNode = searchNode(prefix);
            if (startNode == null) {
                return null;
            }
            List<String> result = new List<String>();
            if (startNode.endOfWord) {
                result.Add(prefix);
            }
            return recursiveSearch(result, startNode, prefix);
        }

        /// <summary>
        /// Recursively searches for the prefix from the searchPrefix method
        /// </summary>
        /// <param name="result">The result list of strings</param>
        /// <param name="currentNode">The current node it is searching</param>
        /// <param name="currentWord">The current word that is being built</param>
        /// <returns>A list of strings with the result words</returns>
        public List<String> recursiveSearch(List<String> result, TrieNode currentNode, String currentWord) {
            if (currentNode.childNodes != null && result.Count < 10) {
                foreach (KeyValuePair<char, TrieNode> letter in currentNode.childNodes) {
                    if (result.Count() >= 10) {
                        return result;
                    }
                    string newWord = currentWord + letter.Key;
                    if (result.Count() < 10 && letter.Value.endOfWord) {
                        result.Add(newWord);
                    }
                    recursiveSearch(result, currentNode.childNodes[letter.Key], newWord);
                }
            }
            return result;
        }

        /// <summary>
        /// Finds the start node that the recursive search should begin at
        /// </summary>
        /// <param name="searchWord">The word that the user is looking for</param>
        /// <returns>Returns the TrieNode that the search begins at</returns>
        public TrieNode searchNode(String searchWord) {
            TrieNode search = rootNode;
            searchWord = searchWord.ToLower();
            for (int i = 0; i < searchWord.Length; i++) {
                char currentCharacter = searchWord[i];
                //If the a leaf node is the same as the current character in the search
                //word, move down the Trie
                if (search.childNodes.ContainsKey(currentCharacter)) {
                    search = search.childNodes[currentCharacter];
                }
                else {
                    return null;
                }
            }
            if (search == rootNode) {
                return null;
            }
            return search;
        }
    }
}