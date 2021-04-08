using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SUNTDLite.ViewModel
{
    public abstract class BaseVM : ApplicationLogger, INotifyPropertyChanged
    {
        public BaseVM(string LogTag) : base(LogTag) {}

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
