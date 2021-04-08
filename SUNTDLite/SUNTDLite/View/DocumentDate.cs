using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SUNTDLite.View
{
    public class DocumentDate : DocumentType
    {
        public DocumentDate(string name, long attributeNumber) : base(name, attributeNumber) { }
        public DateTime Value { get; set; }

        public override string GetValueString()
        {
            return Value.ToShortDateString();
        }
    }
}
