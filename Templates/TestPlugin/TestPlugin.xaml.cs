using Aml.Editor.Plugin;
using Aml.Editor.Plugin.Contracts;
using Aml.Editor.PlugIn.TestPlugin.json;
using Aml.Editor.PlugIn.TestPlugin.ViewModel;
using Aml.Engine.CAEX;
using Aml.Engine.CAEX.Extensions;
using Aml.Toolkit.ViewModel;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Aml.Editor.PlugIn.TestPlugin
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    [ExportMetadata("Author", "Yingbing Hua, KIT")]
    [ExportMetadata("DisplayName", "Test")]
    [ExportMetadata("Description", "Testing AML Editor Plugin")]
    [Export(typeof(IAMLEditorView))]
    public partial class Test : UserControl, IAMLEditorView
    {
        /// <summary>
        /// <see cref="AboutCommand"/>
        /// </summary>
        private RelayCommand<object> aboutCommand;

        public Test()
        {

            InitializeComponent();
            DataContext = TestViewModel.Instance;
            TestViewModel.Instance.Plugin = this;

            // Defines the Command list, which will contain user commands, which a user can select
            // via the PlugIn Menu.
            Commands = new List<PluginCommand>();


            // Every PlugIn needs at least an Activation command, which will be called by a user to activate the PlugIn.
            ActivatePlugin = new PluginCommand()
            {
                Command = new RelayCommand<object>(this.StartCommandExecute,
                    this.StartCommandCanExecute),
                CommandName = "Start",
                CommandToolTip = "Start the PlugIn"
            };

            // Every PlugIn should provide a Termination command, which will be called when the PlugIn window is closed by the user. This can only
            // occur, if the PlugIn view is embedded in a docking window by the Editor.
            TerminatePlugin = new PluginCommand()
            {
                Command = new RelayCommand<object>(this.StopCommandExecute, this.StopCommandCanExecute),
                CommandName = "Stop",
                CommandToolTip = "Stop the PlugIn"
            };            


            // Add the StartCommand (should exist in any PlugIn)
            Commands.Add(ActivatePlugin);

            // Add the Stop Command (should exist in any PlugIn)
            Commands.Add(TerminatePlugin);

            // Add the About Command (recommended to exist in any PlugIn)
            Commands.Add(new PluginCommand()
            {
                CommandName = "About",
                Command = AboutCommand,
                CommandToolTip = "Information about this PlugIn"
            });
            

            this.IsActive = false;
        }

        /// <summary>
        /// Occurs when the PlugIn is activated (for example via the <see cref="StartCommand"/> ).
        /// </summary>
        public event EventHandler PluginActivated;

        /// <summary>
        /// Occurs when the PlugIn is deactivated (some UserInteraction inside the PlugIn or via the
        /// <see cref="StopCommand"/> ).
        /// </summary>
        public event EventHandler PluginTerminated;

        /// <summary>
        /// The AboutCommand - Command
        /// </summary>
        /// <value>The about command.</value>
        public System.Windows.Input.ICommand AboutCommand
        {
            get
            {
                return this.aboutCommand
                ??
                (this.aboutCommand = new RelayCommand<object>(this.AboutCommandExecute, this.AboutCommandCanExecute));
            }
        }

        /// <summary>
        /// Gets the Command to activate the PlugIn.
        /// </summary>
        public PluginCommand ActivatePlugin
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether this UserControl could be closed from the Editor's
        /// WindowManager. When a close occurs from the WindowManager, the StopCommand will be
        /// executed via the <see cref="ExecuteCommand"/> Method.
        /// </summary>
        /// <value><c>true</c> if this instance can close; otherwise, <c>false</c>.</value>
        public bool CanClose
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the List of commands, which are viewed in the PlugIn Menu in the Host Application
        /// </summary>
        /// <value>The command List.</value>
        public List<PluginCommand> Commands
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the display name which is shown in the PlugIn Menu in the Host Application
        /// </summary>
        /// <value>The display name.</value>
        public string DisplayName
        {
            get { return "Test"; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is active. The Property should be set to
        /// true in the <see cref="StartCommand"/> and set to false in the <see cref="StopCommand"/>
        /// </summary>
        /// <value><c>true</c> if this instance is active; otherwise, <c>false</c>.</value>
        public bool IsActive
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is reactive. Reactive PlugIn will be
        /// notified, when the actual CAEX-Object changes (Selection of the Tree view Item) <see
        /// cref="ChangeAMLFilePath"/> and <see cref="ChangeSelectedObject"/>.
        /// </summary>
        /// <value><c>true</c> if this instance is reactive; otherwise, <c>false</c>.</value>
        public bool IsReactive
        {
            get { return true; }
        }


        /// <summary>
        /// Gets a value indicating whether this instance is read only. A Read only PlugIn should not
        /// change any CAEX Objects.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadonly
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the terminate PlugIn command.
        /// </summary>
        public PluginCommand TerminatePlugin
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the initial dock position for the PlugIn window.
        /// </summary>
        public DockPositionEnum InitialDockPosition => DockPositionEnum.DockRight;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is automatic active when loaded.
        /// This value can be initially set and will be defined by the user.
        /// </summary>
        public bool IsAutoActive { get; set; }

        /// <summary>
        /// Gets the package name which is used to download the PlugIn package from a NuGet feed. If a Package name
        /// is defined, the AMLEditor can update PlugIn packages independently from its own update cycle.
        /// </summary>
        /// <value>
        /// The package name.
        /// </value>
        public string PackageName => "";

        /// <summary>
        /// Gets the image which should be used in the Header of the PlugIn window.
        /// If no image is defined the editor uses a default image.
        /// </summary>
        public BitmapImage PaneImage => null;

        /// <summary>
        /// This Method is called from the AutomationML Editor to execute a specific command. The
        /// Editor can only execute those commands, which are identified by the <see
        /// cref="PluginCommandsEnum"/> Enumeration. The Editor may execute the termination command
        /// of the PlugIn, so here some preparations for a clean termination should be performed.
        /// </summary>
        /// <param name="command">    The command.</param>
        /// <param name="amlFilePath">The amlFilePath.</param>
        public void ExecuteCommand(PluginCommandsEnum command, string amlFilePath)
        {
            switch (command)
            {
                case PluginCommandsEnum.Terminate:
                    StopCommandExecute(null);
                    break;
            }
        }

        /// <summary>
        /// Test, if the <see cref="AboutCommand"/> can execute.
        /// </summary>
        /// <param name="parameter">unused.</param>
        /// <returns>true, if command can execute</returns>
        private bool AboutCommandCanExecute(object parameter)
        {
            // Execution is always possible, also for inactive PlugIns
            return true;
        }

        /// <summary>
        /// The <see cref="AboutCommand"/> Execution Action.
        /// </summary>
        /// <param name="parameter">unused.</param>
        private void AboutCommandExecute(object parameter)
        {
            var dialog = new About();
            dialog.ShowDialog();
        }

        /// <summary>
        /// Test, if the <see cref="StartCommand"/> can execute. The <see cref="IsActive"/> Property
        /// should be false prior to Activation.
        /// </summary>
        /// <param name="parameter">unused</param>
        /// <returns>true, if command can execute</returns>
        private bool StartCommandCanExecute(object parameter)
        {
            return !this.IsActive;
        }

        /// <summary>
        /// The <see cref="StartCommand"/> s execution Action. The <see cref="PluginActivated"/>
        /// event is raised and the <see cref="IsActive"/> Property is set to true.
        /// </summary>
        /// <param name="parameter">unused</param>
        private void StartCommandExecute(object parameter)
        {
            this.IsActive = true;
            PluginActivated?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Test, if the <see cref="StopCommand"/> can execute.
        /// </summary>
        /// <param name="parameter">unused</param>
        /// <returns>true, if command can execute</returns>
        private bool StopCommandCanExecute(object parameter)
        {
            return this.IsActive;
        }

        /// <summary>
        /// The <see cref="StopCommand"/> Execution Action sets the <see cref="IsActive"/> Property
        /// to false. The <see cref="PluginTerminated"/> event will be raised.
        /// </summary>
        /// <param name="parameter">unused</param>
        private void StopCommandExecute(object parameter)
        {
            this.IsActive = false;
            PluginTerminated?.Invoke(this, EventArgs.Empty);
        }        

        public void ChangeAMLFilePath(string amlFilePath)
        {
            this.HelloText.Text = System.IO.Path.GetFileName(amlFilePath);
        }

        private CAEXObject _selectedObj;

        public void ChangeSelectedObjectWithPrefix(CAEXBasicObject selectedObject, String prefix)
        {
            if (selectedObject != null)
            {
                String s = ((selectedObject is CAEXObject caex) ? caex.Name : selectedObject.Node.Name.LocalName);
                if (selectedObject is CAEXObject)
                    this._selectedObj = (CAEXObject)selectedObject;
                this.HelloText.Text = prefix + ": " + s;

                if (this._selectedObj.Name.Equals("PlaceHolder"))
                {
                    btnPos.IsEnabled = false;
                    btnNeg.IsEnabled = false;
                    btnRm.IsEnabled = false;
                }

                else if (TestViewModel.Instance.containsPositiveExample(this._selectedObj))
                {
                    btnPos.IsEnabled = false;
                    btnNeg.IsEnabled = true;
                    btnRm.IsEnabled = true;
                }
                else if (TestViewModel.Instance.containsNegativeExample(this._selectedObj))
                {
                    btnNeg.IsEnabled = false;
                    btnPos.IsEnabled = true;
                    btnRm.IsEnabled = true;
                }
                else
                {
                    btnPos.IsEnabled = true;
                    btnNeg.IsEnabled = true;
                    btnRm.IsEnabled = true;
                }
                
            }
        }

        public void ChangeSelectedObject(CAEXBasicObject selectedObject)
        {
            ChangeSelectedObjectWithPrefix(selectedObject, "editor");
        }       

        public void PublishAutomationMLFileAndObject(string amlFilePath, CAEXBasicObject selectedObject)
        {
            if (!string.IsNullOrEmpty(amlFilePath))
                this.HelloText.Text = "Hello " + System.IO.Path.GetFileName(amlFilePath);
            else
                this.HelloText.Text = "Nobody to say hello to!";

            if (selectedObject != null)
            {
                ChangeSelectedObject(selectedObject);
            }
        }

        private void BtnPos_Click(object sender, RoutedEventArgs e)
        {
            btnNeg.IsEnabled = true;
            btnPos.IsEnabled = false;
            // add selected to positive 
            TestViewModel.Instance.addPositive(this._selectedObj);
            Clear();
        }

        private void BtnNeg_Click(object sender, RoutedEventArgs e)
        {
            btnNeg.IsEnabled = false;
            btnPos.IsEnabled = true;

            TestViewModel.Instance.addNegative(this._selectedObj);
            Clear();
        }

        private void BtnConfig_Click(object sender, RoutedEventArgs e)
        {
            String aml = "D:/repositories/aml/aml_framework/src/main/resources/test/data_src_3.0.aml";
            AMLLearnerExamplesConfig examples = new AMLLearnerExamplesConfig();

            List<String> positives = new List<String>();
            List<String> negatives = new List<String>();
            foreach (CAEXObject obj in TestViewModel.Instance.Positives)
            {
                positives.Add("ie_" + obj.Name + "_" + obj.ID);
            }
            foreach (CAEXObject obj in TestViewModel.Instance.Negatives)
            {
                negatives.Add("ie_" + obj.Name + "_" + obj.ID);
            }
            examples.Positives = positives.ToArray();
            examples.Negatives = negatives.ToArray();

            AMLLearnerConfig config = new AMLLearnerConfig(aml, TestViewModel.Instance.ObjType, examples);
            using (StreamWriter file = File.CreateText(@"D:/repositories/aml/aml_framework/src/main/resources/test/aml.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.NullValueHandling = NullValueHandling.Ignore;
                serializer.Formatting = Formatting.Indented;
                //serialize object directly into file stream
                serializer.Serialize(file, config);
            }
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BtnRm_Click(object sender, RoutedEventArgs e)
        {
            TestViewModel.Instance.removeObj(this._selectedObj);
            Clear();
        }

        private void Clear()
        {
            this._selectedObj = null;
            this.HelloText.Text = "";
            btnNeg.IsEnabled = false;
            btnPos.IsEnabled = false;
            btnRm.IsEnabled = false;
        }
    }
}
