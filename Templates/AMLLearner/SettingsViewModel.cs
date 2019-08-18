﻿using Aml.Editor.PlugIn.AMLLearner.json;
using GalaSoft.MvvmLight;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Aml.Editor.PlugIn.AMLLearner
{
    public class ShouldSerializeContractResolver : DefaultContractResolver
    {
        public new static readonly ShouldSerializeContractResolver Instance = new ShouldSerializeContractResolver();

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);

            if (property.DeclaringType == typeof(ViewModelBase) && property.PropertyName == "IsInDesignMode")
            {
                property.ShouldSerialize =
                    instance =>
                    {
                        return false;
                    };
            }

            return property;
        }
    }

    public class SettingsViewModel : ViewModelBase
    {

        [JsonIgnore]
        public string DirTmp { get; set; }

        [JsonIgnore]
        public string CommandStartServer { get; set; }

        [JsonIgnore]
        private string PreviousHome { get; set; }

        [JsonIgnore]
        private string PreviousPath { get; set; }

        public static SettingsViewModel Instance { get; private set; }

        private AMLLearnerConfig _learnerConfig;
        [JsonProperty("LearnerConfig")]
        public AMLLearnerConfig LearnerConfig
        {
            get { return _learnerConfig; }
            set
            {
                _learnerConfig = value;                
                RaisePropertyChanged("LearnerConfig");
            }
        }

        [JsonIgnore]
        public AMLLearnerConfig PrevisouLearnerConfig { get; set; }

        [JsonIgnore]
        public readonly string DirLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/AMLLearner";

        [JsonIgnore]
        public readonly string FileLocalBackup = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/AMLLearner/settings.json";

        [JsonIgnore]
        public bool IsInitializedFromFile { get; set; } = false;

        static SettingsViewModel()
        {
            Instance = new SettingsViewModel();
        }

        private SettingsViewModel ()
        {
            init();
        }        

        public void copy(SettingsViewModel other)
        {
            Home = other.Home;
            Path = other.Path;
            Port = other.Port;
            NumResults = other.NumResults;
            LearnerConfig = other.LearnerConfig;
        }

        public bool initFromFile()
        {
            if (File.Exists(FileLocalBackup))
            {
                using (StreamReader reader = new StreamReader(FileLocalBackup))
                {
                    String configStr = reader.ReadToEnd();
                    //ViewModel.LearnerConfig = AMLLearnerConfig.FromJsonString(configStr);
                    SettingsViewModel defaultConfig = Newtonsoft.Json.JsonConvert.DeserializeObject<SettingsViewModel>(configStr);
                    this.copy(defaultConfig);
                }

                IsInitializedFromFile = true;
                return true;                
            }

            return false;
        }

        public void init()
        {
            LearnerConfig = new AMLLearnerConfig();
            //Home = "D:/repositories/aml/aml_framework/src/test/resources/demo";
            //_path = "D:/repositories/aml/aml_framework/target/bin";
            //CommandStartServer = _path + "/server.bat";
            //Port = 4343;
            //NumResults = 5;

            //LearnerConfig.Home = Home;
            //PreviousHome = Home;
            //PreviousPath = Path;

            PrevisouLearnerConfig = LearnerConfig;                       
        }

        public void Backup()
        {
            PreviousHome = Home;
            PreviousPath = Path;
            PrevisouLearnerConfig = LearnerConfig;
        }

        public void Reset()
        {
            Home = PreviousHome;
            Path = PreviousPath;
            LearnerConfig = PrevisouLearnerConfig;
        }

        private string _home;

        [JsonProperty("Home")]
        public string Home
        {
            get { return _home; }
            set
            {                
                _home = value;
                LearnerConfig.Home = _home;
                DirTmp = _home + "/tmp/";
                Console.WriteLine("setting home to: " + value);
                RaisePropertyChanged("Home");
            }
        }        

        private string _path;

        [JsonProperty("Path")]
        public string Path
        {
            get { return _path; }
            set
            {
                String server = value + "/server.bat";                

                if (System.IO.File.Exists(server))
                {
                    _path = value;
                    CommandStartServer = server;
                    Console.WriteLine("setting aml learner path to: " + value);
                    RaisePropertyChanged("Path");
                }                
                else
                    System.Windows.MessageBox.Show("Can not find the server.bat program in the path!");
            }
        }

        private int _port;

        [JsonProperty("Port")]
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

        [JsonProperty("NumResults")]
        public int NumResults
        {
            get { return _numResults; }
            set
            {
                _numResults = value;
                Console.WriteLine("setting num results to: " + value);
            }
        }

        public string toJsonString()
        {
            return JsonConvert.SerializeObject(
                                            this,
                                            Formatting.Indented,
                                            new JsonSerializerSettings
                                            {
                                                ContractResolver = new ShouldSerializeContractResolver(),
                                                NullValueHandling = NullValueHandling.Ignore
                                            });
        }
    }
}
