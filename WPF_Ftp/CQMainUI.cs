using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace WPF_Ftp
{
    public class CQMainUI:INotifyPropertyChanged
    {
        public ObservableCollection<string> HandSharks { set; get; }
        public CQMainUI()
        {
            this.HandSharks = new ObservableCollection<string>();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        void Update(string name) { if (this.PropertyChanged != null) { this.PropertyChanged(this, new PropertyChangedEventArgs(name)); } }
    }
}
