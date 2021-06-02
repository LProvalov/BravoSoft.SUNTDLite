using SUNTDLite.Services.KBDAPIServiceModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SUNTDLite.View
{
    public class DocumentClassificator : DocumentType
    {
        public class DocumentClassificatorEntry
        {
            public DocumentClassificatorEntry(string name)
            {
                Value = name;
            }
            private string _value;
            public string Value
            {
                get
                {
                    return _value;
                }
                set
                {
                    _value = value;
                }
            }
        }
        private List<DocumentClassificatorEntry> _classificatorEntries = new List<DocumentClassificatorEntry>();
        public List<DocumentClassificatorEntry> ClassificatorEntries
        {
            get => _classificatorEntries;
            set
            {
                if (value != null && value.Count > 0)
                {
                    _classificatorEntries.Clear();
                    _classificatorEntries.AddRange(value);
                    // TODO: OnChange
                }
            }
        }
        public DocumentClassificatorEntry Value { get; set; }

        public DocumentClassificator(string name, long attributeNumber, int valueIndex, ClassificatorDescModel classificatorDescModel) : base(name, attributeNumber)
        {
            if (classificatorDescModel.Type == ClassificatorDescModel.ClassificatorDescModelType.Linear)
            {
                foreach (var classItemValue in classificatorDescModel.Values.OrderBy(v => v.Name))
                {
                    _classificatorEntries.Add(new DocumentClassificatorEntry(classItemValue.Name));
                }
            }
            else if (classificatorDescModel.Type == ClassificatorDescModel.ClassificatorDescModelType.Multiple)
            {
                throw new ArgumentException("DocumentClassificator got model of Multiple type");
            }

            try
            {
                Value = _classificatorEntries[valueIndex];
            } 
            catch (ArgumentOutOfRangeException)
            {
                Value = _classificatorEntries[0];
            }
        }

        public override IEnumerable<string> GetValueString()
        {
            if (Value != null)
            {
                return new string[] { Value?.Value };
            }
            return null;
        }

        public override void Clean()
        {
            Value = null;
            OnPropertyChanged("Value");
        }
    }
}
