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

        public override void Clean()
        {
            Value = string.Empty;
            OnPropertyChanged("Value");
        }

        public override IEnumerable<string> GetValueString()
        {
            if (!string.IsNullOrEmpty(Value))
            {
                return new string[] { Value };
            }
            return null;
        }
    }
}
