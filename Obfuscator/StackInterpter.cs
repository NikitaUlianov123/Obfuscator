using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obfuscator
{

    
    public class StackInterpter
    {
        public void AddVarible(string varibleName)
        {
            OringalToNew.Add(varibleName, "VARIBLE" + VaribleNumber);
            VaribleNumber++;
        }
        
        public string FindVarible(string varibleName)
        {
            if(OringalToNew.ContainsKey(varibleName))
            {
                return OringalToNew[varibleName];
            }
            if(Parent != null)
            {
                return Parent.FindVarible(varibleName);
            }
            return "";
        }
        private Dictionary<string, string> OringalToNew;
        public int VaribleNumber = 0;
        public StackInterpter Parent;

        public StackInterpter()
        {
            OringalToNew = new Dictionary<string, string>();
            Parent = null;

        }
    }
}
