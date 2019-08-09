using Aml.Editor.Plugin.Contracts;
using Aml.Engine.CAEX;
using Aml.Engine.CAEX.Extensions;
using Aml.Toolkit.ViewModel;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Aml.Editor.PlugIn.TestPlugin.ViewModel
{
    public class TestViewModel : ViewModelBase
    {
        public Test Plugin
        {
            get;
            set;
        }

        public enum ObjectType { IE, EI, UNKNOWN };
        public enum ExampleType { POSITIVE, NEGATIVE };
        public enum ExampleCollectionType { SELECTED, DEDUCED };

        private AMLTreeViewModel _aMLDocumentTreeViewModelPos;
        private AMLTreeViewModel _aMLDocumentTreeViewModelNeg;
        private AMLTreeViewModel _aMLDocumentTreeViewModelACM;

        /// <summary>
        /// Gets the singleton instance of the view model
        /// </summary>
        public static TestViewModel Instance { get; private set; }

        public CAEXDocument DocumentPos { get; private set; }
        public CAEXDocument DocumentNeg { get; private set; }
        public CAEXDocument DocumentACM { get; private set; }

        public List<CAEXObject> Positives { get; private set; }
        public List<CAEXObject> Negatives { get; private set; }

        public ObjectType ObjType { get; private set; } = ObjectType.UNKNOWN;

        private InstanceHierarchyType IhSelectedPos { get; set; }
        private InstanceHierarchyType IhSelectedNeg { get; set; }
        public InternalElementType PlaceholderSelectedPos { get; set; }
        public InternalElementType PlaceholderSelectedNeg { get; set; }
        public Boolean PlaceholderPosAttached { get; set; } = false;
        public Boolean PlaceholderNegAttached { get; set; } = false;

        //private InstanceHierarchyType IhDeducedPos { get; set; }
        //private InstanceHierarchyType IhDeducedNeg { get; set; }
        //public Boolean DeducedIhsAttached { get; set; } = false;
        //private InternalElementType PlaceholderDeducedPos { get; set; }
        //private InternalElementType PlaceholderDeducedNeg { get; set; }


        static TestViewModel()
        {
            Instance = new TestViewModel();
        }

        private TestViewModel()
        {
            Positives = new List<CAEXObject>();
            Negatives = new List<CAEXObject>();
            DocumentPos = CAEXDocument.New_CAEXDocument();
            DocumentNeg = CAEXDocument.New_CAEXDocument();

            IhSelectedPos = DocumentPos.CAEXFile.InstanceHierarchy.Append("selectedPositives");
            IhSelectedNeg = DocumentNeg.CAEXFile.InstanceHierarchy.Append("selectedNegatives");

            BuildTreeViewModel();
        }

        public void loadACM(String filename)
        {
            DocumentACM = CAEXDocument.LoadFromFile(filename);
            AMLDocumentTreeViewModelACM = new AMLTreeViewModel(DocumentACM.CAEXFile.Node, AMLTreeViewTemplate.CompleteInstanceHierarchyTree);
        }

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
                    //RaisePropertyChanged(() => AMLDocumentTreeViewModel);

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
        public AMLTreeViewModel AMLDocumentTreeViewModelACM
        {
            get
            {
                return _aMLDocumentTreeViewModelACM;
            }
            set
            {
                if (_aMLDocumentTreeViewModelACM != value)
                {
                    _aMLDocumentTreeViewModelACM = value;
                    RaisePropertyChanged(() => AMLDocumentTreeViewModelACM);

                    // we need a handler to recognize a selection in the tree view. Every selection can be propagated to every plugIn.
                    if (AMLDocumentTreeViewModelACM != null)
                        AMLDocumentTreeViewModelACM.SelectedElements.CollectionChanged += SelectedElementsCollectionChanged;
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
                    Plugin.ChangeSelectedObjectWithPrefix(CurrentSelectedObject, "plugin");
                }
            }
        }       

        public void AddPositive(CAEXObject obj)
        {           
            if (obj is InternalElementType)
            {
                if (ObjType.Equals(ObjectType.EI))
                {
                    MessageBox.Show("can not mixing EI and IE in one list!");
                }
                else
                {
                    ObjType = ObjectType.IE;
                    IhSelectedPos.InternalElement.Insert((InternalElementType)obj);
                    Positives.Add(obj);                    
                }                    
            }                

            if (obj is ExternalInterfaceType)
            {
                if (ObjType.Equals(ObjectType.IE))
                {
                    MessageBox.Show("can not mixing EI and IE in one list!");
                }

                if (!IhSelectedPos.InternalElement.Exists)
                {
                    PlaceholderSelectedPos = IhSelectedPos.InternalElement.Append("PlaceHolder");
                }

                else if (!PlaceholderPosAttached)
                {
                    IhSelectedPos.InternalElement.Insert(PlaceholderSelectedPos);
                }

                PlaceholderPosAttached = true;

                ObjType = ObjectType.EI;
                PlaceholderSelectedPos.ExternalInterface.Insert((ExternalInterfaceType)obj);
                Positives.Add(obj);                
            }
            RemoveNegativeObj(obj);
            UpdateTreeViewModel();
        }

        public void AddNegative(CAEXObject obj)
        {

            if (obj is InternalElementType)
            {
                IhSelectedNeg.InternalElement.Insert((InternalElementType)obj);
                Negatives.Add(obj);
            }

            if (obj is ExternalInterfaceType)
            {
                if (PlaceholderSelectedNeg is null)
                {
                    PlaceholderSelectedNeg = IhSelectedNeg.InternalElement.Append("PlaceHolder");
                }
                else if (!PlaceholderNegAttached)
                {
                    IhSelectedNeg.InternalElement.Insert(PlaceholderSelectedNeg);
                }

                PlaceholderNegAttached = true;
                PlaceholderSelectedNeg.ExternalInterface.Insert((ExternalInterfaceType)obj);
                Negatives.Add(obj);                
            }

            RemovePositiveObj(obj);
            UpdateTreeViewModel();
        }

        public void AddNegative(List<CAEXObject> objs)
        {
            foreach (CAEXObject obj in objs)
            {
                if (obj is InternalElementType)
                {
                    IhSelectedNeg.InternalElement.Insert((InternalElementType)obj);
                    Negatives.Add(obj);
                }

                if (obj is ExternalInterfaceType)
                {
                    if (PlaceholderSelectedNeg is null)
                    {
                        PlaceholderSelectedNeg = IhSelectedNeg.InternalElement.Append("PlaceHolder");
                    }

                    else if (!PlaceholderNegAttached)
                    {
                        IhSelectedNeg.InternalElement.Insert(PlaceholderSelectedNeg);
                    }

                    PlaceholderNegAttached = true;

                    PlaceholderSelectedNeg.ExternalInterface.Insert((ExternalInterfaceType)obj);
                    Negatives.Add(obj);
                }

                RemovePositiveObj(obj);
            }

            UpdateTreeViewModel();
        }

        public void RemoveObj(CAEXObject obj)
        {

            if (obj.Equals(PlaceholderSelectedPos))
            {
                MessageBox.Show("removing all EIs from this place holder object!");
                foreach (ExternalInterfaceType ei in PlaceholderSelectedPos.ExternalInterface)
                {                    
                    RemovePositiveObj(ei);                    
                }
                PlaceholderSelectedPos.Remove();
                PlaceholderPosAttached = false;
                ObjType = ObjectType.UNKNOWN;
            }

            else if (obj.Equals(PlaceholderSelectedNeg))
            {
                MessageBox.Show("removing all EIs from this place holder object!");
                foreach (ExternalInterfaceType ei in PlaceholderSelectedNeg.ExternalInterface)
                {                    
                    RemovePositiveObj(ei);                    
                }
                PlaceholderSelectedNeg.Remove();
                PlaceholderNegAttached = false;
            }

            else
            {
                RemovePositiveObj(obj);
                RemoveNegativeObj(obj);
            }
            
            UpdateTreeViewModel();
        }        

        public void RemovePositiveObj(CAEXObject obj)
        {
            if (obj is InternalElementType)
            {
                foreach (CAEXObject ele in IhSelectedPos.InternalElement.ToList())
                {
                    if (ele.ID.Equals(obj.ID))
                    {
                        ele.Remove();
                    }
                }
            }

            if (obj is ExternalInterfaceType)
            {
                if (IhSelectedPos.InternalElement.Exists && PlaceholderSelectedPos != null)
                {
                    foreach (CAEXObject ele in PlaceholderSelectedPos.ExternalInterface.ToList())
                    {
                        if (ele.ID.Equals(obj.ID))
                        {
                            ele.Remove();
                        }
                    }
                }                
            }          

            Positives.RemoveAll(item => item.ID.Equals(obj.ID));
        }

        public void ClearPositives()
        {
            Positives.Clear();
            IhSelectedPos.InternalElement.Remove();
            UpdateTreeViewModel();
            ObjType = ObjectType.UNKNOWN;
        }

        public void ClearNegatives()
        {
            Negatives.Clear();
            IhSelectedNeg.InternalElement.Remove();
            UpdateTreeViewModel();
        }

        public void RemoveNegativeObj(CAEXObject obj)
        {

            if (obj is InternalElementType)
            {
                foreach (CAEXObject ele in IhSelectedNeg.InternalElement.ToList())
                {
                    if (ele.ID.Equals(obj.ID))
                    {
                        ele.Remove();
                    }
                }
            }

            if (obj is ExternalInterfaceType)
            {
                if (IhSelectedNeg.InternalElement.Exists && PlaceholderSelectedNeg != null)
                {
                    foreach (CAEXObject ele in PlaceholderSelectedNeg.ExternalInterface.ToList())
                    {
                        if (ele.ID.Equals(obj.ID))
                        {
                            ele.Remove();
                        }
                    }
                }                
            }

            Negatives.RemoveAll(item => item.ID.Equals(obj.ID));
        }

        public bool ContainsPositiveExample(CAEXObject obj)
        {
            //var caexObj = DocumentPos.FindByID(obj.ID);
            //if (caexObj is null)
            //    return false;
            //if (caexObj.CAEXParent is InstanceHierarchyType)
            //    return true;
            //return false;

            //return (caexObj == null) ? false : true;  

            //return Positives.Contains(obj);
            return ContainsExample(Positives, obj);
        }

        public bool ContainsNegativeExample(CAEXObject obj)
        {
            //var caexObj = DocumentNeg.FindByID(obj.ID);
            //if (caexObj is null)
            //    return false;
            //if (caexObj.CAEXParent is InstanceHierarchyType)
            //    return true;
            //return false;

            //return (caexObj == null) ? false : true;

            //return Negatives.Contains(obj);

            return ContainsExample(Negatives, obj);
        }

        public bool ContainsExample(CAEXObject obj)
        {
            return ContainsPositiveExample(obj) || ContainsNegativeExample(obj);
        }

        private bool ContainsExample(List<CAEXObject> list, CAEXObject obj)
        {
            foreach (CAEXObject ele in list)
            {
                if (ele.ID.Equals(obj.ID))
                    return true;
            }

            return false;
        }

        private void UpdateTreeViewModel()
        {
            BuildTreeViewModel();
            AMLDocumentTreeViewModelPos.RefreshTree(true);
            AMLDocumentTreeViewModelPos.RefreshNodeInformation(true);
            AMLDocumentTreeViewModelNeg.RefreshTree(true);
            AMLDocumentTreeViewModelNeg.RefreshNodeInformation(true);
            
            RaisePropertyChanged(() => AMLDocumentTreeViewModelPos);
            RaisePropertyChanged(() => AMLDocumentTreeViewModelNeg);
        }

        /// <summary>
        /// Builds the TreeView model for the generated test data.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        private void BuildTreeViewModel()
        {
            // use the InstanceHierarchy template for the created tree view because our document contains an IH only.
            AMLDocumentTreeViewModelPos = new AMLTreeViewModel(DocumentPos.CAEXFile.Node, AMLTreeViewTemplate.CompleteInstanceHierarchyTree);
            AMLDocumentTreeViewModelNeg = new AMLTreeViewModel(DocumentNeg.CAEXFile.Node, AMLTreeViewTemplate.CompleteInstanceHierarchyTree);
            
            // expands the first level
            AMLDocumentTreeViewModelPos.Root.Children[0].IsExpanded = true;
            AMLDocumentTreeViewModelNeg.Root.Children[0].IsExpanded = true;
        }

        internal void SelectPos(ICAEXWrapper caexObject, bool activate)
        {
            var lib = caexObject.Library();
            if (lib is InstanceHierarchyType)
            {
                AMLDocumentTreeViewModelPos?.SelectCaexNode(caexObject.Node, true, true);
                if (activate)
                    AMLDocumentTreeViewModelPos.RaisePropertyChanged("Activate");
            }
        }

        internal void SelectNeg(ICAEXWrapper caexObject, bool activate)
        {
            var lib = caexObject.Library();
            if (lib is InstanceHierarchyType)
            {
                AMLDocumentTreeViewModelNeg?.SelectCaexNode(caexObject.Node, true, true);
                if (activate)
                    AMLDocumentTreeViewModelNeg.RaisePropertyChanged("Activate");
            }
        }

        //public void attachDeducedIhs()
        //{
        //    IhDeducedPos = DocumentPos.CAEXFile.InstanceHierarchy.Append("deducedPositives");
        //    IhDeducedNeg = DocumentNeg.CAEXFile.InstanceHierarchy.Append("deducedNegatives");
        //    DeducedIhsAttached = true;
        //}

        //public void addObjectTo(CAEXObject obj, ExampleType exampleType, ExampleCollectionType collectionType)
        //{
        //    InstanceHierarchyType target, counterTarget;
        //    if (exampleType.Equals(ExampleType.POSITIVE) && collectionType.Equals(ExampleCollectionType.DEDUCED))
        //    {
        //        target = IhDeducedPos;
        //    }
        //    else if (exampleType.Equals(ExampleType.POSITIVE) && collectionType.Equals(ExampleCollectionType.SELECTED))
        //    {
        //        target = IhSelectedPos;
        //        counterTarget = IhSelectedNeg;
        //    }
        //    else if (exampleType.Equals(ExampleType.NEGATIVE) && collectionType.Equals(ExampleCollectionType.DEDUCED))
        //    {
        //        target = IhDeducedNeg;
        //    }
        //    else
        //    {
        //        target = IhSelectedNeg;
        //        counterTarget = IhSelectedPos;
        //    }            

        //}

        public void AddDeducedExamples(List<CAEXObject> objs, ExampleType type, String ihName)
        {
            InstanceHierarchyType ih;

            if (type.Equals(ExampleType.POSITIVE))
            {
                ih = DocumentPos.CAEXFile.InstanceHierarchy.Append(ihName);
            }
            else
            {
                ih = DocumentNeg.CAEXFile.InstanceHierarchy.Append(ihName);
            }

            InternalElementType placeholder = null;
            // if there is at least one EI in the list, add a placeholder
            foreach (CAEXObject obj in objs)
            {
                if (obj is ExternalInterfaceType)
                {
                    placeholder = ih.InternalElement.Append("placeholder");
                    break;
                }
            }
            
            foreach (CAEXObject obj in objs)
            {
                AddDeducedExample(obj, ih, placeholder);
            }

            UpdateTreeViewModel();
        }

        private void AddDeducedExample(CAEXObject obj, InstanceHierarchyType ih, InternalElementType placeholder)
        {
            if (obj is InternalElementType)
            {
                ih.InternalElement.Insert((InternalElementType)obj);
            }

            else if (obj is ExternalInterfaceType)
            {
                if (placeholder is null)
                {
                    //TODO: handle exception
                    return;
                }
                placeholder.ExternalInterface.Insert((ExternalInterfaceType)obj);
            }
        }
        

        /// <summary>
        /// Generates some automationML test data to be viewed in the tree
        /// </summary>
        private void GenerateSomeAutomationMLTestData(string file)
        {
            // we want unique names for the created elements
            Engine.Services.UniqueNameService.Register();

            DocumentPos = CAEXDocument.New_CAEXDocument();
            var ih = DocumentPos.CAEXFile.InstanceHierarchy.Append(file);
            var slib = DocumentPos.CAEXFile.SystemUnitClassLib.Append("SLib");
            var rand = new Random(DateTime.Now.Millisecond);
            for (int i = 0; i < 10; i++)
            {
                var s = slib.SystemUnitClass.Append();
                for (int j = 0; j < rand.Next(5); j++)
                    s.InternalElement.Append();
                for (int j = 0; j < rand.Next(3); j++)
                    s.ExternalInterface.Append();
            }

            for (int i = 0; i < 15; i++)
            {
                ih.InternalElement.Insert(slib.SystemUnitClass[rand.Next(0, 9)].CreateClassInstance(), false);
                ih.InternalElement.Last.Name = "IE of " + ih.InternalElement.Last.Name;
            }

            DocumentNeg = CAEXDocument.New_CAEXDocument();
            var ih2 = DocumentNeg.CAEXFile.InstanceHierarchy.Append(file);
            var slib2 = DocumentNeg.CAEXFile.SystemUnitClassLib.Append("SLib");
            for (int i = 0; i < 10; i++)
            {
                var s = slib2.SystemUnitClass.Append();
                for (int j = 0; j < rand.Next(5); j++)
                    s.InternalElement.Append();
                for (int j = 0; j < rand.Next(3); j++)
                    s.ExternalInterface.Append();
            }

            for (int i = 0; i < 15; i++)
            {
                ih2.InternalElement.Insert(slib2.SystemUnitClass[rand.Next(0, 9)].CreateClassInstance(), false);
                ih2.InternalElement.Last.Name = "IE of " + ih2.InternalElement.Last.Name;
            }

        }
    }
}
