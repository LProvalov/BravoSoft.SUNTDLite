using SUNTDLite.Services.KBDAPIServiceModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SUNTDLite.View
{
    public class DocumentClassificator : DocumentType
    {
        public enum DocumentClassificatorType
        {
            Single,
            Multiple
        }
        public class DocumentClassificatorEntry
        {
            public DocumentClassificatorEntry(string value)
            {
                _value = value;
            }
            private string _value;
            public string Value 
            {
                get
                {
                    if (Childrens != null && Childrens.Any())
                    {
                        return $"\t+{_value} ({Childrens.Count})";
                    }
                    return _value;
                }
                set
                {
                    _value = value;
                }
            }
            public List<DocumentClassificatorEntry> Childrens { get; } = new List<DocumentClassificatorEntry>();
        }

        public DocumentClassificatorType Type { get; protected set; }

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

        public DocumentClassificator(string name, long attributeNumber, int valueIndex, ClassificatorDescModel classificatorDescModel, 
            DocumentClassificatorType type = DocumentClassificatorType.Single) : base(name, attributeNumber)
        {
            if (classificatorDescModel.Type == ClassificatorDescModel.ClassificatorDescModelType.Linear)
            {
                foreach (var classItemValue in classificatorDescModel.Values)
                {
                    _classificatorEntries.Add(new DocumentClassificatorEntry(classItemValue.Name));
                }
            }
            else if (classificatorDescModel.Type == ClassificatorDescModel.ClassificatorDescModelType.Multiple)
            {
                foreach(var classItemValue in classificatorDescModel.Values)
                {
                    var entry = new DocumentClassificatorEntry(classItemValue.Name);
                    if (classItemValue.Childrens != null && classItemValue.Childrens.Any())
                    {
                        entry.Childrens.AddRange(classItemValue.Childrens.Select(c => new DocumentClassificatorEntry(c.Name)));
                    }
                    _classificatorEntries.Add(entry);
                }
            }

            Type = type;
            try
            {
                Value = _classificatorEntries[valueIndex];
            } 
            catch (ArgumentOutOfRangeException)
            {
                Value = _classificatorEntries[0];
            }
        }

        public override string GetValueString()
        {
            return Value?.Value;
        }
    }
}
