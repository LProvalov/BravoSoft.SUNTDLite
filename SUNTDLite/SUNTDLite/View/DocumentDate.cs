using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SUNTDLite.View
{
    public class DocumentDate : DocumentType
    {
        public DocumentDate(string name, long attributeNumber) : base(name, attributeNumber) { }
        public Nullable<DateTime> Value { get; set; } = null;

        public override void Clean()
        {
            Value = null;
            OnPropertyChanged("Value");
        }

        public override IEnumerable<string> GetValueString()
        {
            if (Value != null)
            {
                return new string[] { Value?.ToShortDateString() };
            }
            return null;
        }
    }
}
