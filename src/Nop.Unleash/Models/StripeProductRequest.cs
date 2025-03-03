using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Unleash.Models
{
    public class StripeProductRequest
    {
        public string Name { get; set; }    
        public bool Active { get; set; }
        public string Description { get; set; } 
        public Dictionary<string, string> MetaData { get; set; }

    }
}
