using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SUNTDLite.View
{
    public class DocumentString : DocumentType
    {
        public DocumentString(string name, long attributeNumber) : base(name, attributeNumber) { }
        public string Value { get; set; }

        public override string GetValueString()
        {
            return Value;
        }
    }
}
