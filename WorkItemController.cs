using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace TaskJeeves
{
    public class WorkItemController : INotifyPropertyChanged
    {
        private TfsTeamProjectCollection tpc;
        private WorkItemStore workItemStore;
        private Project workItemProject;

        private string TFSUser;
        private string TFSServer;

        private WorkItemLinkType parentLinkType;
        public ObservableCollection<DisplayTask> AllTasks; 

        public ObservableCollection<TaskUpdate> UpdateQueue;

        private DateTime lastRefresh;

        private MainWindow mainWindow;

        public WorkItemController()
        {
            TFSServer = ConfigurationManager.AppSettings["TFSUrl"];

            tpc = new TfsTeamProjectCollection(
              new Uri(TFSServer));
            try
            {
                workItemStore = (WorkItemStore) tpc.GetService(typeof (WorkItemStore));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to Connect to TFS. Check your VPN Connection. \nError " + ex.Message);
                Environment.Exit(-1);
                return;
            }

            parentLinkType = workItemStore.WorkItemLinkTypes.FirstOrDefault(l => l.ReverseEnd.Name == "Parent");
            
            workItemProject = workItemStore.Projects[0];

            GetAreas(workItemProject);
            GetIterations(workItemProject);
            GetUsers(tpc);
            GetStates();

            UpdateQueue = new ObservableCollection<TaskUpdate>();

            lastRefresh = DateTime.Now;

            mainWindow = UICommon.GetProperty("MainWindow") as MainWindow;
        }

        private void GetUsers(TfsTeamProjectCollection tpc)
        {
            IIdentityManagementService2 identityManagementService = tpc.GetService<IIdentityManagementService2>();
            var validUsers = identityManagementService.ReadIdentities(IdentitySearchFactor.AccountName, new[] { "Project Collection Valid Users" }, MembershipQuery.Expanded, ReadIdentityOptions.None)[0][0].Members;
            var users = identityManagementService.ReadIdentities(validUsers, MembershipQuery.None, ReadIdentityOptions.None).Where(x => !x.IsContainer).ToArray();
            var userNames = users.Select(u => u.DisplayName).ToList();

            userNames.Sort();

            UICommon.AddToProperties("Users", userNames);
        }
        

        private void GetAreas(Project project)
        {
            List<string> Areas = new List<string>();

            foreach (Node areaNode in project.AreaRootNodes)
            {
                GetNodes(areaNode, Areas);
            }

            UICommon.AddToProperties("Areas", Areas);
        }

        

        private void GetIterations(Project project)
        {
            List<string> Iterations = new List<string>();

            foreach (Node IterationNode in project.IterationRootNodes)
            {
                GetNodes(IterationNode, Iterations);
            }

            Iterations.Sort();
            Iterations.Reverse();

            UICommon.AddToProperties("Iterations", Iterations);
        }

        private void GetStates()
        {
            List<string> UserStoryStates = new List<string> {"Active", "New", "Closed", "Removed", "Resolved"};
            List<string> TaskStates = new List<string> { "Active", "New", "Closed", "Removed"};
            List<string> BugStates = new List<string> { "Active", "Resolved" };

            UICommon.AddToProperties("UserStoryStates", UserStoryStates);
            UICommon.AddToProperties("TaskStates", TaskStates);
            UICommon.AddToProperties("BugStates", BugStates);
        }

        private void GetNodes(Node thisNode, List<string> paths)
        {
            paths.Add(thisNode.Path);
            foreach (Node child in thisNode.ChildNodes)
            {
                GetNodes(child, paths);
            }
        }

        public WorkItem GetWorkItemByID(int id)
        {
            return workItemStore.GetWorkItem(id); ;
        }
        public WorkItemCollection GetAllWorkItems()
        {
            TFSUser = ConfigurationManager.AppSettings["TFSUser"];
            UICommon.AddToProperties("MainUser", TFSUser);

            var retensionPeriod = Convert.ToInt32(ConfigurationManager.AppSettings["TFSRetention"]);
            var retensionDate = DateTime.Now.Date.AddDays(retensionPeriod*-1);

            // Run a query.
            WorkItemCollection queryResults = workItemStore.Query(
                "Select *" +
                "From WorkItems " +
                "Where [Assigned To] = '" + TFSUser + "' " +
                "AND (([State] <> 'Closed' AND [State] <> 'Resolved' AND [State] <> 'Removed') OR [System.CreatedDate] > '" + retensionDate + "')");

            return queryResults;
        }

        public void QueueTaskUpdate(TaskUpdate task)
        {
            UpdateQueue.Add(task);
            mainWindow.UpdateQueueCount();
        }

        public async void ProcessQueue()
        {
            Task task = Task.Run(() => {; });
            Task nextTask = task.ContinueWith(x =>
            {
                while (true)
                {
                    try
                    {
                        mainWindow.UpdateQueueProgress(0, 0);
                        if (UpdateQueue.Count > 0)
                        {
                            if (UpdateQueue[0].IsReadRequest)
                            {
                                mainWindow.UpdateQueueProgress(-1, 1);
                                var workItems = GetAllWorkItems();
                                mainWindow.UpdateQueueProgress(0, workItems.Count);

                                var tasks = new ObservableCollection<DisplayTask>();

                                var i = 0;

                                foreach (WorkItem workItem in workItems)
                                {
                                    var newTask = new DisplayTask(workItem);
                                    //newTask.LinkedTasks = GetRelatedWorkItems(workItem);
                                    tasks.Add(newTask);
                                    i++;
                                    mainWindow.UpdateQueueProgress(i, workItems.Count);
                                }

                                mainWindow.ReloadTasks(tasks);
                                mainWindow.UpdateQueueProgress(0, 0);

                                lastRefresh = DateTime.Now;
                            }
                            else
                            {
                                mainWindow.UpdateQueueProgress(-1, 1);

                                var savedCorrectly = SaveWorkItem(UpdateQueue[0]);

                                if (!savedCorrectly)
                                {
                                    mainWindow.UpdateQueueProgress(0, 0);
                                    return;
                                }

                                if (UpdateQueue[0].DisplayTask != null)
                                {
                                    if (!AllTasks.Contains(UpdateQueue[0].DisplayTask))
                                    {
                                        if (mainWindow != null)
                                            mainWindow.AddTaskFromThread(UpdateQueue[0].DisplayTask);
                                    }
                                    else
                                    {
                                        mainWindow.FilterTasksFromThread();
                                    }
                                }
                                mainWindow.UpdateQueueProgress(0, 0);
                            }

                            if (UpdateQueue[0].DisplayTask != null)
                                UpdateQueue[0].DisplayTask.NotifyPropertyChanged("ID");

                            UpdateQueue.RemoveAtOnUI(0);
                        }
                        else if ((DateTime.Now - lastRefresh).Seconds >
                                 Convert.ToInt32(ConfigurationManager.AppSettings["TFSUpdateInterval"])*60)
                        {
                            UpdateQueue.AddOnUI(new TaskUpdate {ID = -1, IsReadRequest = true});
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            "An error was encountered. Whatever you were doing may have to be reattempted. \n" +
                            ex.Message);
                    }
                    Thread.Sleep(1000);
                }
                
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }

        public ObservableCollection<DisplayTask> GetRelatedWorkItems(WorkItem workItem)
        {
            var Links = new ObservableCollection<DisplayTask>();
            if (workItem.WorkItemLinks != null)
            {
                foreach (WorkItemLink link in workItem.WorkItemLinks)
                {
                    Links.Add(new DisplayTask(GetWorkItemByID(link.TargetId)));
                }
            }
            return Links;
        }

        private bool SaveWorkItem(TaskUpdate taskUpdate)
        {
            var workItem = taskUpdate.DisplayTask.workItem;

            //Add nonexistant attachments
            foreach (var attachment in taskUpdate.DisplayTask.Attachments)
            {
                var found = false;
                foreach (Attachment workItemAttachment in workItem.Attachments)
                {
                    if (attachment.FileName == workItemAttachment.Name)
                        found = true;
                }

                if (!found)
                {
                    workItem.Attachments.Add(attachment.Attachment);
                }
            }

            //Delete removed attachments
            var removedAttachments = new List<Attachment>();
            foreach (Attachment workItemAttachment in workItem.Attachments)
            {
                var found = false;
                foreach (var attachment in taskUpdate.DisplayTask.Attachments)
                {
                    if (attachment.FileName == workItemAttachment.Name)
                        found = true;
                }

                if (!found)
                {
                    removedAttachments.Add(workItemAttachment);
                }
            }

            foreach (var removedAttachment in removedAttachments)
            {
                workItem.Attachments.Remove(removedAttachment);
            }

            var savedCorrectly = SaveWorkItem(workItem);

            if (savedCorrectly && workItem.Id != 0 && taskUpdate.ParentID != 0)
            {
                SaveWorkItemToParent(workItem.Id, taskUpdate.ParentID);
            }

            return savedCorrectly;
        }

        private static bool SaveWorkItem(WorkItem workItem)
        {
            var invalidFields = workItem.Validate();
            if (invalidFields.Count > 0)
            {
                foreach (Field field in invalidFields)
                {
                    string errorMessage = string.Empty;
                    if (field.Status == FieldStatus.InvalidEmpty)
                    {

                    }

                    errorMessage = string.Format("{0} {1} {2}: TF20012: field \"{3}\" {4}."
                            , field.WorkItem.State
                            , field.WorkItem.Type.Name
                            , field.WorkItem.TemporaryId
                            , field.Name
                            , field.Status);

                    MessageBox.Show(field.Status + " " + errorMessage);
                }
            }
            else // Validation passed
            {
                workItem.Save();
                return true;
            }
            return false;
        }

        private void SaveWorkItemToParent(int id, int parentId)
        {
            WorkItem parent = workItemStore.GetWorkItem(parentId);

            parent.Links.Add(new RelatedLink(parentLinkType.ForwardEnd, id));
            parent.Save();
        }

        public WorkItem GetNewWorkItem(string type)
        {
            WorkItemType workItemType = workItemProject.WorkItemTypes[type];

            return new WorkItem(workItemType) { };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public void CopyTaskToIteration(DisplayTask task, string iteration)
        {
            var workItem = GetWorkItemByID(task.ID);
            var newWorkItem = workItem.Copy();
            foreach (Attachment attachment in workItem.Attachments)
            {
                var taskAttachment = new TaskAttachment(attachment, task);
                var tempFileName = taskAttachment.DownloadFile();
                newWorkItem.Attachments.Add(new Attachment(tempFileName));
            }
            newWorkItem.IterationPath = iteration;

            SaveWorkItem(newWorkItem);

            AllTasks.Add(new DisplayTask(newWorkItem));
        }
    }
}
