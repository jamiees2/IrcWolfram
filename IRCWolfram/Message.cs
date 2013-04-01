using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IRCWolfram
{
    public struct Message
    {
        public string User { get; set; }
        public string Host { get; set; }
        public Reply Code { get; set; }
        public string Text { get; set; }
        public string Command { get; set; }
        public string FullText { get; set; }

        public override string ToString()
        {
            return (new StringBuilder())
                .Append("User: ").Append(User)
                .AppendLine("Host: ").Append(Host)
                .AppendLine("Code: ").Append(Code)
                .AppendLine("Text: ").Append(Text)
                .AppendLine("Command: ").Append(Command)
                .AppendLine("FullText: ").Append(FullText).ToString();
        }
    }
}
