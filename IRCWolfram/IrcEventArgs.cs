using System;

namespace IRCWolfram
{
    public class IrcEventArgs : EventArgs
    {
        public Message Message { get; set; }
    }
}
