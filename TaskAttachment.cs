using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace TaskJeeves
{
    public class TaskAttachment : INotifyPropertyChanged
    {
        private Attachment attachment;
        private DisplayTask parent;

        public TaskAttachment(Attachment attachment, DisplayTask parent)
        {
            this.attachment = attachment;
            this.parent = parent;
            NotifyPropertyChanged("FileName");
        }

        public TaskAttachment(string filePath, DisplayTask parent)
        {
            this.attachment = new Attachment(filePath);
            this.parent = parent;
            parent.AddAttachment(this);
            NotifyPropertyChanged("FileName");
        }

        public string FileName
        {
            get
            {
                return attachment.Name;
            }
        }

        public string FilePath
        {
            get { return attachment.Uri.ToString(); }
        }

        public Attachment Attachment
        {
            get
            {
                return attachment;
            }
        }

        public string DownloadFile()
        {
            System.Net.WebClient request = new System.Net.WebClient();

            request.Credentials = System.Net.CredentialCache.DefaultCredentials;


            var tempFileName = Path.Combine(Path.GetDirectoryName(Path.GetTempFileName()), FileName);
            request.DownloadFile(FilePath, tempFileName);
            return tempFileName;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public void Remove(WorkItemController WIC)
        {
            parent.Attachments.Remove(this);
            parent.SaveUpdates(WIC);
        }
    }
}
