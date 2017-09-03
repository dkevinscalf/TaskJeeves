using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskJeeves
{
    public class TaskUpdate : INotifyPropertyChanged
    {
        public TaskUpdate() { Fields = new ObservableCollection<TaskUpdateField>();}

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public int ID { get; set; }

        public string Type { get; set; }

        public int ParentID { get; set; }

        public bool IsReadRequest { get; set; }

        public string UpdateDescription
        {
            get
            {
                if (ID == -1)
                {
                    return "Task DB Refresh";
                }
                var sb = new StringBuilder();
                foreach (var field in Fields)
                {
                    sb.Append(string.Format("{0}:{1},", field.Key, field.Value));
                }

                return sb.ToString();
            }
        }

        public DisplayTask DisplayTask { get; set; }

        public ObservableCollection<TaskUpdateField> Fields;
    }

    public class TaskUpdateField : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private string _key;
        public string Key
        {
            get { return _key; }
            set { _key = value; NotifyPropertyChanged("Key"); }
        }

        private object _value;
        public object Value
        {
            get { return _value; }
            set { _value = value; NotifyPropertyChanged("Value"); }
        }
    }
}
