
using Aml.Editor.PlugIn.AMLLearner.json;
using Aml.Engine.CAEX;
using Aml.Engine.CAEX.Extensions;
using Aml.Toolkit.ViewModel;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace Aml.Editor.PlugIn.AMLLearner.ViewModel
{
    public enum ObjectType { IE, EI, UNKNOWN };
    public enum ExampleType { POSITIVE, NEGATIVE };
    public enum ExampleCollectionType { SELECTED, DEDUCED };
    public enum TreeType { POSITIVE, NEGATIVE, ACM};

    public class AMLLearnerViewModel : ViewModelBase, INotifyPropertyChanged
    {
        public AMLLearnerGUI Plugin
        {
            get;
            set;
        }

        public SettingsViewModel Settings = SettingsViewModel.Instance;

        private AMLTreeViewModel _aMLDocumentTreeViewModelPos;
        private AMLTreeViewModel _aMLDocumentTreeViewModelNeg;
        private AMLTreeViewModel _aMLDocumentTreeViewModelAcm;

        public readonly string AcmFile = "learned_acm.aml";
        //public readonly String AmlFile = "data_3.0_SRC.aml";
        public string AmlFile { get; set; }
        //public AMLLearnerConfig Config { get; set; }
        public AMLLearnerACMConfig Acm { get; set; }

        /// <summary>
        /// Gets the singleton instance of the view model
        /// </summary>
        public static AMLLearnerViewModel Instance { get; private set; }

        public AMLLearnerTree TreePos;
        public AMLLearnerTree TreeNeg;
        public AMLLearnerTree TreeAcm;


        //public List<CAEXObject> Positives { get; private set; }
        //public List<CAEXObject> Negatives { get; private set; }

        public ObjectType ObjType { get; private set; } = ObjectType.UNKNOWN;

        static AMLLearnerViewModel()
        {
            Instance = new AMLLearnerViewModel();
        }

        private AMLLearnerViewModel()
        {
            //Positives = new List<CAEXObject>();
            TreePos = new AMLLearnerTree();
            TreePos.AddInstanceHiearchy("selectedPositives");

            //Negatives = new List<CAEXObject>();
            TreeNeg = new AMLLearnerTree();
            TreeNeg.AddInstanceHiearchy("selectedNegatives");

            TreeAcm = new AMLLearnerTree();
            TreeAcm.AddInstanceHiearchy("acms");

            UpdateTreeViewModel(TreeType.POSITIVE);
            UpdateTreeViewModel(TreeType.NEGATIVE);
            UpdateTreeViewModel(TreeType.ACM);

            //Home = "D:/repositories/aml/aml_framework/src/test/resources/demo";            

            Acm = new AMLLearnerACMConfig();
            Acm.File = Settings.DirTmp + AcmFile;
        }

        public void loadACM()
        {
            TreeAcm = new AMLLearnerTree(CAEXDocument.LoadFromFile(Settings.DirTmp + AcmFile));
            UpdateTreeViewModel(TreeType.ACM);
        }

        public void loadACM(string filename)
        {
            TreeAcm = new AMLLearnerTree(CAEXDocument.LoadFromFile(filename));
            UpdateTreeViewModel(TreeType.ACM);
        }

        //public void saveACM()
        //{
        //    TreeAcm.Document.SaveToFile(DirTmp + AcmFile, true);
        //}

        /// <summary>
        ///  Gets and sets the AMLDocumentTreeViewModel which holds the data for the AML document tree view
        /// </summary>
        public AMLTreeViewModel AMLDocumentTreeViewModelPos
        {
            get
            {
                return _aMLDocumentTreeViewModelPos;
            }
            set
            {
                if (_aMLDocumentTreeViewModelPos != value)
                {
                    _aMLDocumentTreeViewModelPos = value;
                    RaisePropertyChanged(() => AMLDocumentTreeViewModelPos);

                    // we need a handler to recognize a selection in the tree view. Every selection can be propagated to every plugIn.
                    if (AMLDocumentTreeViewModelPos != null)
                        AMLDocumentTreeViewModelPos.SelectedElements.CollectionChanged += SelectedElementsCollectionChanged;
                }
            }
        }

        /// <summary>
        ///  Gets and sets the AMLDocumentTreeViewModel which holds the data for the AML document tree view
        /// </summary>
        public AMLTreeViewModel AMLDocumentTreeViewModelNeg
        {
            get
            {
                return _aMLDocumentTreeViewModelNeg;
            }
            set
            {
                if (_aMLDocumentTreeViewModelNeg != value)
                {
                    _aMLDocumentTreeViewModelNeg = value;
                    RaisePropertyChanged(() => AMLDocumentTreeViewModelNeg);

                    // we need a handler to recognize a selection in the tree view. Every selection can be propagated to every plugIn.
                    if (AMLDocumentTreeViewModelNeg != null)
                        AMLDocumentTreeViewModelNeg.SelectedElements.CollectionChanged += SelectedElementsCollectionChanged;
                }
            }
        }

        /// <summary>
        ///  Gets and sets the AMLDocumentTreeViewModel which holds the data for the AML document tree view
        /// </summary>
        public AMLTreeViewModel AMLDocumentTreeViewModelAcm
        {
            get
            {
                return _aMLDocumentTreeViewModelAcm;
            }
            set
            {
                if (_aMLDocumentTreeViewModelAcm != value)
                {
                    _aMLDocumentTreeViewModelAcm = value;
                    RaisePropertyChanged(() => AMLDocumentTreeViewModelAcm);

                    // we need a handler to recognize a selection in the tree view. Every selection can be propagated to every plugIn.
                    if (AMLDocumentTreeViewModelAcm != null)
                        AMLDocumentTreeViewModelAcm.SelectedElements.CollectionChanged += SelectedElementsCollectionChanged;
                }
            }
        }

        /// <summary>
        /// Gets the current selected object.
        /// </summary>
        public CAEXBasicObject CurrentSelectedObject { get; private set; }

        /// <summary>
        /// Handler for the SelectedElements collection changed event which will propagate the selection event to every PlugIn.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void SelectedElementsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace)
            {
                AMLNodeViewModel node = e.NewItems.OfType<AMLNodeViewModel>().FirstOrDefault();
                if (node != null)
                {
                    CurrentSelectedObject = node.CAEXObject as CAEXBasicObject;

                    string prefix = "plugin:";
                    if (TreePos.ContainsObject(CurrentSelectedObject))
                        prefix += "positives::";
                    else if (TreeNeg.ContainsObject(CurrentSelectedObject))
                        prefix += "negatives::";
                    else if (TreeAcm.ContainsObject(CurrentSelectedObject))
                        prefix += "acms::";

                    Plugin.ChangeSelectedObjectWithPrefix(CurrentSelectedObject, prefix);
                }
            }
        }

        public Boolean isPlaceHolder(CAEXObject obj)
        {
            return TreePos.HasPlaceHolder(obj) || TreeNeg.HasPlaceHolder(obj);
        }

        public Boolean ContainsPositiveExample(CAEXObject obj)
        {
            return TreePos.ContainsDataObject(obj);
        }

        public Boolean ContainsNegativeExample(CAEXObject obj)
        {
            return TreeNeg.ContainsDataObject(obj);
        }

        public void AddPositive(CAEXObject obj)
        {
            if (obj is InternalElementType)
            {
                if (ObjType.Equals(ObjectType.EI))
                {
                    MessageBox.Show("can not mixing EI and IE in one list!");
                    return;
                }
                else
                {
                    ObjType = ObjectType.IE;
                }
            }

            else if (obj is ExternalInterfaceType)
            {
                if (ObjType.Equals(ObjectType.IE))
                {
                    MessageBox.Show("can not mixing EI and IE in one list!");
                    return;
                }
                else
                {
                    ObjType = ObjectType.EI;
                }
            }

            TreePos.AddObjectToIh(TreePos.Ihs[0], obj);
            //Positives.Add(obj);

            TreeNeg.RemoveObject(obj);
            //Negatives.Remove(obj);

            UpdateTreeViewModel(TreeType.POSITIVE);
            UpdateTreeViewModel(TreeType.NEGATIVE);
        }

        public void AddNegative(CAEXObject obj)
        {
            TreeNeg.AddObjectToIh(TreeNeg.Ihs[0], obj);
            //Negatives.Add(obj);

            TreePos.RemoveObject(obj);
            //Positives.Remove(obj);

            UpdateTreeViewModel(TreeType.POSITIVE);
            UpdateTreeViewModel(TreeType.NEGATIVE);
        }

        public void AddNegative(List<CAEXObject> objs)
        {
            foreach (CAEXObject obj in objs)
            {
                TreeNeg.AddObjectToIh(TreeNeg.Ihs[0], obj);
                //Negatives.Add(obj);

                TreePos.RemoveObject(obj);
                //Positives.Remove(obj);
            }

            UpdateTreeViewModel(TreeType.POSITIVE);
            UpdateTreeViewModel(TreeType.NEGATIVE);
        }

        public void RemoveObj(CAEXObject obj)
        {
            TreePos.RemoveObject(obj);
            TreeNeg.RemoveObject(obj);

            //Positives.RemoveAll(item => item.ID.Equals(obj.ID));
            //Negatives.RemoveAll(item => item.ID.Equals(obj.ID));

            UpdateTreeViewModel(TreeType.POSITIVE);
            UpdateTreeViewModel(TreeType.NEGATIVE);
        }

        public void RemoveAcm(CAEXObject obj)
        {
            TreeAcm.RemoveObject(obj);
            UpdateTreeViewModel(TreeType.ACM);
        }

        public void ClearPositives()
        {
            //Positives.Clear();
            TreePos.Clear();
            ObjType = ObjectType.UNKNOWN;

            UpdateTreeViewModel(TreeType.POSITIVE);            
        }

        public void ClearNegatives()
        {
            //Negatives.Clear();
            TreeNeg.Clear();
            
            UpdateTreeViewModel(TreeType.NEGATIVE);
        }

        public void ClearAcm()
        {
            //Negatives.Clear();
            TreeAcm.Clear();

            UpdateTreeViewModel(TreeType.ACM);
        }

        //public bool ContainsAcm(CAEXObject obj)
        //{
        //    if (TreeAcm != null)
        //    {
        //        return TreeAcm.Contains(obj);
        //    }

        //    return false;
        //}

        public bool IsAcm(CAEXObject obj)
        {
            if (TreeAcm is null)
                return false;

            return TreeAcm.Document.FindByID(obj.ID) != null;
        }

        public void AddAcm(CAEXObject obj)
        {
            //TODO: need to equip the object with default acm config
            TreeAcm.AddObjectToIh(TreeAcm.Ihs[0], obj);
            UpdateTreeViewModel(TreeType.ACM);
        }

        public bool ContainsExample(CAEXObject obj)
        {
            return TreePos.ContainsDataObject(obj) || TreeNeg.ContainsDataObject(obj);
        }

        private void UpdateTreeViewModel(TreeType type)
        {           
            switch (type)
            {
                case TreeType.POSITIVE:
                    AMLDocumentTreeViewModelPos = new AMLTreeViewModel(TreePos.Document.CAEXFile.Node, AMLTreeViewTemplate.CompleteInstanceHierarchyTree);
                    AMLDocumentTreeViewModelPos.Root.Children[0].IsExpanded = true;
                    AMLDocumentTreeViewModelPos.RefreshTree(true);
                    AMLDocumentTreeViewModelPos.RefreshNodeInformation(true);
                    RaisePropertyChanged(() => AMLDocumentTreeViewModelPos);
                    break;

                case TreeType.NEGATIVE:
                    AMLDocumentTreeViewModelNeg = new AMLTreeViewModel(TreeNeg.Document.CAEXFile.Node, AMLTreeViewTemplate.CompleteInstanceHierarchyTree);
                    AMLDocumentTreeViewModelNeg.Root.Children[0].IsExpanded = true;
                    AMLDocumentTreeViewModelNeg.RefreshTree(true);
                    AMLDocumentTreeViewModelNeg.RefreshNodeInformation(true);
                    RaisePropertyChanged(() => AMLDocumentTreeViewModelNeg);
                    break;

                case TreeType.ACM:
                    AMLDocumentTreeViewModelAcm = new AMLTreeViewModel(TreeAcm.Document.CAEXFile.Node, AMLTreeViewTemplate.CompleteInstanceHierarchyTree);
                    AMLDocumentTreeViewModelAcm.Root.Children[0].IsExpanded = true;
                    AMLDocumentTreeViewModelAcm.RefreshTree(true);
                    AMLDocumentTreeViewModelAcm.RefreshNodeInformation(true);
                    RaisePropertyChanged(() => AMLDocumentTreeViewModelAcm);
                    break;

                default:
                    return;
            }
        }

        public void AddDeducedExamples(List<CAEXObject> objs, ExampleType type, String ihName)
        {
            InstanceHierarchyType ih;

            if (type.Equals(ExampleType.POSITIVE))
            {
                //ih = DocumentPos.CAEXFile.InstanceHierarchy.Append(ihName);
                ih = TreePos.AddInstanceHiearchy(ihName);
                foreach (CAEXObject obj in objs)
                    TreePos.AddObjectToIh(ih, obj);
            }
            else
            {
                //ih = DocumentNeg.CAEXFile.InstanceHierarchy.Append(ihName);
                ih = TreeNeg.AddInstanceHiearchy(ihName);
                foreach (CAEXObject obj in objs)
                    TreeNeg.AddObjectToIh(ih, obj);
            }

            UpdateTreeViewModel(TreeType.POSITIVE);
            UpdateTreeViewModel(TreeType.NEGATIVE);
        }

        public List<CAEXObject> GetAllSelectedPositives()
        {
            return TreePos.Ihs[0].GetAllObjects();
        }

        public List<CAEXObject> GetAllSelectedNegatives()
        {
            return TreeNeg.Ihs[0].GetAllObjects();
        }

        private Boolean IsConfigAttribute(AttributeType attr)
        {
            return attr.Name.Equals("queryConfig");
        }

        public AttributeType GetConfigParameter(AttributeType attr, String config)
        {
            AttributeType parameter = null;
            foreach (AttributeType sub in attr.Attribute)
            {
                if (sub.Name.Equals(config))
                    parameter = sub;
            }

            if (parameter is null)
                return attr.New_Attribute(config);
            else
                return parameter;
        }

        public AttributeType GetConfigAttribute(CAEXObject obj)
        {
            if (obj is InternalElementType)
            {
                InternalElementType ie = (InternalElementType)obj;
                foreach (AttributeType attr in ie.Attribute)
                {
                    if (IsConfigAttribute(attr)) {
                        return attr;
                    }
                }
            }

            else if (obj is ExternalInterfaceType)
            {
                ExternalInterfaceType ei = (ExternalInterfaceType)obj;
                foreach (AttributeType attr in ei.Attribute)
                {
                    if (IsConfigAttribute(attr))
                    {
                        return attr;
                    }
                }
            }

            else if (obj is AttributeType)
            {
                AttributeType attribute = (AttributeType)obj;
                foreach (AttributeType attr in attribute.Attribute)
                {
                    if (IsConfigAttribute(attr))
                    {
                        return attr;
                    }
                }
            }

            return null;
        }

        public void AdaptQueryConfig(CAEXObject obj, String config, String value)
        {            
            AttributeType configAttr = GetConfigAttribute(obj);
            AttributeType configParam = GetConfigParameter(configAttr, config);
            configParam.Value = value;
        }

        private Boolean _configPriamry;

        public Boolean ConfigPrimary
        {
            get { return _configPriamry; }
            set
            {
                _configPriamry = value;
                if(IsAcm((CAEXObject) CurrentSelectedObject))
                    AdaptQueryConfig((CAEXObject) CurrentSelectedObject, "distinguished", value.ToString());

                RaisePropertyChanged("ConfigPrimary");
            }
        }

        private Boolean _configId;

        public Boolean ConfigId
        {
            get { return _configId; }
            set
            {
                _configId = value;
                if (IsAcm((CAEXObject)CurrentSelectedObject))
                    AdaptQueryConfig((CAEXObject)CurrentSelectedObject, "identifiedById", value.ToString());

                RaisePropertyChanged("ConfigId");
            }
        }

        private Boolean _configName;

        public Boolean ConfigName
        {
            get { return _configName; }
            set
            {
                _configName = value;

                if (IsAcm((CAEXObject)CurrentSelectedObject))
                    AdaptQueryConfig((CAEXObject)CurrentSelectedObject, "identifiedByName", value.ToString());

                RaisePropertyChanged("ConfigName");
            }
        }

        private Boolean _configNegated;

        public Boolean ConfigNegated
        {
            get { return _configNegated; }
            set
            {
                _configNegated = value;

                if (IsAcm((CAEXObject)CurrentSelectedObject))
                    AdaptQueryConfig((CAEXObject)CurrentSelectedObject, "negated", value.ToString());

                RaisePropertyChanged("ConfigNegated");
            }
        }

        private Boolean _configDescendant;

        public Boolean ConfigDescendant
        {
            get { return _configDescendant; }
            set
            {
                _configDescendant = value;

                if (IsAcm((CAEXObject)CurrentSelectedObject))
                    AdaptQueryConfig((CAEXObject)CurrentSelectedObject, "descendant", value.ToString());

                RaisePropertyChanged("ConfigDescendant");
            }
        }

        private int _configMincardinalitiy;

        public int ConfigMinCardinality
        {
            get { return _configMincardinalitiy; }
            set
            {
                _configMincardinalitiy = value;

                if (IsAcm((CAEXObject)CurrentSelectedObject))
                    AdaptQueryConfig((CAEXObject)CurrentSelectedObject, "minCardinality", value.ToString());

                RaisePropertyChanged("ConfigMinCardinality");
            }
        }

        private int _configMaxcardinalitiy;

        public int ConfigMaxCardinality
        {
            get { return _configMaxcardinalitiy; }
            set
            {
                _configMaxcardinalitiy = value;

                if (IsAcm((CAEXObject)CurrentSelectedObject))
                    AdaptQueryConfig((CAEXObject)CurrentSelectedObject, "maxCardinality", value.ToString());

                RaisePropertyChanged("ConfigMaxCardinality");
            }
        }

        //private string _home;
        //public string Home
        //{
        //    get { return _home; }
        //    set
        //    {
        //        if (value != _home)
        //        {
        //            _home = value;
        //            DirTmp = Home + "/tmp/";
        //            Console.WriteLine("setting home to: " + value);
        //            RaisePropertyChanged("Home");
        //        }
        //    }
        //}        

        //public string DirTmp { get; set; }

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
                    RaisePropertyChanged("AcmId");
                }
            }
        }


        public void UpdateAMLLearnerConfig()
        {
            AMLLearnerExamplesConfig examples = new AMLLearnerExamplesConfig();

            List<String> positives = new List<String>();
            List<String> negatives = new List<String>();
            foreach (CAEXObject obj in GetAllSelectedPositives())
            {
                if (obj is InternalElementType)
                    positives.Add("ie_" + obj.Name + "_" + obj.ID);
                else if (obj is ExternalInterfaceType)
                    positives.Add("ei_" + obj.Name + "_" + obj.ID);
            }
            foreach (CAEXObject obj in GetAllSelectedNegatives())
            {
                if (obj is InternalElementType)
                    negatives.Add("ie_" + obj.Name + "_" + obj.ID);
                else if (obj is ExternalInterfaceType)
                    negatives.Add("ei_" + obj.Name + "_" + obj.ID);
            }
            examples.Positives = positives.ToArray();
            examples.Negatives = negatives.ToArray();

            String objType = "";
            if (ObjType.Equals(ObjectType.IE))
                objType = "IE";
            else if (ObjType.Equals(ObjectType.EI))
                objType = "EI";
            else
                return;

            //Config = new AMLLearnerConfig(Settings.Home, AmlFile, objType, examples);
            //Settings.LearnerConfig.Aml = AmlFile;
            Settings.LearnerConfig.reinitialize(Settings.Home, AmlFile, objType, examples);

            if (Acm.Id != null)
            {
                //UpdateAcm();
                Settings.LearnerConfig.Algorithm.Acm = Acm;        
            }
        }


        /// <summary>
        /// Update the ACM tree if the current selected object is an ACM object
        /// Use the UI setting to update the config parameters of the selected ACM object
        /// Write the update to the ACM file written by the server in the tmp folder 
        /// </summary>
        public void UpdateAcm()
        {
            if (IsAcm((CAEXObject)CurrentSelectedObject))
            {
                Acm.Id = ((CAEXObject)CurrentSelectedObject).ID;
                TreeAcm.Document.SaveToFile(Settings.DirTmp + AcmFile, true);
                MessageBox.Show("successfully set the acm [" + ((CAEXObject) CurrentSelectedObject).Name + "] for learning!");
            }
        }


        /// <summary>
        /// Save the current ACM tree to the given file
        /// This acts as a backup of the ACM file to any position of the file system
        /// The saved file is not used by the AMLLearner system if not configured explicitly
        /// </summary>
        /// <param name="filename"></param>
        public void SaveAcm(String filename)
        {
            TreeAcm.Document.SaveToFile(filename, true);            
        }
    }
}
