using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskJeeves
{
    public class Preferences : INotifyPropertyChanged
    {
        private string tfsUser;
        public string TFSUser
        {
            get { return tfsUser; }

            set
            {
                if (value == tfsUser) return;
                tfsUser = value;
                NotifyPropertyChanged("TFSUser");
            }
        }

        private string tfsUrl;
        public string TFSUrl
        {
            get { return tfsUrl; }

            set
            {
                if (value == tfsUrl) return;
                tfsUrl = value;
                NotifyPropertyChanged("TFSUrl");
            }
        }

        private string tfsRefresh;
        public string TFSRefresh
        {
            get { return tfsRefresh; }

            set
            {
                if (value == tfsRefresh) return;
                tfsRefresh = value;
                NotifyPropertyChanged("TFSRefresh");
            }
        }

        private string tfsRetention;
        public string TFSRetention
        {
            get { return tfsRetention; }

            set
            {
                if (value == tfsRetention) return;
                tfsRetention = value;
                NotifyPropertyChanged("TFSRetention");
            }
        }

        private string bugMe;
        public bool BugMe
        {
            get { return bugMe == "True"; }

            set
            {
                bugMe = value ? "True" : "False";
                NotifyPropertyChanged("BugMe");
            }
        }

        private string bugTimer;
        public string BugTimer
        {
            get { return bugTimer; }

            set
            {
                if (value == bugTimer) return;
                bugTimer = value;
                NotifyPropertyChanged("BugTimer");
            }
        }

        public Preferences()
        {
            tfsUrl = ConfigurationManager.AppSettings["TFSUrl"];
            tfsUser = ConfigurationManager.AppSettings["TFSUser"];
            tfsRefresh = ConfigurationManager.AppSettings["TFSUpdateInterval"];
            tfsRetention = ConfigurationManager.AppSettings["TFSRetention"];
            bugMe = ConfigurationManager.AppSettings["BugMe"];
            bugTimer = ConfigurationManager.AppSettings["BugTimer"];
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public void Save()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            //make changes
            config.AppSettings.Settings["TFSUrl"].Value = TFSUrl;
            config.AppSettings.Settings["TFSUser"].Value = TFSUser;
            config.AppSettings.Settings["TFSUpdateInterval"].Value = TFSRefresh;
            config.AppSettings.Settings["TFSRetention"].Value = TFSRetention;
            config.AppSettings.Settings["BugMe"].Value = bugMe;
            config.AppSettings.Settings["BugTimer"].Value = bugTimer;

            //save to apply changes
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
