using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace TaskJeeves
{
    /// <summary>
    /// Interaction logic for WorkItemDetails.xaml
    /// </summary>
    public partial class WorkItemDetails : Window
    {
        private WorkItemController WIC;

        public WorkItemDetails(DisplayTask root)
        {
            InitializeComponent();

            WIC = Application.Current.Properties["WIC"] as WorkItemController;

            var tasks = new ObservableCollection<DisplayTask>(root.LinkedTasks);

            tasks.Insert(0,root);

            icDetailTasks.ItemsSource = tasks;

            detailTaskEditor.DataContext = root;
        }

        private void icDetailTasks_MouseDown(object sender, MouseButtonEventArgs e)
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
            var displayTask = (DisplayTask)item.DataContext;

            displayTask.BorderColor = "Red";

            detailTaskEditor.DataContext = displayTask;
        }

        private void btnSaveDetail_Click(object sender, RoutedEventArgs e)
        {
            var task = (DisplayTask) detailTaskEditor.DataContext;
            task.SaveUpdates(WIC);
        }

        private void btnNewLink_Click(object sender, RoutedEventArgs e)
        {
            var parentTask = detailTaskEditor.DataContext as DisplayTask;
            parentTask.BorderColor = "Aqua";

            var newTask = new DisplayTask("Task") {Title = "New Task", Area = parentTask.Area, Iteration = parentTask.Iteration, BorderColor = "Red"};
            newTask.Update.ParentID = parentTask.ID;
            newTask.Update.Type = "Task";
            var associatedTasks = icDetailTasks.ItemsSource as ObservableCollection<DisplayTask>;
            associatedTasks.Add(newTask);

            detailTaskEditor.DataContext = newTask;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var mainWindow = UICommon.GetProperty("MainWindow") as MainWindow;
            mainWindow.FilterTasksFromThread();
        }
    }
}
