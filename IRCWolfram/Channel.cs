using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCWolfram
{
    public struct Channel
    {
        public string Name { get; set; }
        public List<string> Members { get; set; }
        public List<string> Operators { get; set; }
        public string Owner { get; set; }
        
    }
}
