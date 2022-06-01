
using System.Collections.Generic;
using System;

namespace queWorker
{
    public class EmailPOCO
    {
        public string Key { get; set; }
        public string Email { get; set; }
        public DateTime Stamp { get; set; } 
        public List<string> Attributes { get; set; }
    }
}