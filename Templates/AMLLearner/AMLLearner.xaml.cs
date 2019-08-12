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
using System.Windows.Forms;
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
    public partial class AMLLearnerGUI : System.Windows.Controls.UserControl, IAMLEditorView, INotifyPropertyChanged, ISupportsSelection
    {
        /// <summary>
        /// <see cref="AboutCommand"/>
        /// </summary>
        private RelayCommand<object> aboutCommand;
        
        public AMLLearnerGUI()
        {

            InitializeComponent();
            DataContext = AMLLearnerViewModel.Instance;
            AMLLearnerViewModel.Instance.Plugin = this;

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

            Home = textHome.Text;
            DirTmp = Home + "/tmp/";
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
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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
            this.HelloText.Text = System.IO.Path.GetFileName(amlFilePath);
            Document = Open(amlFilePath);
        }

        public CAEXDocument Document { get; private set; }

        internal CAEXDocument Open(string filePath)
        {                        
            return CAEXDocument.LoadFromFile(filePath);
        }

        private CAEXObject _selectedObj;

        private bool isPlaceHolder(CAEXBasicObject selectedObject)
        {
            return selectedObject.Equals(AMLLearnerViewModel.Instance.PlaceholderSelectedPos) || selectedObject.Equals(AMLLearnerViewModel.Instance.PlaceholderSelectedNeg);
        }

        public void ChangeSelectedObjectWithPrefix(CAEXBasicObject selectedObject, String prefix)
        {            
            if (selectedObject != null)
            {
                String s = ((selectedObject is CAEXObject caex) ? caex.Name : selectedObject.Node.Name.LocalName);
                if (selectedObject is CAEXObject)
                    this._selectedObj = (CAEXObject)selectedObject;
                this.HelloText.Text = prefix + ": " + s;

                if (selectedObject is InstanceHierarchyType)
                {
                    btnRest.IsEnabled = true;
                    btnPos.IsEnabled = false;
                    btnNeg.IsEnabled = false;
                    btnRm.IsEnabled = false;
                    btnAcm.IsEnabled = false;
                }

                else if (isPlaceHolder(selectedObject))
                {
                    btnRest.IsEnabled = false;
                    btnPos.IsEnabled = false;
                    btnNeg.IsEnabled = false;
                    btnRm.IsEnabled = true;
                    btnAcm.IsEnabled = false;
                }

                else if (AMLLearnerViewModel.Instance.ContainsPositiveExample(this._selectedObj))
                {
                    btnRest.IsEnabled = false;
                    btnPos.IsEnabled = false;
                    btnNeg.IsEnabled = true;
                    btnRm.IsEnabled = true;
                    btnAcm.IsEnabled = false;
                }
                else if (AMLLearnerViewModel.Instance.ContainsNegativeExample(this._selectedObj))
                {
                    btnRest.IsEnabled = false;
                    btnNeg.IsEnabled = false;
                    btnPos.IsEnabled = true;
                    btnRm.IsEnabled = true;
                    btnAcm.IsEnabled = false;
                }
                else if (AMLLearnerViewModel.Instance.ContainsAcm(this._selectedObj))
                {
                    btnRest.IsEnabled = false;
                    btnNeg.IsEnabled = false;
                    btnPos.IsEnabled = false;
                    btnRm.IsEnabled = false;
                    btnAcm.IsEnabled = true;
                }
                else
                {
                    btnRest.IsEnabled = false;
                    btnPos.IsEnabled = true;
                    btnNeg.IsEnabled = true;
                    btnRm.IsEnabled = true;
                    btnAcm.IsEnabled = false;
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
            {
                this.HelloText.Text = "Hello " + System.IO.Path.GetFileName(amlFilePath);
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
            AMLLearnerViewModel.Instance.AddPositive(this._selectedObj);
            Clear();
        }

        private void BtnNeg_Click(object sender, RoutedEventArgs e)
        {
            btnNeg.IsEnabled = false;
            btnPos.IsEnabled = true;

            AMLLearnerViewModel.Instance.AddNegative(this._selectedObj);
            Clear();
        }

        private string _home;

        public string Home
        {
            get { return _home; }
            set
            {
                if (value != _home)
                {
                    _home = value;
                    DirTmp = Home + "/tmp/";
                    Console.WriteLine("setting home to: " + value);
                    OnPropertyChanged("Home");
                }
            }
        }

        private string DirTmp { get; set; }

        private string _acmId;

        public string AcmId
        {
            get { return _acmId; }
            set
            {
                if (value != _acmId)
                {
                    _acmId = value;
                    Console.WriteLine("setting ACM to: " + value);
                    OnPropertyChanged("AcmId");
                }
            }
        }

        private readonly String aml = "data_3.0_SRC.aml";
        private AMLLearnerConfig Config { get; set; }

        private void SetAMLLearnerConfig()
        {
            AMLLearnerExamplesConfig examples = new AMLLearnerExamplesConfig();

            List<String> positives = new List<String>();
            List<String> negatives = new List<String>();
            foreach (CAEXObject obj in AMLLearnerViewModel.Instance.Positives)
            {
                if(obj is InternalElementType)
                    positives.Add("ie_" + obj.Name + "_" + obj.ID);
                else if(obj is ExternalInterfaceType)
                    positives.Add("ei_" + obj.Name + "_" + obj.ID);
            }
            foreach (CAEXObject obj in AMLLearnerViewModel.Instance.Negatives)
            {
                if (obj is InternalElementType)
                    negatives.Add("ie_" + obj.Name + "_" + obj.ID);
                else if (obj is ExternalInterfaceType)
                    negatives.Add("ei_" + obj.Name + "_" + obj.ID);
            }
            examples.Positives = positives.ToArray();
            examples.Negatives = negatives.ToArray();

            String objType = "";
            if (AMLLearnerViewModel.Instance.ObjType.Equals(AMLLearnerViewModel.ObjectType.IE))
                objType = "IE";
            else if (AMLLearnerViewModel.Instance.ObjType.Equals(AMLLearnerViewModel.ObjectType.EI))
                objType = "EI";
            else
                return;

            Config = new AMLLearnerConfig(Home, aml, objType, examples);
            Config.Algorithm.Acm = Acm;
        }

        private void BtnStoreConfig_Click(object sender, RoutedEventArgs e)
        {
            SetAMLLearnerConfig();

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Config|*.json";
            sfd.Title = "save the AMLLearner config file";
            sfd.InitialDirectory = Path.GetFullPath(Home);
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
                    serializer.Serialize(file, Config);
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

                    //String start = AMLLearnerProtocol.MakeStartRequest(Home + "/" + json, 5);
                    String start = AMLLearnerProtocol.MakeStartRequest(Config, 5);
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
            String command = "mvn exec:java -Dexec.mainClass=\"server.AMLLearnerServer\" -f D:\\repositories\\aml\\aml_framework";

            ProcessStartInfo processStartInfo = new ProcessStartInfo("cmd", "/c " + command);
            processStartInfo.FileName = "cmd.exe";            
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
            if (!AMLLearnerViewModel.Instance.Positives.Any()) { 
                System.Windows.MessageBox.Show("Select some positive examples first!");
                return;
            }

            if (!AMLLearnerViewModel.Instance.Negatives.Any())
            {
                System.Windows.MessageBox.Show("Select some negative examples first!");
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
                System.Windows.MessageBox.Show("Cannot find running AMLLearner Server. Starting the server first!");

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

            AMLLearnerViewModel.Instance.RemoveObj(this._selectedObj);
            Clear();
        }

        private void Clear()
        {
            this._selectedObj = null;
            this.HelloText.Text = "";
            btnNeg.IsEnabled = false;
            btnPos.IsEnabled = false;
            btnRm.IsEnabled = false;
            btnAcm.IsEnabled = false;
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
                    if (!AMLLearnerViewModel.Instance.ContainsExample(descendant))
                    {
                        objs.Add(descendant);
                    }
                }

                foreach (ExternalInterfaceType descendant in ih.Descendants<ExternalInterfaceType>())
                {
                    if (!AMLLearnerViewModel.Instance.ContainsExample(descendant))
                    {
                        objs.Add(descendant);
                    }
                }

                AMLLearnerViewModel.Instance.AddNegative(objs);
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

                    AMLLearnerViewModel.Instance.AddDeducedExamples(positiveObjs, AMLLearnerViewModel.ExampleType.POSITIVE, "result_" + (++idx).ToString());
                    AMLLearnerViewModel.Instance.AddDeducedExamples(negativeObjs, AMLLearnerViewModel.ExampleType.NEGATIVE, "result_" + (idx).ToString());
                }
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            AMLLearnerViewModel.Instance.ClearPositives();
            AMLLearnerViewModel.Instance.ClearNegatives();
        }

        private readonly string AcmFile = "learned_acm.aml";

        // for now, we load ACM by writing the ACMs into the original aml file
        private void BtnLoadACM_Click(object sender, RoutedEventArgs e)
        {
            //InstanceHierarchyType acmIh = Document.CAEXFile.InstanceHierarchy.Append("acms");
            AMLLearnerViewModel.Instance.loadACM(DirTmp + AcmFile);
        }

        private void BtnHome_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.SelectedPath = Home;
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    Home = fbd.SelectedPath;
                    textHome.Text = Home;
                }
            }
        }

        private void BtnStartServer_Click(object sender, RoutedEventArgs e)
        {            
            ThreadStart serverStart = new ThreadStart(StartServer);
            Thread server = new Thread(serverStart);
            server.Start();
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
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Config|*.json";
            ofd.Title = "load the AMLLearner config file";
            ofd.InitialDirectory = Path.GetFullPath(Home);
            ofd.RestoreDirectory = true;
            
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                AMLLearnerViewModel.Instance.ClearPositives();
                AMLLearnerViewModel.Instance.ClearNegatives();

                var fileStream = ofd.OpenFile();
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    String configStr = reader.ReadToEnd();
                    Config = Newtonsoft.Json.JsonConvert.DeserializeObject<AMLLearnerConfig>(configStr);

                    foreach (String positive in Config.Examples.Positives)
                    {
                        CAEXObject obj = getObjectById(parseObjectID(positive));
                        AMLLearnerViewModel.Instance.AddPositive(obj);
                    }

                    foreach (String negative in Config.Examples.Negatives)
                    {
                        CAEXObject obj = getObjectById(parseObjectID(negative));
                        AMLLearnerViewModel.Instance.AddNegative(obj);
                    }
                }
            }

        }

        public AMLLearnerACMConfig Acm { get; set; }

        private void BtnAcm_Click(object sender, RoutedEventArgs e)
        {
            textAcm.Text = _selectedObj.ID;
            Acm = new AMLLearnerACMConfig();
            Acm.File = DirTmp + AcmFile;
            Acm.Id = _selectedObj.ID;
            SetAMLLearnerConfig();
        }
    }
}
