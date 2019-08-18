using Aml.Editor.Plugin;
using Aml.Editor.Plugin.Contracts;
using Aml.Editor.PlugIn.AMLLearner.json;
using Aml.Editor.PlugIn.AMLLearner.ViewModel;
using Aml.Engine.CAEX;
using Aml.Engine.CAEX.Extensions;
using Aml.Toolkit.ViewModel;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Aml.Editor.PlugIn.AMLLearner
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    [ExportMetadata("Author", "Yingbing Hua, KIT")]
    [ExportMetadata("DisplayName", "AMLLearner")]
    [ExportMetadata("Description", "AMLLearner Plugin")]
    [Export(typeof(IAMLEditorView))]
    public partial class AMLLearnerGUI : UserControl, IAMLEditorView, ISupportsSelection //, INotifyPropertyChanged
    {
        /// <summary>
        /// <see cref="AboutCommand"/>
        /// </summary>
        private RelayCommand<object> aboutCommand;

        private AMLLearnerViewModel ViewModel { get; set; } = AMLLearnerViewModel.Instance;

        public AMLLearnerGUI()
        {

            InitializeComponent();                        

            DataContext = ViewModel;
            ViewModel.Plugin = this;

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

            //InputQueue = new ConcurrentQueue<string>();
            OutputQueue = new ConcurrentQueue<string>();
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
        public event EventHandler<SelectionEventArgs> Selected;
        //public event PropertyChangedEventHandler PropertyChanged;

        //protected void OnPropertyChanged(string propertyName)
        //{
        //    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}

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
            get { return "AMLLearner"; }
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
            // Editor could pass an invalid parameter, so a test is necessary
            if (amlFilePath != null && amlFilePath.Length>0)
            {
                string filename = System.IO.Path.GetFileName(amlFilePath);
                if (filename != null && filename != String.Empty)
                {
                    this.HelloText.Text = filename;
                    ViewModel.AmlFile = filename;
                    Document = CAEXDocument.LoadFromFile(System.IO.Path.GetFullPath(amlFilePath));
                }                                
            }            
        }

        public CAEXDocument Document { get; private set; }

        internal CAEXDocument Open(string filePath)
        {                        
            return CAEXDocument.LoadFromFile(filePath);
        }

        private CAEXObject _selectedObj;

        private bool isPlaceHolder(CAEXBasicObject selectedObject)
        {            
            return ViewModel.isPlaceHolder((CAEXObject)selectedObject);
        }

        public void ChangeSelectedObjectWithPrefix(CAEXBasicObject selectedObject, String prefix)
        {            
            if (selectedObject != null)
            {
                String s = ((selectedObject is CAEXObject caex) ? caex.Name : selectedObject.Node.Name.LocalName);
                if (selectedObject is CAEXObject)
                    this._selectedObj = (CAEXObject)selectedObject;
                this.HelloText.Text = prefix + s;

                if (selectedObject is InstanceHierarchyType)
                {
                    btnRest.IsEnabled = true;
                    btnPos.IsEnabled = false;
                    btnNeg.IsEnabled = false;
                    btnRm.IsEnabled = false;
                    btnSetAcm.IsEnabled = false;

                    cbPrimary.IsEnabled = false;
                    cbId.IsEnabled = false;
                    cbDesendant.IsEnabled = false;
                    cbName.IsEnabled = false;
                    cbNegated.IsEnabled = false;
                    slMin.IsEnabled = false;
                    slMax.IsEnabled = false;
                    textMin.IsEnabled = false;
                    textMax.IsEnabled = false;
                }

                else if (isPlaceHolder(selectedObject))
                {
                    btnRest.IsEnabled = false;
                    btnPos.IsEnabled = false;
                    btnNeg.IsEnabled = false;
                    btnRm.IsEnabled = true;
                    btnSetAcm.IsEnabled = false;

                    cbPrimary.IsEnabled = false;
                    cbId.IsEnabled = false;
                    cbDesendant.IsEnabled = false;
                    cbName.IsEnabled = false;
                    cbNegated.IsEnabled = false;
                    slMin.IsEnabled = false;
                    slMax.IsEnabled = false;
                    textMin.IsEnabled = false;
                    textMax.IsEnabled = false;
                }

                else if (ViewModel.ContainsPositiveExample(this._selectedObj))
                {
                    btnRest.IsEnabled = false;
                    btnPos.IsEnabled = false;
                    btnNeg.IsEnabled = true;
                    btnRm.IsEnabled = true;
                    btnSetAcm.IsEnabled = false;

                    cbPrimary.IsEnabled = false;
                    cbId.IsEnabled = false;
                    cbDesendant.IsEnabled = false;
                    cbName.IsEnabled = false;
                    cbNegated.IsEnabled = false;
                    slMin.IsEnabled = false;
                    slMax.IsEnabled = false;
                    textMin.IsEnabled = false;
                    textMax.IsEnabled = false;
                }
                else if (ViewModel.ContainsNegativeExample(this._selectedObj))
                {
                    btnRest.IsEnabled = false;
                    btnNeg.IsEnabled = false;
                    btnPos.IsEnabled = true;
                    btnRm.IsEnabled = true;
                    btnSetAcm.IsEnabled = false;

                    cbPrimary.IsEnabled = false;
                    cbId.IsEnabled = false;
                    cbDesendant.IsEnabled = false;
                    cbName.IsEnabled = false;
                    cbNegated.IsEnabled = false;
                    slMin.IsEnabled = false;
                    slMax.IsEnabled = false;
                    textMin.IsEnabled = false;
                    textMax.IsEnabled = false;
                }
                else if (ViewModel.IsAcm(this._selectedObj))
                {
                    btnRest.IsEnabled = false;
                    btnNeg.IsEnabled = false;
                    btnPos.IsEnabled = false;
                    btnRm.IsEnabled = false;

                    btnSetAcm.IsEnabled = true;
                    btnRmAcm.IsEnabled = true;

                    cbPrimary.IsEnabled = true;
                    cbId.IsEnabled = true;
                    cbDesendant.IsEnabled = true;
                    cbName.IsEnabled = true;
                    cbNegated.IsEnabled = true;
                    slMin.IsEnabled = true;
                    slMax.IsEnabled = true;
                    textMin.IsEnabled = true;
                    textMax.IsEnabled = true;

                    ViewModel.AcmId = ((CAEXObject)_selectedObj).ID;

                    AttributeType config = CaexToAcm.GetConfigAttribute(this._selectedObj);

                    AttributeType primary = ViewModel.GetConfigParameter(config, "distinguished");
                    ViewModel.ConfigPrimary = bool.Parse(primary.Value);
                    //cbPrimary.IsChecked = bool.Parse(ConfigPrimary);

                    AttributeType id = ViewModel.GetConfigParameter(config, "identifiedById");
                    ViewModel.ConfigId = bool.Parse(id.Value);
                    //cbId.IsChecked = bool.Parse(ConfigId);

                    AttributeType name = ViewModel.GetConfigParameter(config, "identifiedByName");
                    ViewModel.ConfigName = bool.Parse(name.Value);
                    //cbName.IsChecked = bool.Parse(ConfigName);

                    AttributeType negated = ViewModel.GetConfigParameter(config, "negated");
                    ViewModel.ConfigNegated = bool.Parse(negated.Value);
                    //cbNegated.IsChecked = bool.Parse(ConfigNegated);

                    AttributeType descendant = ViewModel.GetConfigParameter(config, "descendant");
                    ViewModel.ConfigDescendant = bool.Parse(descendant.Value);
                    //cbDesendant.IsChecked = bool.Parse(ConfigDescendant);

                    AttributeType min = ViewModel.GetConfigParameter(config, "minCardinality");
                    ViewModel.ConfigMinCardinality = int.Parse(min.Value);
                    //slMin.Value = ConfigMinCardinality;
                    //textMin.Text = ConfigMinCardinality.ToString();

                    AttributeType max = ViewModel.GetConfigParameter(config, "maxCardinality");
                    ViewModel.ConfigMaxCardinality = int.Parse(max.Value);
                    //slMax.Value = ConfigMaxCardinality;
                    //textMax.Text = ConfigMaxCardinality.ToString();
                }
                else
                {
                    btnRest.IsEnabled = false;
                    btnPos.IsEnabled = true;
                    btnNeg.IsEnabled = true;
                    btnRm.IsEnabled = true;
                    btnSetAcm.IsEnabled = false;

                    cbPrimary.IsEnabled = false;
                    cbId.IsEnabled = false;
                    cbDesendant.IsEnabled = false;
                    cbName.IsEnabled = false;
                    cbNegated.IsEnabled = false;
                    slMin.IsEnabled = false;
                    slMax.IsEnabled = false;
                    textMin.IsEnabled = false;
                    textMax.IsEnabled = false;
                }

            }
        }       

        public void ChangeSelectedObject(CAEXBasicObject selectedObject)
        {
            ChangeSelectedObjectWithPrefix(selectedObject, "editor::");
        }

        public void PublishAutomationMLFileAndObject(string amlFilePath, CAEXBasicObject selectedObject)
        {
            if (!string.IsNullOrEmpty(amlFilePath))
            {
                this.HelloText.Text = System.IO.Path.GetFileName(amlFilePath);
                ViewModel.AmlFile = System.IO.Path.GetFileName(amlFilePath);
                Document = Open(amlFilePath);
            }
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
            ViewModel.AddPositive(this._selectedObj);
            ResetUI();
        }

        private void BtnNeg_Click(object sender, RoutedEventArgs e)
        {
            btnNeg.IsEnabled = false;
            btnPos.IsEnabled = true;

            ViewModel.AddNegative(this._selectedObj);
            ResetUI();
        }                                     



        private void BtnStoreConfig_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.UpdateAMLLearnerConfig();

            System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog();
            sfd.Filter = "Config|*.json";
            sfd.Title = "save the AMLLearner config file";
            sfd.InitialDirectory = Path.GetFullPath(ViewModel.Settings.Home);
            sfd.RestoreDirectory = true;
            sfd.ShowDialog();

            if (sfd.FileName != "")
            {
                using (StreamWriter file = File.CreateText(sfd.FileName))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.NullValueHandling = NullValueHandling.Ignore;
                    serializer.Formatting = Formatting.Indented;
                    //serialize object directly into file stream
                    serializer.Serialize(file, ViewModel.Settings.LearnerConfig);
                }
            }
        }

        private ConcurrentQueue<String> OutputQueue { get; set; }

        private readonly String MESSAGE_END = "transmission finished";
        private readonly String ALG_RUNNING = "Current config";
        private readonly int MESSAGE_LEN = 4;
        private Socket ClientSocket { get; set; }
        private Boolean IsRunning { get; set; }

        private AMLLearnerProtocolResults Results { get; set; }

        private void Listen()
        {
            Boolean receiving = true;
            while (receiving) {
                try
                {
                    byte[] rcvLenBytes = new byte[MESSAGE_LEN];
                    ClientSocket.Receive(rcvLenBytes);
                    int rcvLen = System.BitConverter.ToInt32(rcvLenBytes, 0);
                    byte[] rcvBytes = new byte[rcvLen];
                    ClientSocket.Receive(rcvBytes);
                    String rcv = System.Text.Encoding.UTF8.GetString(rcvBytes);

                    if (rcv.StartsWith("{") && rcv.Contains("results"))
                    {
                        Results = AMLLearnerProtocol.GetResults(rcv);
                        continue;
                    }

                    else if (rcv.Contains(ALG_RUNNING))
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            btnStop.IsEnabled = true;
                        });
                    }

                    else if (rcv.Contains(MESSAGE_END))
                    {
                        receiving = false;
                        continue;
                    }

                    //InputQueue.Enqueue(rcv);
                    Console.WriteLine(rcv);

                    this.Dispatcher.Invoke(() =>
                    {
                        Logger.Focus();
                        Logger.AppendText(Environment.NewLine + rcv);
                        Logger.CaretIndex = Logger.Text.Length;
                        Logger.ScrollToEnd();
                        //var rect = Logger.GetRectFromCharacterIndex(Logger.CaretIndex);
                        //Logger.ScrollToHorizontalOffset(rect.Right);
                    });                    
                }
                catch (ThreadAbortException e)
                {
                    Console.WriteLine("Thread Abort Exception");
                }
            }
        }

        private void Write()
        {
            Boolean sending = true;
            while (sending)
            {
                try
                {
                    String toSend;
                    OutputQueue.TryDequeue(out toSend);

                    if (toSend is null) continue;
                    
                    int toSendLen = System.Text.Encoding.UTF8.GetByteCount(toSend);
                    byte[] toSendBytes = System.Text.Encoding.UTF8.GetBytes(toSend);
                    byte[] toSendLenBytes = System.BitConverter.GetBytes(toSendLen);

                    ClientSocket.Send(toSendLenBytes);
                    if (toSendLen != ClientSocket.Send(toSendBytes))
                    {
                        Console.WriteLine("writing to server failed");
                    }

                    if (toSend.Contains(MESSAGE_END))
                    {
                        sending = false;                        
                    }
                }
                catch (ThreadAbortException e)
                {
                    Console.WriteLine("Thread Abort Exception");
                }
            }
        }

        private Boolean Write(String toSend) {

            Console.WriteLine("writing to server: " + toSend);

            int toSendLen = System.Text.Encoding.UTF8.GetByteCount(toSend);            
            byte[] toSendLenBytes = System.BitConverter.GetBytes(toSendLen);
            byte[] toSendBytes = System.Text.Encoding.UTF8.GetBytes(toSend);

            ClientSocket.Send(toSendLenBytes);
           
            if (toSendLen != ClientSocket.Send(toSendBytes))
            {
                return false;
            }
            return true;
        }        

        private void Learn()
        {
            if (SocketConnected())
            {                
                try
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        btnRun.IsEnabled = false;
                        btnLoadResults.IsEnabled = false;
                        btnLoadACM.IsEnabled = false;
                    });

                    ViewModel.UpdateAMLLearnerConfig();
                    //String start = AMLLearnerProtocol.MakeStartRequest(Home + "/" + json, 5);
                    String start = AMLLearnerProtocol.MakeStartRequest(ViewModel.Settings.LearnerConfig, 5);
                    OutputQueue.Enqueue(start);
                    //if (!write(start)) {
                    //    Console.WriteLine("failed to send start signal!");
                    //    return;
                    //}
                    Console.WriteLine("waiting for the server");

                    Listener.Join();

                    OutputQueue.Enqueue(MESSAGE_END);
                    //if (!write(MESSAGE_END))
                    //{
                    //    Console.WriteLine("failed to send end signal!");
                    //    return;
                    //}                        
                    Console.WriteLine("Learning finished successively.");                    

                    while (SocketConnected()) { }

                    this.Dispatcher.Invoke(() =>
                    {
                        btnRun.IsEnabled = true;
                        btnStop.IsEnabled = false;
                        btnLoadResults.IsEnabled = true;
                        btnLoadACM.IsEnabled = true;
                    });
                }
                catch (ThreadAbortException e)
                {
                    Console.WriteLine("Thread Abort Exception");
                }
            }
        }

        private Thread Listener { get; set; }
        private Thread Writer { get; set; }
        private Thread Learner { get; set; }

        private bool PingHost(string hostUri, int portNumber)
        {
            try
            {
                using (var client = new TcpClient(hostUri, portNumber))
                    return true;
            }
            catch (SocketException ex)
            {
                return false;
            }

            //var client = new TcpClient();
            //if (!client.ConnectAsync(hostUri, portNumber).Wait(1000))
            //{
            //    // connection failure
            //    return false;
            //}

            //return true;

            //var client = new TcpClient();
            //var result = client.BeginConnect(hostUri, portNumber, null, null);
            //var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(100));

            //if (!success)
            //{
            //    return false;
            //}

            //// we have connected            
            //client.EndConnect(result);
            //return true;
        }

        private void StartServer()
        {
            //String command = "mvn exec:java -Dexec.mainClass=\"server.AMLLearnerServer\" -f D:\\repositories\\aml\\aml_framework";

            String info = "echo starting AMLLearner server, please wait...";
            ProcessStartInfo processStartInfo = new ProcessStartInfo("cmd", "/c " + ViewModel.Settings.CommandStartServer);
            processStartInfo.FileName = "cmd.exe";
            processStartInfo.Verb = "runas";
            //processStartInfo.RedirectStandardInput = true;
            //processStartInfo.RedirectStandardOutput = true;
            //processStartInfo.CreateNoWindow = true;
            //processStartInfo.UseShellExecute = false;

            Process process = new Process();
            process.StartInfo = processStartInfo;
            process.Start();

            //System.Windows.MessageBox.Show("server running");

            this.Dispatcher.Invoke(() =>
            {
                btnRun.IsEnabled = true;
            });

            //cmd.StandardInput.WriteLine(command);
            //cmd.StandardInput.Flush();
            //cmd.StandardInput.Close();
            //process.WaitForExit();
            //Console.WriteLine(cmd.StandardOutput.ReadToEnd());
        }

        private void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.GetAllSelectedPositives().Any()) { 
                MessageBox.Show("Select some positive examples first!");
                return;
            }

            if (!ViewModel.GetAllSelectedNegatives().Any())
            {
                MessageBox.Show("Select some negative examples first!");
                return;
            }

            var host = Dns.GetHostEntry(Dns.GetHostName());
            String address = "";
            int port = 4343;
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    address = ip.ToString();
                }
            }

            IPEndPoint serverAddress = new IPEndPoint(IPAddress.Parse(address), port);

            if (PingHost(address, port)) {                

                ClientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                ClientSocket.Connect(serverAddress);

                ThreadStart listenerStart = new ThreadStart(Listen);
                Listener = new Thread(listenerStart);
                Listener.Start();

                ThreadStart writerStart = new ThreadStart(Write);
                Writer = new Thread(writerStart);
                Writer.Start();

                ThreadStart learnerStart = new ThreadStart(Learn);
                Learner = new Thread(learnerStart);
                Learner.Start();                
            }          
            else
                MessageBox.Show("Cannot find running AMLLearner Server. Starting the server first!");

        }

        private bool SocketConnected()
        {
            bool part1 = ClientSocket.Poll(1000, SelectMode.SelectRead);
            bool part2 = (ClientSocket.Available == 0);
            if (part1 && part2)
                return false;
            else
                return true;
        }

        private void BtnRm_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.RemoveObj(this._selectedObj);
            ResetUI();
        }

        private void ResetUI()
        {
            this._selectedObj = null;
            this.HelloText.Text = "";
            btnNeg.IsEnabled = false;
            btnPos.IsEnabled = false;
            btnRm.IsEnabled = false;
            btnSetAcm.IsEnabled = false;
        }       

        private void BtnRest_Click(object sender, RoutedEventArgs e)
        {
            if (this._selectedObj is InstanceHierarchyType) {
                InstanceHierarchyType ih = (InstanceHierarchyType)this._selectedObj;                                

                List<CAEXObject> objs = new List<CAEXObject>();
                // for each caex obj (ele) under this IH: 
                // - if (ele) is already in the positive or negative list, ignore
                // - if (ele) is unknown: add to negative list
                foreach (InternalElementType descendant in ih.Descendants<InternalElementType>())
                {
                    if (!ViewModel.ContainsExample(descendant))
                    {
                        objs.Add(descendant);
                    }
                }

                foreach (ExternalInterfaceType descendant in ih.Descendants<ExternalInterfaceType>())
                {
                    if (!ViewModel.ContainsExample(descendant))
                    {
                        objs.Add(descendant);
                    }
                }

                ViewModel.AddNegative(objs);
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            String stop = AMLLearnerProtocol.MakeStopRequest();
            OutputQueue.Enqueue(stop);
        }

        private void BtnLoadResults_Click(object sender, RoutedEventArgs e)
        {
            if (Results is null)
                return;
            else
            {
                int idx = 0;
                foreach (AMLLearnerProtocolResult result in Results.Data)
                {
                    String concept = result.Concept;
                    String[] positives = result.Positives;
                    String[] negatives = result.Negatives;

                    List<CAEXObject> positiveObjs = new List<CAEXObject>();
                    foreach (String positive in positives)
                    {
                        String id = positive.Substring(positive.LastIndexOf("_")+1);
                        positiveObjs.Add(Document.FindByID(id));
                    }

                    List<CAEXObject> negativeObjs = new List<CAEXObject>();
                    foreach (String negative in negatives)
                    {
                        String id = negative.Substring(negative.LastIndexOf("_")+1);
                        negativeObjs.Add(Document.FindByID(id));
                    }

                    //ViewModel.AddDeducedExamples(positiveObjs, ExampleType.POSITIVE, "result_" + (++idx).ToString());
                    //ViewModel.AddDeducedExamples(negativeObjs, ExampleType.NEGATIVE, "result_" + (idx).ToString());

                    ViewModel.AddDeducedExamples(positiveObjs, ExampleType.POSITIVE, "result_" + (++idx).ToString());
                    ViewModel.AddDeducedExamples(negativeObjs, ExampleType.NEGATIVE, "result_" + (idx).ToString());
                }
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ClearPositives();
            ViewModel.ClearNegatives();
        }        

        // for now, we load ACM by writing the ACMs into the original aml file
        private void BtnLoadACM_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.loadACM();
            btnSaveAcm.IsEnabled = true;
            //btnClearAcm.IsEnabled = true;
        }

        private void BtnStartServer_Click(object sender, RoutedEventArgs e)
        {
            if (!System.IO.File.Exists(ViewModel.Settings.CommandStartServer))
            {
                MessageBox.Show("Cannot find AMLLearner server.bat in the path: " + System.IO.Path.GetFullPath(ViewModel.Settings.CommandStartServer) + "!");
            }
            else
            {
                ThreadStart serverStart = new ThreadStart(StartServer);
                Thread server = new Thread(serverStart);
                server.Start();
            }            
        }

        private String parseObjectID(String objComplexID)
        {
            return objComplexID.Substring(objComplexID.LastIndexOf("_") + 1);
        }

        private CAEXObject getObjectById(String id)
        {
            return Document.FindByID(id);
        }

        private void BtnLoadConfig_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Filter = "Config|*.json";
            ofd.Title = "load the AMLLearner config file";
            ofd.InitialDirectory = Path.GetFullPath(ViewModel.Settings.Home);
            ofd.RestoreDirectory = true;
            
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ViewModel.ClearPositives();
                ViewModel.ClearNegatives();

                var fileStream = ofd.OpenFile();
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    String configStr = reader.ReadToEnd();
                    ViewModel.Settings.LearnerConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<AMLLearnerConfig>(configStr);

                    foreach (String positive in ViewModel.Settings.LearnerConfig.Examples.Positives)
                    {
                        CAEXObject obj = getObjectById(parseObjectID(positive));
                        ViewModel.AddPositive(obj);
                    }

                    foreach (String negative in ViewModel.Settings.LearnerConfig.Examples.Negatives)
                    {
                        CAEXObject obj = getObjectById(parseObjectID(negative));
                        ViewModel.AddNegative(obj);
                    }
                }
            }
        }        

        private void BtnSetAcm_Click(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.GetAllSelectedPositives().Any() || !ViewModel.GetAllSelectedNegatives().Any())
            {
                MessageBox.Show("Select a configuration first: either load a config or select some examples!");
                return;
            }
            ViewModel.UpdateAcm();            
        }        


        private void BtnClearAcm_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ClearAcm();
            btnSaveAcm.IsEnabled = false;
            //btnClearAcm.IsEnabled = false;
        }

        private void SlMax_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ViewModel.ConfigMaxCardinality = (int)slMax.Value;
        }

        private void SlMin_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ViewModel.ConfigMinCardinality = (int)slMin.Value;
        }

        private void CbPrimary_Unchecked(object sender, RoutedEventArgs e)
        {
            ViewModel.ConfigPrimary = false;
        }

        private void CbId_Unchecked(object sender, RoutedEventArgs e)
        {
            ViewModel.ConfigId = false;
        }

        private void CbName_Unchecked(object sender, RoutedEventArgs e)
        {
            ViewModel.ConfigName = false;
        }

        private void CbNegated_Unchecked(object sender, RoutedEventArgs e)
        {
            ViewModel.ConfigNegated = false;
        }

        private void CbDesendant_Unchecked(object sender, RoutedEventArgs e)
        {
            ViewModel.ConfigDescendant = false;
        }

        //private void MenuSetting_Click(object sender, RoutedEventArgs e)
        //{
        //    Window settings = new Settings();
        //    settings.Show();
        //}

        private void BtnSaveAcm_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog();
            sfd.Filter = "ACM file|*.aml";
            sfd.Title = "save the ACM file";
            sfd.InitialDirectory = Path.GetFullPath(ViewModel.Settings.Home);
            sfd.RestoreDirectory = true;
            sfd.ShowDialog();

            if (sfd.FileName != "")
            {
                ViewModel.SaveAcm(sfd.FileName);                
            }
        }

        private void BtnLoadACMFile_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Filter = "ACM file|*.aml";
            ofd.Title = "load ACM file";
            ofd.InitialDirectory = Path.GetFullPath(ViewModel.Settings.Home);
            ofd.RestoreDirectory = true;

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ViewModel.loadACM(ofd.FileName);
                btnSaveAcm.IsEnabled = true;
                btnSetAcm.IsEnabled = true;
            }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            //Window settings = new Settings();
            //Window settings = SettingsWindow.Instance;
            SettingsWindow window = new SettingsWindow();
            window.Show();
        }

        private void BtnAddAcm_Click(object sender, RoutedEventArgs e)
        {
            if (!ViewModel.IsAcm(_selectedObj))
            {
                ViewModel.AddAcm(_selectedObj);
                btnSaveAcm.IsEnabled = true;
            }
        }

        private void BtnRmAcm_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.IsAcm(_selectedObj))
            {
                ViewModel.RemoveAcm(_selectedObj);
            }
        }
    }
}
