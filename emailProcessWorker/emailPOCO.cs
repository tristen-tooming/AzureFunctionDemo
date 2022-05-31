
using System.Collections.Generic;

namespace emailProcesserWorker
{
    public class EmailPOCO
    {
        public string Key { get; set; }
        public string Email { get; set; }
        public List<string> Attributes { get; set; }
    }
}