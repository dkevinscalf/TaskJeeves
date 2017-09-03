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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TaskJeeves
{
    /// <summary>
    /// Interaction logic for TaskControl.xaml
    /// </summary>
    public partial class TaskControl : UserControl
    {
        public DisplayTask DisplayTaskSourceTask;

        public static readonly DependencyProperty DisplayTaskProperty =
            DependencyProperty.Register("DisplayTask", typeof(DisplayTask), typeof(TaskControl));

        public DisplayTask DisplayTaskSource
        {
            get
            {
                return GetValue(DisplayTaskProperty) as DisplayTask;
            }
            set
            {
                SetValue(DisplayTaskProperty, value);
            }
        }

        public TaskControl()
        {
            InitializeComponent();

            //var defaultTask = new DisplayTask {Title="Test Case", ID = 54354, Area = "test/path/area", TypeColor = "#FFE9FF5C", Description = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum." };


            //this.DataContext = DisplayTaskSource;
        }
        public TaskControl(DisplayTask task)
        {
            InitializeComponent();

            DisplayTaskSource = task;

            DataContext = DisplayTaskSource;
        }

        private void txtDescription_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var textBox = (TextBlock) sender;

            textBox.Height = Math.Abs(textBox.Height - 50.0) < 1 ? double.NaN : 50;
        }

        private async void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                //double click
                var selectedTask = DataContext as DisplayTask;

                var DetailWindow = new WorkItemDetails(selectedTask);
                await Task.Delay(100);
                DetailWindow.Show();
            }
        }
    }
}
