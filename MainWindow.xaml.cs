using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using CheckBox = System.Windows.Forms.CheckBox;
using ContextMenu = System.Windows.Forms.ContextMenu;
using DataObject = System.Windows.DataObject;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using MenuItem = System.Windows.Forms.MenuItem;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace TaskJeeves
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<DisplayTask> AllTasks;
        private ObservableCollection<string> Areas;

        private Point startPoint;

        private ObservableCollection<DisplayTask> NewTasks = new ObservableCollection<DisplayTask>();
        private ObservableCollection<DisplayTask> ActiveTasks = new ObservableCollection<DisplayTask>();
        private ObservableCollection<DisplayTask> CompleteTasks = new ObservableCollection<DisplayTask>();

        private ObservableCollection<DisplayTask> UserStories = new ObservableCollection<DisplayTask>();
        private ObservableCollection<DisplayTask> UnestimatedTasks = new ObservableCollection<DisplayTask>();
        private ObservableCollection<DisplayTask> PlanningTasks = new ObservableCollection<DisplayTask>();

        private DisplayTask PlanningSelectedUserStory;
        private DisplayTask PlanningSelectedTask;

        private WorkItemController WIC;
        public DispatcherTimer BugTimer;

        private Preferences pref;

        public MainWindow(ObservableCollection<DisplayTask> allTasks)
        {
            pref = new Preferences();

            InitializeComponent();

            CreateSystemTray();

            CreateBugTimer();

            UICommon.AddToProperties("MainWindow", this);

            AllTasks = allTasks;

            WIC = new WorkItemController();
            WIC.AllTasks = AllTasks;

            WIC.ProcessQueue();

            Application.Current.Properties["WIC"] = WIC;

            lbUpdateQueue.ItemsSource = WIC.UpdateQueue;

            LoadData("All");
        }

        private void LoadData(string filter)
        {
            UserStories = new ObservableCollection<DisplayTask>(AllTasks.Where(t => t.Type == "User Story").OrderByDescending(o => o.Iteration).ThenBy(o => o.Area));

            var UnestimatedTask = new DisplayTask("User Story") { isPlaceHolder = true, Title = "Unestimated Tasks" };
            UserStories.Insert(0, UnestimatedTask);

            UnestimatedTasks = new ObservableCollection<DisplayTask>(AllTasks.Where(t => t.Type != "User Story" && (string.IsNullOrEmpty(t.Estimated) || t.Estimated == "0")));

            PlanningTasks = new ObservableCollection<DisplayTask>(AllTasks.Where(t => t.Type == "User Story" || (string.IsNullOrEmpty(t.Estimated) || t.Estimated == "0")));

            var selectedArea = (string)cbArea.SelectedItem;
            var selectedAreaString = selectedArea == null ? "" : selectedArea.ToString();

            Areas = new ObservableCollection<string>();
            Areas.Add("All");

            cbArea.ItemsSource = Areas;

            foreach (DisplayTask task in AllTasks)
            {
                if (!Areas.Contains(task.Area))
                {
                    Areas.Add(task.Area);
                }
            }

            if (Areas.Contains(selectedArea))
                cbArea.SelectedValue = selectedArea;
            else
                cbArea.SelectedValue = "All";

            cbArea.ItemsSource = Areas;

            FilterTasks(filter);
        }

        private void CreateBugTimer()
        {
            BugTimer = new System.Windows.Threading.DispatcherTimer();
            BugTimer.Tick += bugTimer_Tick;

            var minutes = Convert.ToInt32(pref.BugTimer);
            menuBugMe.IsChecked = pref.BugMe;

            BugTimer.Interval = new TimeSpan(0, minutes, 0);
            BugTimer.Start();
        }
        private void bugTimer_Tick(object sender, EventArgs e)
        {
            if (!pref.BugMe)
            {
                return;
            }
            var bugger = new BugWindow();
            bugger.Show();
        }

        private void CreateSystemTray()
        {
            NotifyIcon ni = new NotifyIcon();
            Stream iconStream = Application.GetResourceStream(new Uri(@"pack://application:,,,/TaskJeeves;component/Images/TaskJeevesIcon.ico")).Stream;
            ni.Icon = new System.Drawing.Icon(iconStream);
            ni.Visible = true;
            ni.DoubleClick +=
                delegate (object sender, EventArgs args)
                {
                    Show();
                    WindowState = WindowState.Normal;
                };

            ni.ContextMenu = new ContextMenu(new MenuItem[] {new MenuItem("Quit Task Jeeves", delegate(object sender, EventArgs args)
            {
                Application.Current.Shutdown(110);
            })});
        }

        public delegate void FilterTaskCallBack(string areaName);
        private void FilterTasks(string areaName)
        { 
            if (string.IsNullOrEmpty(areaName))
                areaName = (string)cbArea.SelectedItem;
            NewTasks = new ObservableCollection<DisplayTask>(AllTasks.Where(t => t.State == "New" && t.Type == "Task" && (t.Area == areaName || areaName == "All")));
            ActiveTasks = new ObservableCollection<DisplayTask>(AllTasks.Where(t => t.State == "Active" && t.Type != "User Story" && (t.Area == areaName || areaName == "All")));
            CompleteTasks = new ObservableCollection<DisplayTask>(AllTasks.Where(t => (t.State == "Complete" || t.State == "Resolved" || t.State == "Closed") && (t.Area == areaName || areaName == "All")));
            BindTaskData();
        }

        public void FilterTasksFromThread()
        {
            icNewTaskList.Dispatcher.Invoke(new FilterTaskCallBack(FilterTasks), new object[] { string.Empty });
        }

        public delegate void AddTaskCallBack(DisplayTask task);
        private void AddTask(DisplayTask task)
        {
            AllTasks.Add(task);
            FilterTasks(null);
        }

        public void AddTaskFromThread(DisplayTask task)
        {
            icNewTaskList.Dispatcher.Invoke(new AddTaskCallBack(AddTask), new object[] { task });
        }

        private ObservableCollection<DisplayTask> GetAssociatedTasks(DisplayTask parentTask)
        {
            return new ObservableCollection<DisplayTask>(AllTasks.Where(t=>parentTask.Links.Contains(t.ID)));
        }

        private void BindTaskData()
        {
            icNewTaskList.ItemsSource = NewTasks;
            icActiveTaskList.ItemsSource = ActiveTasks;
            icCompleteTaskList.ItemsSource = CompleteTasks;

            icPlanningUserStories.ItemsSource = UserStories;

            dgMoveTasks.ItemsSource = AllTasks;
            ddMassMoveAreas.ItemsSource = UICommon.GetProperty("Areas") as List<string>;
            ddMassMoveUsers.ItemsSource = UICommon.GetProperty("Users") as List<string>;
            ddMassMoveIterations.ItemsSource = UICommon.GetProperty("Iterations") as List<string>;

            ddIterationCopySource.ItemsSource = UICommon.GetProperty("Iterations") as List<string>;
            ddIterationCopyDestination.ItemsSource = UICommon.GetProperty("Iterations") as List<string>;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            
        }

        private void cbArea_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterTasks((string)cbArea.SelectedItem);
        }

        private void GetDragTask(object sender, MouseEventArgs e, string taskType)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = startPoint - mousePos;

                if (
                    Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {

                    var itemsControl = sender as ItemsControl;
                    var item = UICommon.FindAncestor<TaskControl>((DependencyObject)e.OriginalSource);
                    if (item == null || item.DataContext == null)
                    {
                        return;
                    }
                    var displayTask = (DisplayTask)item.DataContext;

                    DataObject dragData = new DataObject(taskType, displayTask);
                    DragDrop.DoDragDrop(item, dragData, DragDropEffects.Move);
                }
            }
        }
        private void MoveTask(DragEventArgs e, string taskType, string moveType, ObservableCollection<DisplayTask> source, ObservableCollection<DisplayTask> destination)
        {
            if (e.Data.GetDataPresent(taskType))
            {
                var task = e.Data.GetData(taskType) as DisplayTask;
                if (QueueMoveTask(moveType, task))
                {
                    destination.Add(task);
                    source.Remove(task);
                }
            }
        }

        private bool QueueMoveTask(string moveType, DisplayTask task)
        {
            var state = "";
            bool isComplete = false;
            switch (moveType)
            {
                    
                case "new":
                    if (task.Type == "Bug")
                    {
                        MessageBox.Show("You can't move a bug to NEW");
                        return false;
                    }
                    else
                    {
                        state = "New";
                    }               
                    break;
                case "active":
                    state = "Active";
                    break;
                case "complete":
                    isComplete = true;
                    if (task.Type == "Task")
                    {
                        state = "Closed";
                    }
                    else
                    {
                        state = "Resolved";
                    }
                    break;
                default:
                    MessageBox.Show("Unknown Data Type");
                    return false;
            }

            task.State = state;
            if (isComplete && task.Type != "User Story")
            {
                var estimateHours = string.IsNullOrEmpty(task.Estimated) ? "0" : task.Estimated;

                task.Remaining = "0";
            }

            task.SaveUpdates(WIC);
            return true;
        }

        private void generic_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
        }

        private void icNewTaskList_MouseMove(object sender, MouseEventArgs e)
        {
            GetDragTask(sender, e, "newTask");
        }

        private void icNewTaskList_Drop(object sender, DragEventArgs e)
        {
            MoveTask(e, "activeTask", "new", ActiveTasks, NewTasks);
            MoveTask(e, "completeTask", "new", CompleteTasks, NewTasks);
            BindTaskData();
        }

        private void icActiveTaskList_MouseMove(object sender, MouseEventArgs e)
        {
            GetDragTask(sender, e, "activeTask");
        }
        private void icActiveTaskList_Drop(object sender, DragEventArgs e)
        {
            MoveTask(e, "newTask", "active", NewTasks, ActiveTasks);
            MoveTask(e, "completeTask", "active", CompleteTasks, ActiveTasks);
            BindTaskData();
        }

        private void icCompleteTaskList_MouseMove(object sender, MouseEventArgs e)
        {
            GetDragTask(sender, e, "completeTask");
        }

        private void icCompleteTaskList_Drop(object sender, DragEventArgs e)
        {
            MoveTask(e, "newTask", "complete", NewTasks, CompleteTasks);
            MoveTask(e, "activeTask", "complete", ActiveTasks, CompleteTasks);
            BindTaskData();
        }

        private void icPlanningUserStories_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var itemsControl = sender as ItemsControl;
            var item = UICommon.FindAncestor<TaskControl>((DependencyObject)e.OriginalSource);
            if (item == null || item.DataContext == null)
            {
                return;
            }

            foreach (DisplayTask task in itemsControl.ItemsSource)
            {
                task.BorderColor = task.DefaultBorderColor;
            }

            PlanningSelectedUserStory = (DisplayTask)item.DataContext;
            PlanningSelectedUserStory.BorderColor = "Red";

            if (PlanningSelectedUserStory.ID == -1)
            {
                icPlanningTasks.ItemsSource = UnestimatedTasks;
            }
            else
            {
                PlanningSelectedUserStory.LinkedTasks = WIC.GetRelatedWorkItems(PlanningSelectedUserStory.workItem);
                icPlanningTasks.ItemsSource = PlanningSelectedUserStory.LinkedTasks;
            }
        }

        private void icPlanningTasks_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var itemsControl = sender as ItemsControl;
            var item = UICommon.FindAncestor<TaskControl>((DependencyObject)e.OriginalSource);
            if (item == null || item.DataContext == null)
            {
                return;
            }

            foreach (DisplayTask task in itemsControl.ItemsSource)
            {
                task.BorderColor = task.DefaultBorderColor;
            }

            PlanningSelectedTask = (DisplayTask)item.DataContext;
            PlanningSelectedTask.BorderColor = "Red";

            planningTaskEditor.DataContext = PlanningSelectedTask;
        }

        private void EstimateButton_Click(object sender, RoutedEventArgs e)
        {
            if (planningTaskEditor.DataContext == null)
                return;

            var button = sender as Button;

            var estimate = Convert.ToDouble(button.Content.ToString());

            var task = (DisplayTask) planningTaskEditor.DataContext;

            task.Estimated = estimate.ToString();
            task.Remaining = estimate.ToString();
            task.Completed = "0";

            if (PlanningSelectedUserStory.ID == -1)
            {
                PlanningSelectedTask.SaveUpdates(WIC);
                UnestimatedTasks.Remove(PlanningSelectedTask);

                //icPlanningTasks.ItemsSource = UnestimatedTasks;
                if (icPlanningTasks.Items.Count > 0)
                {
                    PlanningSelectedTask = icPlanningTasks.Items[0] as DisplayTask;
                    PlanningSelectedTask.BorderColor = "Red";
                    planningTaskEditor.DataContext = PlanningSelectedTask;
                }
                else
                {
                    planningTaskEditor.DataContext = null;
                }
            }
        }

        private void btnSaveTask_Click(object sender, RoutedEventArgs e)
        {
            if (planningTaskEditor.DataContext == null)
                return;

            PlanningSelectedTask.SaveUpdates(WIC);
        }

        public delegate void UpdateQueueProgressCallBack(int value, int max);

        private void UpdateQueueBar(int value, int max)
        {
            prgQueueBar.Value = value;
            prgQueueBar.Maximum = max;

            prgQueueBar.Visibility = max == 0 ? Visibility.Collapsed : Visibility.Visible;

            prgQueueBar.IsIndeterminate = value == -1;

            UpdateQueueCount();
        }

        public void UpdateQueueProgress(int value, int max)
        {
            prgQueueBar.Dispatcher.Invoke(new UpdateQueueProgressCallBack(UpdateQueueBar), value, max);
        }

        public void UpdateQueueCount()
        {
            tiQueue.Header = string.Format("Queue({0})", lbUpdateQueue.Items.Count);
        }

        public delegate void ReloadTasksCallBack(ObservableCollection<DisplayTask> tasks);

        private void ReloadAllTasks(ObservableCollection<DisplayTask> tasks)
        {
            AllTasks = tasks;
            LoadData(null);
        }

        public void ReloadTasks(ObservableCollection<DisplayTask> tasks)
        {
            icNewTaskList.Dispatcher.Invoke(new ReloadTasksCallBack(ReloadAllTasks), tasks);
        }

        private void dgMoveTasks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedIteration = null;
            string selectedArea = null;
            string selectedState = null;
            string selectedUser = null;
            string selectedType = null;

            foreach (DisplayTask selectedTask in dgMoveTasks.SelectedItems)
            {
                selectedIteration = CompareSelection(selectedIteration, selectedTask.Iteration);
                selectedArea = CompareSelection(selectedArea, selectedTask.Area);
                selectedState = CompareSelection(selectedState, selectedTask.State);
                selectedUser = CompareSelection(selectedUser, selectedTask.AssignedTo);
                selectedType = CompareSelection(selectedType, selectedTask.Type);
            }

            ddMassMoveAreas.SelectedValue = selectedArea;
            ddMassMoveIterations.SelectedValue = selectedIteration;
            ddMassMoveUsers.SelectedValue = selectedUser;

            if (!string.IsNullOrEmpty(selectedType))
            {
                ddMassMoveStates.IsEnabled = true;
                var firstTask = dgMoveTasks.SelectedItems[0] as DisplayTask;
                ddMassMoveStates.ItemsSource = firstTask.StateList;
                ddMassMoveStates.SelectedValue = selectedState;
            }
            else
            {
                ddMassMoveStates.Text = "";
                ddMassMoveStates.IsEnabled = false;
            }
        }

        private static string CompareSelection(string selectedValue, string selection)
        {
            if (selectedValue == null)
            {
                selectedValue = selection;
            }
            else
            {
                if (selection != selectedValue)
                {
                    selectedValue = string.Empty;
                }
            }

            return selectedValue;
        }

        private void btnMassMoveSubmit_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to modify all these poor tasks? They trust you, you know.", "Mass Move Confirmation", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            foreach (DisplayTask selectedTask in dgMoveTasks.SelectedItems)
            {
                if (ddMassMoveIterations.SelectedValue != null && !string.IsNullOrEmpty(ddMassMoveIterations.SelectedValue.ToString()) &&
                    selectedTask.Iteration != ddMassMoveIterations.SelectedValue.ToString())
                {
                    selectedTask.Iteration = ddMassMoveIterations.SelectedValue.ToString();
                }

                if (ddMassMoveStates.SelectedValue != null && !string.IsNullOrEmpty(ddMassMoveStates.SelectedValue.ToString()) &&
                    selectedTask.State != ddMassMoveStates.SelectedValue.ToString())
                {
                    selectedTask.State = ddMassMoveStates.SelectedValue.ToString();
                }

                if (ddMassMoveAreas.SelectedValue != null && !string.IsNullOrEmpty(ddMassMoveAreas.SelectedValue.ToString()) &&
                    selectedTask.Area != ddMassMoveAreas.SelectedValue.ToString())
                {
                    selectedTask.Area = ddMassMoveAreas.SelectedValue.ToString();
                }

                if (ddMassMoveUsers.SelectedValue != null && !string.IsNullOrEmpty(ddMassMoveUsers.SelectedValue.ToString()) &&
                    selectedTask.AssignedTo != ddMassMoveUsers.SelectedValue.ToString())
                {
                    selectedTask.AssignedTo = ddMassMoveUsers.SelectedValue.ToString();
                }

                selectedTask.SaveUpdates(WIC);
            }
        }

        private void ddIterationCopySource_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(ddIterationCopySource.SelectedValue!=null)
                dgIterationCopyTasks.ItemsSource = AllTasks.Where(t => t.Iteration == ddIterationCopySource.SelectedValue.ToString());
        }

        private void btnCopyIteration_Click(object sender, RoutedEventArgs e)
        {
            if (ddIterationCopySource.SelectedValue == null || ddIterationCopyDestination.SelectedValue == null)
            {
                MessageBox.Show("Please select a source and destination");
                return;
            }

            if (dgIterationCopyTasks.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select tasks to be copied");
                return;
            }

            var result = MessageBox.Show("Are you sure you want to copy these tasks?", "Iteration Copy Confirmation", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            foreach (DisplayTask task in dgIterationCopyTasks.SelectedItems)
            {
                WIC.CopyTaskToIteration(task, ddIterationCopyDestination.SelectedValue.ToString());
                QueueMoveTask("complete", task);
            }
        }

        private void menuNewTask_Click(object sender, RoutedEventArgs e)
        {
            var newTask = new DisplayTask("Task") {Title="New Task"};
            var detailWindow = new WorkItemDetails(newTask);
            detailWindow.Show();
        }

        private void menuNewBug_Click(object sender, RoutedEventArgs e)
        {
            var newTask = new DisplayTask("Bug") {Title = "New Bug" };
            var detailWindow = new WorkItemDetails(newTask);
            detailWindow.Show();
        }

        private void menuNewUserStory_Click(object sender, RoutedEventArgs e)
        {
            var newTask = new DisplayTask("User Story") {Title = "New User Story" };
            var detailWindow = new WorkItemDetails(newTask);
            detailWindow.Show();
        }

        private void menuPreferences_Click(object sender, RoutedEventArgs e)
        {
            var prefWindow = new EditPreferences();
            prefWindow.Show();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            Hide();
        }

        private void menuBugMe_Click(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as System.Windows.Controls.MenuItem;

            pref.BugMe = checkbox.IsChecked;
            pref.Save();
        }
    }
}
