using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SUNTDLite.ApplicationConfiguration
{
    [Serializable]
    public class DocumentAttribute
    {
        public string Name { get; set; }
        
        private string Type { get; set; }
        
    }
}
