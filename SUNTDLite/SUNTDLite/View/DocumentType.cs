using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace SUNTDLite.View
{
    public abstract class DocumentType : INotifyPropertyChanged
    {
        public event Action OnNameChanged;
        public DocumentType() { }
        public DocumentType(string name, long documentAttributeNumber)
        {
            Name = name;
            DocumentAttributeNumber = documentAttributeNumber;
        }

        public long DocumentAttributeNumber
        {
            get; private set;
        }

        private string _name;
        public string Name 
        { 
            get => _name; 
            set
            {
                _name = value;
                OnNameChanged?.Invoke();
            } 
        }

        public abstract IEnumerable<string> GetValueString();

        public abstract void Clean();

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
