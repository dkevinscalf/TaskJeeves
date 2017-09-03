using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace TaskJeeves
{
    public class DisplayTask : INotifyPropertyChanged
    {
        public WorkItem workItem;

        public int ID
        {
            get { return workItem.Id; }
        }

        public string Title
        {
            get { return workItem.Title; }

            set
            {
                if (value == workItem.Title) return;
                workItem.Title = value;
                NotifyPropertyChanged("Title");
                NotifyPropertyChanged("IDTitle");
            }
        }

        public string Area
        {
            get { return workItem.AreaPath; }

            set
            {
                if (value == workItem.AreaPath) return;
                workItem.AreaPath = value;
                NotifyPropertyChanged("Area");
            }
        }

        public string AssignedTo
        {
            get { return GetField("System.AssignedTo"); }

            set
            {
                SetField("System.AssignedTo", value);
                NotifyPropertyChanged("AssignedTo");
            }
        }

        public string Description
        {
            get
            {
                if (Type == "Bug")
                {
                    var repro = GetField("Repro Steps");
                    repro= Regex.Replace(repro, "<.*?>", string.Empty);
                    repro = repro.Replace("&nbsp;", "");
                    return repro;
                }

                return workItem.Description;
            }
            set
            {
                if (value == workItem.Description) return;
                if (Type == "Bug")
                    SetField("Repro Steps", value);
                else
                    workItem.Description = value;
                NotifyPropertyChanged("Description");
            }
        }

        public string Type
        {
            get { return workItem.Type.Name; }
        }

        public List<int> Links
        {
            get
            {
                var Links = new List<int>();
                if (workItem.WorkItemLinks != null)
                {
                    foreach (WorkItemLink link in workItem.WorkItemLinks)
                    {
                        Links.Add(link.TargetId);
                    }
                }
                return Links;
            }
        }

        public string State
        {
            get { return workItem.State; }

            set
            {
                if (value == workItem.State) return;
                workItem.State = value;
                BuildFieldList();
                NotifyPropertyChanged("State");
            }
        }

        public List<string> StateList
        {
            get
            {
                switch (Type)
                {
                    case "User Story":
                        return UICommon.GetProperty("UserStoryStates") as List<string>;
                        break;
                    case "Task":
                        return UICommon.GetProperty("TaskStates") as List<string>;
                        break;
                    case "Bug":
                        return UICommon.GetProperty("BugStates") as List<string>;
                        break;
                }

                return null;
            }
        }  

        public string Iteration
        {
            get { return workItem.IterationPath; }

            set
            {
                if (value == workItem.IterationPath) return;
                workItem.IterationPath = value;
                NotifyPropertyChanged("Iteration");
            }
        }
        public string Estimated
        {
            get
            {
                return GetField("Original Estimate");
            }

            set
            {
                SetField("Original Estimate", value);
                NotifyPropertyChanged("Estimated");
            }
        }

        

        public string Completed
        {
            get
            {
                return GetField("Completed Work");
            }

            set
            {
                SetField("Completed Work", value);
                NotifyPropertyChanged("Completed");
            }
        }

        public string Remaining
        {
            get
            {
                return GetField("Remaining Work");
            }

            set
            {
                SetField("Remaining Work", value);
                NotifyPropertyChanged("Remaining");
            }
        }

        public string TypeColor
        {
            get
            {
                if (isPlaceHolder)
                    return "White";

                switch (Type)
                {
                    case "Task":
                        return "#FFE9FF5C";
                    case "User Story":
                        return "#FF5CFF63";
                    case "Bug":
                        return "#FFF9887D";
                    default:
                        return "White";
                }
            }
        }

        public string IDTitle
        {
            get
            {
                if (ID <= 0)
                {
                    return Title;
                }
                return ID + " - " + Title;
            }
        }

        private string borderColor;
        public string BorderColor
        {
            get { return borderColor; }

            set
            {
                if (value == borderColor) return;
                borderColor = value;
                NotifyPropertyChanged("BorderColor");
            }
        }

        public string DefaultBorderColor => "#FF363636";
        public ObservableCollection<DisplayTask> LinkedTasks { get; set; }

        public ObservableCollection<TaskAttachment> Attachments { get; set; }

        public ObservableCollection<DisplayTaskField> Fields { get; set; }
        public bool isPlaceHolder { get; set; }

        public TaskUpdate Update { get; set; }

        public DisplayTask(string taskType)
        {
            var WIC = UICommon.GetProperty("WIC") as WorkItemController;

            workItem = WIC.GetNewWorkItem(taskType);
            workItem["Stack Rank"] = 500;
            LinkedTasks = new ObservableCollection<DisplayTask>();
            Attachments = new ObservableCollection<TaskAttachment>();
            Update = new TaskUpdate();
            borderColor = DefaultBorderColor;
            BuildFieldList();
        }

        private void BuildFieldList()
        {
            Fields = new ObservableCollection<DisplayTaskField>();
            foreach (Field field in workItem.Fields)
            {
                if(field.Name != "State" && field.Name != "Iteration Path" && field.Name != "Iteration ID" && field.Name != "System.AssignedTo" && field.Name != "Title" && field.Name != "Area Path" && field.Name != "Area ID" && field.Name != "Original Estimate" && field.Name != "Remaining Work" && field.Name != "Completed Work" && field.Name != "Description" && field.Name != "Repro Steps")
                    Fields.Add(new DisplayTaskField { Key = field.Name, IsEditable = field.IsEditable, AcceptsAllowedValues = field.HasAllowedValuesList, AllowedValues = GetAllowedValues(field.AllowedValues), Value = workItem[field.Name]==null?"":workItem[field.Name].ToString() });
            }
        }

        private List<string> GetAllowedValues(AllowedValuesCollection allowedValues)
        {
            var values = new List<string>();
            foreach (var value in allowedValues)
            {
                values.Add(value.ToString());
            }

            return values;
        }

        public DisplayTask(WorkItem workItem)
        {
            this.workItem = workItem;
            LinkedTasks = new ObservableCollection<DisplayTask>();
            Attachments = new ObservableCollection<TaskAttachment>();
            Update = new TaskUpdate();

            Attachments = new ObservableCollection<TaskAttachment>();

            if (workItem.Attachments != null)
            {
                foreach (Attachment attachment in workItem.Attachments)
                {
                    Attachments.Add(new TaskAttachment(attachment, this));
                }
            }

            borderColor = DefaultBorderColor;
            BuildFieldList();
        }

        private string GetField(string field)
        {
            return workItem.Fields.Contains(field)
                                ? workItem[field]?.ToString() ?? string.Empty
                                : string.Empty;
        }

        private void SetField(string field, object value)
        {
            if (workItem.Fields.Contains(field))
            {
                workItem[field] = value;
            }
            else
            {
                //do nothing?
            }
        }

        public void SaveUpdates(WorkItemController WIC)
        {
            SaveFieldData();
            Update.DisplayTask = this;
            Update.Type = this.Type;
            WIC.QueueTaskUpdate(Update);
        }

        private void SaveFieldData()
        {
            foreach (var field in Fields)
            {
                try
                {
                    var value = workItem[field.Key] == null ? "" : workItem[field.Key].ToString();
                    if (field.IsEditable && value != field.Value)
                        workItem[field.Key] = field.Value;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged; 

        public void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public void AddAttachment(TaskAttachment taskAttachment)
        {
            Attachments.Add(taskAttachment);
        }
    }

    public class DisplayTaskField : INotifyPropertyChanged
    {
        private string _key;
        public string Key
        {
            get { return _key; }

            set
            {
                if (value == _key) return;
                _key = value;
                NotifyPropertyChanged("Key");
                NotifyPropertyChanged("DisplayName");
            }
        }

        public string DisplayName
        {
            get { return _key.Replace("System.", ""); }
        }

        private string _value;
        public string Value
        {
            get { return _value; }

            set
            {
                if (value == _value) return;
                _value = value;
                NotifyPropertyChanged("Value");
            }
        }

        public bool IsEditable { get; set; }

        public Visibility TextVisibility
        {
            get
            {
                if (!IsEditable || !AcceptsAllowedValues)
                {
                    return Visibility.Visible;
                }

                return Visibility.Hidden;
            }
        }

        public Visibility DropDownVisibility
        {
            get
            {
                if (!IsEditable || !AcceptsAllowedValues)
                {
                    return Visibility.Hidden;
                }

                return Visibility.Visible;
            }
        }

        public bool AcceptsAllowedValues { get; set; }

        public List<string> AllowedValues { get; set; } 

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
