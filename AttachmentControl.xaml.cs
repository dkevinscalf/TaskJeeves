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
using System.IO;

namespace TaskJeeves
{
    /// <summary>
    /// Interaction logic for AttachmentControl.xaml
    /// </summary>
    public partial class AttachmentControl : UserControl
    {
        public AttachmentControl()
        {
            InitializeComponent();
        }

        public AttachmentControl(TaskAttachment attachment)
        {
            InitializeComponent();

            DataContext = attachment;
        }
        private void OpenAttachment()
        {
            var attachment = DataContext as TaskAttachment;
            string tempFileName = attachment.DownloadFile();
            System.Diagnostics.Process.Start(tempFileName);
        }

        private void btnOpenAttachment_Click(object sender, RoutedEventArgs e)
        {
            OpenAttachment();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                OpenAttachment();
            }
        }

        private void btnDeleteAttachment_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to delete this attachment?", "Attachment Deletion Confirmation", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            var attachment = DataContext as TaskAttachment;
            attachment.Remove(UICommon.GetProperty("WIC") as WorkItemController);
        }
    }
}
