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

namespace Aml.Editor.PlugIn.AMLLearner
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {

        private SettingsViewModel ViewModel { get; set; } = SettingsViewModel.Instance;

        public Settings()
        {            
            DataContext = ViewModel;
            InitializeComponent();
        }

        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                fbd.SelectedPath = ViewModel.Home;
                System.Windows.Forms.DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    ViewModel.Home = fbd.SelectedPath;
                }
            }
        }

        private void BtnPath_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog())
            {
                fbd.SelectedPath = ViewModel.Path;
                System.Windows.Forms.DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    ViewModel.Path = fbd.SelectedPath;
                }
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.NumResults = int.Parse(textNumResults.Text);
            ViewModel.Port = int.Parse(textPort.Text);
            ViewModel.Backup();
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Reset();
            this.Close();
        }
    }
}
