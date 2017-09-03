using System;
using System.Collections.Generic;
using System.Configuration;
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
using Microsoft.TeamFoundation.Client;

namespace TaskJeeves
{
    /// <summary>
    /// Interaction logic for EditPreferences.xaml
    /// </summary>
    public partial class EditPreferences : Window
    {
        public Preferences Preferences { get; set; }

        private bool userChanged = false;

        public EditPreferences()
        {
            InitializeComponent();

            ddPrefUserNames.ItemsSource = UICommon.GetProperty("Users") as List<string>;

            Preferences = new Preferences();
            DataContext = Preferences;
        }

        private void btnPrefSave_Click(object sender, RoutedEventArgs e)
        {
            Preferences.Save();

            var mainWindow = UICommon.GetProperty("MainWindow") as MainWindow;

            mainWindow.BugTimer.Interval = new TimeSpan(0, Convert.ToInt32(Preferences.BugTimer), 0);

            if (userChanged)
            {
                var WIC = UICommon.GetProperty("WIC") as WorkItemController;

                WIC.UpdateQueue.AddOnUI(new TaskUpdate { IsReadRequest = true, ID = -1 });
            }

            Close();
        }

        private void ddPrefUserNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            userChanged = true;
        }

        private void txtPrefRetention_KeyUp(object sender, KeyEventArgs e)
        {
            userChanged = true;
        }
    }
}
