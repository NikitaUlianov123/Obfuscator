using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuscator
{
    public class GrammerDef
    {
        public List<string> text;
        public string exToken;
        public GrammerDef(List<string> inputs)
        {
            text = new List<string>();
            for (int i = 0; i < inputs.Count; i++)
            {
                if (inputs[i].Contains("^"))
                {
                    exToken = inputs[i].Replace("^", "");
                    text.Add(inputs[i].Replace("^", ""));
                }
                else
                {
                    text.Add(inputs[i]);
                }
            }
            ;
        }
    }
    public class GrammarParser
    {
        public Dictionary<string, List<GrammerDef>> grammarPairs;
        public HashSet<string> canBeNone;
        public GrammarParser(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);
            string currentReader = "";
            bool isReading = false;
            grammarPairs = new Dictionary<string, List<GrammerDef>>();
            canBeNone = new HashSet<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == "}")
                {
                    currentReader = "";
                    isReading = false;
                    continue;
                }
                if (currentReader == "")
                {
                    grammarPairs.Add(lines[i], new List<GrammerDef>());
                    currentReader = lines[i];
                }
                if (isReading == true)
                {
                    List<string> list = lines[i]
                        .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .ToList();

                    for (int d = 0; d < list.Count; d++)
                    {
                        if (list[d] == "")
                        {
                            list.RemoveAt(d);
                        }
                    }
                    if(list.Contains("\"none\""))
                    {
                        canBeNone.Add(currentReader);
                    }
                    grammarPairs[currentReader].Add(new GrammerDef(list));
                }
                if (lines[i] == "{")
                {
                    isReading = true;

                }

                
            }
            ;

        }
    }
}
