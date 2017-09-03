using System;
using System.Collections.Generic;
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
    /// Interaction logic for BugWindow.xaml
    /// </summary>
    public partial class BugWindow : Window
    {
        public BugWindow()
        {
            InitializeComponent();
        }

        private void btnBugOpenApp_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = UICommon.GetProperty("MainWindow") as MainWindow;

            mainWindow.Show();
            this.Close();
        }

        private void btnBugNewTask_Click(object sender, RoutedEventArgs e)
        {
            var newTask = new DisplayTask("Task") { Title = "New Task" };
            var detailWindow = new WorkItemDetails(newTask);
            detailWindow.Show();
        }

        private void btnBugIgnore_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
