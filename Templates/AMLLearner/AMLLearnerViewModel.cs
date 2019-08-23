
using Aml.Editor.PlugIn.AMLLearner.json;
using Aml.Engine.CAEX;
using Aml.Engine.CAEX.Extensions;
using Aml.Engine.Services;
using Aml.Toolkit.ViewModel;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    public class AcmFeature : IEqualityComparer<AcmFeature>, IEquatable<AcmFeature>
    {
        public string Type { get; set; }
        public string Name { get; set; }

        public AcmFeature(string type, string name)
        {
            Type = type;
            Name = name;
            Fullname = Type + ":" + Name;
        }

        public string Fullname { get; set; }

        public bool Equals(AcmFeature x, AcmFeature y)
        {
            return x.Type.Equals(y.Type) && x.Name.Equals(y.Name);
        }

        public int GetHashCode(AcmFeature obj)
        {
            return (Type + Name).GetHashCode();
        }

        public bool Equals(AcmFeature other)
        {
            return this.Type.Equals(other.Type) && this.Name.Equals(other.Name);
        }
    }

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

        //TODO: this need to be changed
        // server saves last acm results still to this file
        // but also copies the acm that is set for learning to the user folder
        // each such copied acm file has only one acm
        // each set action replaces the current acm file in user folder
        // if the config file contains acm, then copy the acm to the user folder before running
        //public readonly string AcmReultFile;

        //public readonly String AmlFile = "data_3.0_SRC.aml";
        //public string AmlFile { get; set; }
        //public AMLLearnerConfig Config { get; set; }
        public AMLLearnerACMConfig SelectedAcm { get; set; }

        private ObservableCollection<AcmFeature> _selectedAcmFeatures;
        public ObservableCollection<AcmFeature> SelectedAcmFeatures
        {
            get
            {
                return _selectedAcmFeatures;
            }
            set
            {
                _selectedAcmFeatures = value;
                RaisePropertyChanged(() => SelectedAcmFeatures);
            }
        }
        private ObservableCollection<AcmFeature> _ignoredAcmFeatures;
        public ObservableCollection<AcmFeature> IgnoredAcmFeatures
        {
            get
            {
                return _ignoredAcmFeatures;
            }
            set
            {
                _ignoredAcmFeatures = value;
                RaisePropertyChanged(() => IgnoredAcmFeatures);
            }
        }

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

        public string _indicationText;
        public string IndicationText
        {
            get
            {
                return _indicationText;
            }
            set
            {
                _indicationText = value;
                RaisePropertyChanged(() => IndicationText);
            }
        }

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

            if (!Settings.IsInitializedFromFile)
                Settings.initFromFile();

            SelectedAcm = new AMLLearnerACMConfig();
            //Acm.File = Settings.getAcmResultFile();
            SelectedAcm.File = Settings.FileAcmInUse;

            SelectedAcmFeatures = new ObservableCollection<AcmFeature>();            
            IgnoredAcmFeatures = new ObservableCollection<AcmFeature>();
        }

        public void loadACM()
        {
            TreeAcm = new AMLLearnerTree(CAEXDocument.LoadFromFile(Settings.getAcmResultFile()));
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
            SelectedAcm.Id = null;
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


        public bool IsDistinguishedSelectedAcm(CAEXObject obj)
        {
            if (TreeAcm is null)
                return false;

            // since ACM can be nested, and the first level object can be a placeholder, we need to find the one with "distinguished=true"
            foreach (InternalElementType ie in TreeAcm.Ihs[0].Ih.Descendants<InternalElementType>())
            {
                if (ie.ID.Equals(obj.ID))
                {
                    AttributeType config = AcmUtilities.GetConfigAttribute(ie);
                    if (config != null)
                    {
                        AMLConceptConfig acm = AcmUtilities.toAcmConfig(config);
                        if (acm.IsDistinguished)
                            return true;
                    }
                }
            }

            foreach (ExternalInterfaceType ei in TreeAcm.Ihs[0].Ih.Descendants<ExternalInterfaceType>())
            {
                if (ei.ID.Equals(obj.ID))
                {
                    AttributeType config = AcmUtilities.GetConfigAttribute(ei);
                    if (config != null)
                    {
                        AMLConceptConfig acm = AcmUtilities.toAcmConfig(config);
                        if (acm.IsDistinguished)
                            return true;
                    }
                }
            }

            return false;
        }
        

        public bool IsSelectedAcm(CAEXObject obj)
        {
            if (TreeAcm.IsEmpty())
            {
                return false;
            }
            else
            {               
                return TreeAcm.ContainsObject(obj);
            }
        }

        public void AddAcm(CAEXObject obj)
        {
            //TODO: need to equip the object with default acm config

            if (obj is InternalElementType)
            {                
                InternalElementType ie = (InternalElementType)obj;
                AcmUtilities.toAcm(ref ie, true);
            }

            else if (obj is ExternalInterfaceType)
            {
                ExternalInterfaceType ei = (ExternalInterfaceType)obj;
                AcmUtilities.toAcm(ref ei, true);
            }

            TreeAcm.AddObjectToIh(TreeAcm.Ihs[0], obj);
            //MessageBox.Show("obj: " + obj.ID + ", added: " + TreeAcm.Ihs[0].Ih.InternalElement.Last.ID);

            //if (TreeAcm.Ihs[0].Ih.InternalElement.Exists)
            //    MessageBox.Show("obj: " + obj.ID + ", last:" + TreeAcm.Ihs[0].Ih.InternalElement.Last.ID);
            //else
            //    MessageBox.Show("obj: " + obj.ID + ", empty tree");

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

        public List<CAEXObject> GetAllLoadedAcms()
        {
            return TreeAcm.Ihs[0].GetAllObjects();
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

        public void AdaptQueryConfig(CAEXObject obj, String config, String value)
        {            
            AttributeType configAttr = AcmUtilities.GetConfigAttribute(obj);
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
                if(IsSelectedAcm((CAEXObject) CurrentSelectedObject))
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
                if (IsSelectedAcm((CAEXObject)CurrentSelectedObject))
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

                if (IsSelectedAcm((CAEXObject)CurrentSelectedObject))
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

                if (IsSelectedAcm((CAEXObject)CurrentSelectedObject))
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

                if (IsSelectedAcm((CAEXObject)CurrentSelectedObject))
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

                if (IsSelectedAcm((CAEXObject)CurrentSelectedObject))
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

                if (IsSelectedAcm((CAEXObject)CurrentSelectedObject))
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

            Settings.LearnerConfig.Algorithm.Ignored = getIgnored();

            //Config = new AMLLearnerConfig(Settings.Home, AmlFile, objType, examples);
            //Settings.LearnerConfig.Aml = AmlFile;
            Settings.LearnerConfig.reinitialize(Settings.Home, Settings.LearnerConfig.Aml, objType, examples);

            if (SelectedAcm.Id != null)
            {
                //UpdateAcm();
                Settings.LearnerConfig.Algorithm.Acm = SelectedAcm;        
            }
        }

        private AMLLearnerIgnoredFeaturesConfig getIgnored()
        {
            AMLLearnerIgnoredFeaturesConfig ignored = new AMLLearnerIgnoredFeaturesConfig();
            List<String> concepts = new List<string>();
            List<String> dataProperties = new List<string>();
            List<String> objectProperties = new List<string>();

            foreach (AcmFeature feature in IgnoredAcmFeatures)
            {
                if (feature.Type.Equals("class"))
                {
                    concepts.Add(feature.Name);
                }

                else if (feature.Type.Equals("attribute"))
                {
                    dataProperties.Add(feature.Name);
                }                
            }

            ignored.Concepts = concepts.ToArray();
            ignored.DataProperties = dataProperties.ToArray();
            return ignored;
        }


        /// <summary>
        /// Update the ACM tree if the current selected object is an ACM object
        /// Use the UI setting to update the config parameters of the selected ACM object
        /// Write the update to the ACM file written by the server in the tmp folder 
        /// </summary>
        public void UpdateAcm()
        {
            if (IsSelectedAcm((CAEXObject)CurrentSelectedObject))
            {
                // update the selected ACM
                SelectedAcm.Id = ((CAEXObject)CurrentSelectedObject).ID;

                // create an AML file to store the selected ACM for future learning
                CAEXDocument acm = CAEXDocument.New_CAEXDocument();
                InstanceHierarchyType ih = acm.CAEXFile.InstanceHierarchy.Append("acm");

                if (CurrentSelectedObject is InternalElementType)
                {
                    ih.InternalElement.Insert((InternalElementType)CurrentSelectedObject);
                }
                else if (CurrentSelectedObject is ExternalInterfaceType)
                {
                    InternalElementType placeholder = ih.InternalElement.Append("PlaceHolder");
                    placeholder.Insert((ExternalInterfaceType)CurrentSelectedObject);
                }
                
                acm.SaveToFile(Settings.FileAcmInUse, true);
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


        private CAEXDocument Open(string filePath)
        {
            CAEXDocument document = CAEXDocument.LoadFromFile(filePath);

            // register the transformation service. After registration of the service, the AMLEngine
            // communicates with the transformation service via event notification.
            var transformer = CAEXSchemaTransformer.Register();

            // transform the document to AutomationML 2.1 and CAEX 3.0
            document = transformer.TransformTo(document, CAEXDocument.CAEXSchema.CAEX3_0);

            // unregister the transformation service. The communication channel between the AMLEngine and
            // the transformation service is closed.
            CAEXSchemaTransformer.UnRegister();

            return document;
        }

        private bool IsAcmFeature(AttributeType attr)
        {
            if (attr.Value != null && attr.Value != "")
                return true;

            if (attr.Constraint.Exists)
                return true;

            return false;
        }


        /// <summary>
        /// Get the Acm features of the CAEX attribute: attributes, class references
        /// </summary>
        /// <param name="attr"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        private ObservableCollection<AcmFeature> GetAcmFeatures(AttributeType attr, String prefix)
        {
            ObservableCollection<AcmFeature> features = new ObservableCollection<AcmFeature>();
            if (!AcmUtilities.IsConfigAttribute(attr))
            {                
                if (prefix != "")
                {
                    prefix += "_";
                }

                if (IsAcmFeature(attr))
                {
                    features.Add(new AcmFeature("attribute", prefix + attr.Name));
                }

                foreach(AttributeType child in attr.Attribute)
                {
                    ObservableCollection<AcmFeature> childFeatures = GetAcmFeatures(child, prefix + attr.Name);
                    foreach (AcmFeature childFeature in childFeatures)
                    {
                        if (!features.Contains(childFeature))
                            features.Add(childFeature);
                    }
                }
                
            }

            return features;
        }

        /// <summary>
        /// Get the Acm features of the CAEX object: attributes, class references       
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public ObservableCollection<AcmFeature> GetAcmFeatures(CAEXObject obj)
        {
            ObservableCollection<AcmFeature> features = new ObservableCollection<AcmFeature>();
            if (IsSelectedAcm(obj))
            {                
                if (obj is InternalElementType)
                {
                    InternalElementType ie = (InternalElementType)obj;
                    foreach (SupportedRoleClassType src in ie.SupportedRoleClass)
                    {
                        features.Add(new AcmFeature("class", src.RefRoleClassPath));
                    }

                    foreach (RoleRequirementsType rr in ie.RoleRequirements)
                    {
                        features.Add(new AcmFeature("class", rr.RefBaseRoleClassPath));
                    }

                    foreach (AttributeType attr in ie.Attribute)
                    {
                        if (!AcmUtilities.IsConfigAttribute(attr))
                        {
                            foreach (AcmFeature childFeature in GetAcmFeatures(attr, ""))
                            {
                                if (!features.Contains(childFeature))
                                    features.Add(childFeature);
                            }
                        }
                    }

                    foreach (InternalElementType child in ie.InternalElement)
                    {
                        foreach (AcmFeature childFeature in GetAcmFeatures(child))
                        {
                            if (!features.Contains(childFeature))
                                features.Add(childFeature);
                        }
                    }

                    foreach (ExternalInterfaceType child in ie.ExternalInterface)
                    {
                        foreach (AcmFeature childFeature in GetAcmFeatures(child))
                        {
                            if (!features.Contains(childFeature))
                                features.Add(childFeature);
                        }
                    }
                }

                else if (obj is ExternalInterfaceType)
                {
                    ExternalInterfaceType ei = (ExternalInterfaceType)obj;
                    if (ei.RefBaseClassPath != null)
                    {
                        features.Add(new AcmFeature("class", ei.RefBaseClassPath));
                    }

                    foreach (AttributeType attr in ei.Attribute)
                    {
                        if (!AcmUtilities.IsConfigAttribute(attr))
                        {
                            foreach (AcmFeature childFeature in GetAcmFeatures(attr, ""))
                            {
                                if (!features.Contains(childFeature))
                                    features.Add(childFeature);
                            }
                        }
                    }
                }                
            }

            return features;
        }


        /// <summary>
        /// Remove the given Acm feature from the current learning session:
        /// it will not be used for learning of any examples
        /// </summary>
        /// <param name="feature"></param>
        public void RemoveActiveAcmFeature(AcmFeature feature)
        {
            SelectedAcmFeatures.Remove(feature);

            if(!IgnoredAcmFeatures.Contains(feature))
                IgnoredAcmFeatures.Add(feature);

            CAEXObject obj = RemoveActiveAcmFeature(feature, (CAEXObject)CurrentSelectedObject);

            //TreeAcm.RemoveObject((CAEXObject)CurrentSelectedObject);
            //TreeAcm.AddObjectToIh(TreeAcm.Ihs[0], obj);
            UpdateTreeViewModel(TreeType.ACM);
        }

        /// <summary>
        /// Remove the given Acm feature from the given caex object: remove it from the XML data object
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        private CAEXObject RemoveActiveAcmFeature(AcmFeature feature, CAEXObject obj)
        {            
            if (feature.Type.Equals("class"))
            {
                if (obj is ExternalInterfaceType)
                {
                    ExternalInterfaceType ei = (ExternalInterfaceType)obj;
                    ei.RefBaseClassPath = null;
                }

                else if (obj is InternalElementType)
                {
                    InternalElementType ie = (InternalElementType)obj;
                    
                    foreach(SupportedRoleClassType src in ie.SupportedRoleClass.ToList())
                    {
                        if (src.RefRoleClassPath.Equals(feature.Name))
                        {
                            ie.SupportedRoleClass.RemoveElement(src);
                            break;
                        }
                    }

                    foreach (RoleRequirementsType rr in ie.RoleRequirements.ToList())
                    {
                        if (rr.RefBaseRoleClassPath.Equals(feature.Name))
                        {
                            ie.RoleRequirements.RemoveElement(rr);
                            break;
                        }
                    }

                    foreach (InternalElementType child in ie.InternalElement.ToList())
                    {
                        RemoveActiveAcmFeature(feature, child);
                    }

                    foreach (ExternalInterfaceType child in ie.ExternalInterface.ToList())
                    {
                        RemoveActiveAcmFeature(feature, child);
                    }
                }
            }

            if (feature.Type.Equals("attribute"))
            {

                if (obj is AttributeType)
                {
                    AttributeType attr = (AttributeType)obj;
                    string featureName = feature.Name;
                    string[] tokens = featureName.Split('_');

                    // if the attribute is the same as the last token of the feature name
                    // remove
                    if (tokens[tokens.Length - 1].Equals(attr.Name))
                    {
                        attr.Remove();
                    }

                    // if the attribute is not the same as the last token of the feature name
                    // but is part of the feature name: check its children
                    else if(tokens.Contains(attr.Name))
                    {
                        foreach (AttributeType child in attr.Attribute.ToList())
                        {
                            if(!AcmUtilities.IsConfigAttribute(child))
                                RemoveActiveAcmFeature(feature, child);
                        }

                        // if the attribute has neither value nor constraints: it is a nested attribute
                        // if the nested attribute has no sub attributes: all sub attributes are removed
                        // then we remove the nested attribute also
                        if (attr.Value == null && !attr.Constraint.Exists && attr.Attribute.Exists)
                        {
                            attr.Remove();
                        }
                    }

                    // if the attribute is not part of the feature name
                    // it shall not be removed
                }

                else if (obj is ExternalInterfaceType)
                {
                    ExternalInterfaceType ei = (ExternalInterfaceType)obj;

                    foreach (AttributeType attr in ei.Attribute.ToList())
                    {
                        if(!AcmUtilities.IsConfigAttribute(attr))
                            RemoveActiveAcmFeature(feature, attr);
                    }
                }

                else if (obj is InternalElementType)
                {
                    InternalElementType ie = (InternalElementType)obj;

                    foreach (AttributeType attr in ie.Attribute.ToList())
                    {
                        if (!AcmUtilities.IsConfigAttribute(attr))
                            RemoveActiveAcmFeature(feature, attr);
                    }
                }                
            }

            return obj;
        }

        public void RemoveIgnoredAcmFeature(AcmFeature feature)
        {
            IgnoredAcmFeatures.Remove(feature);
        }


        /// <summary>
        /// Whether a caex attribute is considered to be an Acm feature to be removed
        /// - for example, if the caex attribtue has name "frame", and "featureToBeRemoved" has name "frame_x"
        /// then the caex attribute "frame" shall be removed from its owner CAEX object
        /// - for example, if the caex attribtue has name "x", and "featureToBeRemoved" has name "frame_x"
        /// then the caex attribute "x" shall be removed from its owner CAEX object
        /// </summary>
        /// <param name="featureToBeRemoved"></param>
        /// <param name="attr"></param>
        /// <returns></returns>
        private Boolean ShouldRemoveAttribute(AcmFeature featureToBeRemoved, AttributeType attr)
        {
            if (featureToBeRemoved.Name.Contains(attr.Name))
            {
                return true;
            }
            return false;
        }


    }
}
