using System;
using System.Collections.Generic;
using System.Globalization;
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

        private void BtnSave_Click(object sender, RoutedEventArgs e)
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
    }
}
