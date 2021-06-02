using SUNTDLite.Services.KBDAPIServiceModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace SUNTDLite.View
{
    public class DocumentClassificatorExp : DocumentType
    {
        public class DocumentClassificatorEntry : INotifyPropertyChanged
        {
            public long Oid { get; set; }
            public int ChildLevel { get; set; }
            public DocumentClassificatorEntry(string name, long oid, int childLevel, bool isChecked = false)
            {
                ClassificatorName = name;
                Oid = oid;
                ChildLevel = childLevel;
                ClassificatorCheck = isChecked;
            }

            private bool _classificatorCheck;
            public bool ClassificatorCheck {
                get => _classificatorCheck;
                set {
                    _classificatorCheck = value;
                    OnPropertyChanged("ClassificatorCheck");
                }
            }

            public string _classificatorName;
            public string ClassificatorName
            {
                get
                {
                    if (ChildLevel > 0)
                    {
                        StringBuilder sb = new StringBuilder();
                        for(int i = 0; i < ChildLevel; i++)
                        {
                            sb.Append('\t');
                        }
                        sb.Append(_classificatorName);
                        return sb.ToString();
                    }
                    return _classificatorName;
                }
                set
                {
                    _classificatorName = value;
                }
            }   

            public event PropertyChangedEventHandler PropertyChanged;
            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
                    OnPropertyChanged("ClassificatorEntries");
                }
            }
        }
        
        public DocumentClassificatorEntry SelectedValue { get; set; }

        public List<DocumentClassificatorEntry> SelectedItems
        {
            get
            {
                return _classificatorEntries.Where(c => c.ClassificatorCheck).ToList();
            }
        }
                
        public string DisplayValue
        {
            get
            {
                if (_classificatorEntries.Any())
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (var entry in _classificatorEntries)
                    {
                        sb.Append(entry.ClassificatorName).Append(",");
                    }
                    sb.Remove(sb.Length - 1, 1);
                    return sb.ToString();
                }
                return string.Empty;
            }
        }
        public DocumentClassificatorExp(string name, long attributeNumber, int valueIndex, ClassificatorDescModel classificatorDescModel) : base(name, attributeNumber)
        {
            if (classificatorDescModel.Type == ClassificatorDescModel.ClassificatorDescModelType.Multiple)
            {
                foreach (var classItemValue in classificatorDescModel.Values.OrderBy(v => v.Name))
                {
                    AddClassificatorEntriesRecursive(_classificatorEntries, classItemValue, 0);
                }
            }
        }

        private void AddClassificatorEntriesRecursive(List<DocumentClassificatorEntry> classificatorEntries, 
            ClassificatorDescModel.ClassificatorDescModelItem childItem, int level = 0)
        {
            _classificatorEntries.Add(new DocumentClassificatorEntry(childItem.Name, childItem.Oid, level));
            if (childItem.Childrens != null && childItem.Childrens.Count > 0)
            {
                foreach(var childSubItem in childItem.Childrens)
                {
                    AddClassificatorEntriesRecursive(classificatorEntries, childSubItem, level + 1);
                }
            }            
        }

        public override IEnumerable<string> GetValueString()
        {
            var checkedItems = _classificatorEntries.Where(c => c.ClassificatorCheck == true).Select(c => c._classificatorName);
            return checkedItems;
        }

        public override void Clean()
        {
            foreach(var item in _classificatorEntries)
            {
                item.ClassificatorCheck = false;
            }
            OnPropertyChanged("ClassificatorEntries");
        }
    }
}
