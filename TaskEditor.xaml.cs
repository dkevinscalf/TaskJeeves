using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using UserControl = System.Windows.Controls.UserControl;

namespace TaskJeeves
{
    /// <summary>
    /// Interaction logic for TaskEditor.xaml
    /// </summary>
    public partial class TaskEditor : UserControl
    {
        public DisplayTask Task;
        public TaskEditor()
        {
            InitializeComponent();
            ddArea.ItemsSource = UICommon.GetProperty("Areas") as List<string>;
            ddUser.ItemsSource = UICommon.GetProperty("Users") as List<string>;
            ddIteration.ItemsSource = UICommon.GetProperty("Iterations") as List<string>;
        }

        public TaskEditor(DisplayTask task)
        {
            InitializeComponent();
            Task = task;
            DataContext = Task;

            ddArea.ItemsSource = UICommon.GetProperty("Areas") as List<string>;
            ddUser.ItemsSource = UICommon.GetProperty("Users") as List<string>;
            ddIteration.ItemsSource = UICommon.GetProperty("Iterations") as List<string>;
            icTaskAttachments.ItemsSource = Task.Attachments;
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Task = DataContext as DisplayTask;
            ddState.ItemsSource = Task.StateList;
            icTaskAttachments.ItemsSource = Task.Attachments;
        }

        private void btnAttachmentBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                txtAttachmentFile.Text = dialog.FileName;
            }
        }

        private void btnAttachmentUpload_Click(object sender, RoutedEventArgs e)
        {
            Task.Attachments.Add(new TaskAttachment(txtAttachmentFile.Text, Task));
            var WIC = UICommon.GetProperty("WIC") as WorkItemController;
            Task.SaveUpdates(WIC);
        }

        private void btnEditorViewFields_Click(object sender, RoutedEventArgs e)
        {
            svFieldEdits.Visibility = svFieldEdits.Visibility == Visibility.Collapsed ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
