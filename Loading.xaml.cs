using Microsoft.TeamFoundation.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.TeamFoundation.Client.Internal;
using Microsoft.TeamFoundation.WorkItemTracking.Client;

namespace TaskJeeves
{
    /// <summary>
    /// Interaction logic for Loading.xaml
    /// </summary>
    public partial class Loading : Window
    {
        public Loading()
        {
            InitializeComponent();

            LoadData();
        }

        public async void LoadData()
        {
            prgLoadingBar.IsIndeterminate = true;

            await Task.Delay(20);

            var WIC = new WorkItemController();

            var workItems = WIC.GetAllWorkItems();

            prgLoadingBar.IsIndeterminate = false;

            prgLoadingBar.Maximum = workItems.Count;

            prgLoadingBar.Value = 0;

            await Task.Delay(20);

            var tasks = new ObservableCollection<DisplayTask>();

            foreach (WorkItem workItem in workItems)
            {

                txtLoadingStatus.Text = string.Format("AreaPath: {0} Task Number: {1} Task Name: {2}", workItem.AreaPath,
                    workItem.Id, workItem.Title);

                prgLoadingBar.Value++;

                var newTask = new DisplayTask(workItem);
                newTask.LinkedTasks = WIC.GetRelatedWorkItems(workItem);
                tasks.Add(newTask);

                await Task.Delay(1);
            }

            var mainWindow = new MainWindow(tasks);
            Hide();
            mainWindow.Show();
        }
    }
}
