using Aml.Editor.PlugIn.AMLLearner.json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    public partial class SettingsWindow : Window
    {

        public SettingsViewModel ViewModel = SettingsViewModel.Instance;

        public SettingsWindow()
        {
            //ViewModel = new SettingsViewModel();
            ViewModel.initFromFile();
            DataContext = ViewModel;
            InitializeComponent();
        }

        //public static SettingsWindow Instance;

        //static SettingsWindow()
        //{
        //    Instance = new SettingsWindow();
        //}

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

        private void setConfig()
        {
            ViewModel.NumResults = int.Parse(textNumResults.Text);
            ViewModel.Port = int.Parse(textPort.Text);

            ViewModel.LearnerConfig.Algorithm.Time = int.Parse(textTimeout.Text);
            ViewModel.LearnerConfig.Algorithm.Size = int.Parse(textNumSolutions.Text);
            ViewModel.LearnerConfig.Algorithm.Tree.Write = cbWriteTree.IsChecked ?? true;

            ViewModel.LearnerConfig.Operator.All = cbUseAll.IsChecked ?? true;
            ViewModel.LearnerConfig.Operator.Cardinality = cbUseCardinality.IsChecked ?? true;
            ViewModel.LearnerConfig.Operator.DataHasValue = cbUseDataHasValue.IsChecked ?? true;
            ViewModel.LearnerConfig.Operator.Negation = cbUseNegation.IsChecked ?? true;
            ViewModel.LearnerConfig.Operator.Numeric = cbUseNumericDatatype.IsChecked ?? true;

            ViewModel.LearnerConfig.Heuristic.ExpansionPenalty = double.Parse(textExpansionPenalty.Text, CultureInfo.InvariantCulture);
            ViewModel.LearnerConfig.Heuristic.RefinementPenalty = double.Parse(textRefinementPenalty.Text, CultureInfo.InvariantCulture);
            ViewModel.LearnerConfig.Heuristic.StartBonus = double.Parse(textStartBonus.Text, CultureInfo.InvariantCulture);
            ViewModel.LearnerConfig.Heuristic.GainBonus = double.Parse(textGainBonus.Text, CultureInfo.InvariantCulture);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            setConfig();
            ViewModel.Backup();
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Reset();
            this.Close();
        }

        private void CbWriteTree_Unchecked(object sender, RoutedEventArgs e)
        {
            ViewModel.LearnerConfig.Algorithm.Tree.Write = false;
        }

        private void CbUseNegation_Unchecked(object sender, RoutedEventArgs e)
        {
            ViewModel.LearnerConfig.Operator.Negation = false;
        }

        private void CbUseAll_Unchecked(object sender, RoutedEventArgs e)
        {
            ViewModel.LearnerConfig.Operator.All = false;
        }

        private void CbUseCardinality_Unchecked(object sender, RoutedEventArgs e)
        {
            ViewModel.LearnerConfig.Operator.Cardinality = false;
        }

        private void CbUseDataHasValue_Unchecked(object sender, RoutedEventArgs e)
        {
            ViewModel.LearnerConfig.Operator.DataHasValue = false;
        }

        private void CbUseNumericDatatype_Unchecked(object sender, RoutedEventArgs e)
        {
            ViewModel.LearnerConfig.Operator.Numeric = false;
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            if(!ViewModel.initFromFile())
                ViewModel.init();
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Filter = "Config|*.json";
            ofd.Title = "load the AMLLearner config file";
            //ofd.InitialDirectory = System.IO.Path.GetFullPath(ViewModel.Home);
            ofd.RestoreDirectory = true;
            
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var fileStream = ofd.OpenFile();
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    String configStr = reader.ReadToEnd();
                    //ViewModel.LearnerConfig = AMLLearnerConfig.FromJsonString(configStr);
                    ViewModel.copy(Newtonsoft.Json.JsonConvert.DeserializeObject<SettingsViewModel>(configStr));
                }
            }
        }

        private void BtnSaveAs_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog();
            sfd.Filter = "Config|*.json";
            sfd.Title = "save the AMLLearner config file";
            //sfd.InitialDirectory = System.IO.Path.GetFullPath(ViewModel.Home);
            sfd.RestoreDirectory = true;
            sfd.ShowDialog();

            if (sfd.FileName != "")
            {
                using (StreamWriter file = File.CreateText(sfd.FileName))
                {
                    setConfig();
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.NullValueHandling = NullValueHandling.Ignore;
                    serializer.Formatting = Formatting.Indented;
                    //serialize object directly into file stream
                    serializer.Serialize(file, ViewModel);
                }
            }
        }

        private void BtnSaveAsDefault_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(ViewModel.DirLocal))
            {
                Directory.CreateDirectory(ViewModel.DirLocal);
            }

            using (StreamWriter file = File.CreateText(ViewModel.FileLocalBackup))
            {
                setConfig();
                JsonSerializer serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
                serializer.Formatting = Formatting.Indented;
                //serialize object directly into file stream
                serializer.Serialize(file, ViewModel);
            }

            MessageBox.Show("successively saved current config as default!");
        }
    }
}
