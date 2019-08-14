using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aml.Editor.PlugIn.AMLLearner
{
    public class SettingsViewModel : ViewModelBase
    {

        public string DirTmp { get; set; }
        public string CommandStartServer { get; set; }

        private string PreviousHome { get; set; }
        private string PreviousPath { get; set; }

        public static SettingsViewModel Instance { get; private set; }

        static SettingsViewModel()
        {
            Instance = new SettingsViewModel();
        }

        private SettingsViewModel ()
        {
            Home = "D:/repositories/aml/aml_framework/src/test/resources/demo";
            Path = "D:/repositories/aml/aml_framework/target/bin";
            Port = 4343;
            NumResults = 5;

            PreviousHome = Home;
            PreviousPath = Path;
        }

        public void Backup()
        {
            PreviousHome = Home;
            PreviousPath = Path;
        }

        public void Reset()
        {
            Home = PreviousHome;
            Path = PreviousPath;
        }

        private string _home;
        public string Home
        {
            get { return _home; }
            set
            {                
                _home = value;
                DirTmp = Home + "/tmp/";
                Console.WriteLine("setting home to: " + value);
                RaisePropertyChanged("Home");
            }
        }        

        private string _path;
        public string Path
        {
            get { return _path; }
            set
            {                                
                string serverFile = value + "/server.bat";
                if (!System.IO.File.Exists(serverFile))
                {
                    System.Windows.MessageBox.Show("Can not find the server.bat program in the path!");
                }

                else
                {
                    _path = value;
                    Console.WriteLine("setting aml learner path to: " + value);
                    CommandStartServer = serverFile;
                    RaisePropertyChanged("Path");
                }                
            }
        }

        private int _port;
        public int Port
        {
            get { return _port; }
            set
            {
                _port = value;
                Console.WriteLine("setting port to: " + value);
            }
        }

        private int _numResults;
        public int NumResults
        {
            get { return _numResults; }
            set
            {
                _numResults = value;
                Console.WriteLine("setting num results to: " + value);
            }
        }
    }
}
