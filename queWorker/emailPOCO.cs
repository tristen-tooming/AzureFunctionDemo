
using System;

namespace emailProcessWorker
{
    public class MessagePOCO
    {
        public string Key { get; set; }
        public string Email { get; set; }
        public string Date { get; set; } 
        public Double Milliseconds { get; set; }
        public string SingleAttribute { get; set; }
    }
}