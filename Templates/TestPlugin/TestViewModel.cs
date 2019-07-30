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
        private AMLTreeViewModel _aMLDocumentTreeViewModelPos;
        private AMLTreeViewModel _aMLDocumentTreeViewModelNeg;

        /// <summary>
        /// Gets the singleton instance of the view model
        /// </summary>
        public static TestViewModel Instance { get; private set; }

        public CAEXDocument DocumentPos { get; private set; }
        public CAEXDocument DocumentNeg { get; private set; }

        public List<CAEXObject> Positives { get; private set; }
        public List<CAEXObject> Negatives { get; private set; }

        private InstanceHierarchyType IhPos;
        private InstanceHierarchyType IhNeg;

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

            IhPos = DocumentPos.CAEXFile.InstanceHierarchy.Append("positives");
            IhNeg = DocumentNeg.CAEXFile.InstanceHierarchy.Append("negatives");
            BuildTreeViewModel();
            //GenerateSomeAutomationMLTestData("test");
            //BuildTreeViewModel();
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
                }
            }
        }
        

        public void addPositive(CAEXObject obj)
        {

            if (obj is InternalElementType)
            {
                if (IhPos.InternalElement.Exists && IhPos.InternalElement.First.Name.Equals("PlaceHolder"))
                {
                    MessageBox.Show("can not mixing EI and IE in one list!");
                }
                else
                {
                    IhPos.InternalElement.Insert((InternalElementType)obj);
                    Positives.Add(obj);
                    //if (IhNeg.InternalElement.Exists && contains(IhNeg.InternalElement, (InternalElementType)obj))
                    if (contains(Negatives, obj))
                    {
                        remove(IhNeg.InternalElement, (InternalElementType)obj);
                        Negatives.Remove(obj);
                    }
                        
                }                    
            }                

            if (obj is ExternalInterfaceType)
            {
                if (!IhPos.InternalElement.Exists)
                {
                    IhPos.InternalElement.Append("PlaceHolder");
                }
                else if (IhPos.InternalElement.First.Name.Equals("PlaceHolder"))
                {
                    IhPos.InternalElement.First.ExternalInterface.Insert((ExternalInterfaceType)obj);
                }
                else
                {
                    MessageBox.Show("can not mixing EI and IE in one list!");
                }
            }

            updateTreeViewModel();
        }

        public void addNegative(CAEXObject obj)
        {
            if (obj is InternalElementType)
            {
                if (IhNeg.InternalElement.Exists && IhNeg.InternalElement.First.Name.Equals("PlaceHolder"))
                {
                    MessageBox.Show("can not mixing EI and IE in one list!");
                }
                else
                {
                    IhNeg.InternalElement.Insert((InternalElementType)obj);
                    Negatives.Add(obj);
                    //if (IhPos.InternalElement.Exists && contains(IhPos.InternalElement, (InternalElementType)obj))
                    if (contains(Positives, obj))
                    {
                        remove(IhPos.InternalElement, (InternalElementType)obj);
                        Positives.Remove(obj);
                    }
                        
                }
            }

            if (obj is ExternalInterfaceType)
            {
                if (!IhNeg.InternalElement.Exists)
                {
                    IhNeg.InternalElement.Append("PlaceHolder");
                }
                else if (IhNeg.InternalElement.First.Name.Equals("PlaceHolder"))
                {
                    IhNeg.InternalElement.First.ExternalInterface.Insert((ExternalInterfaceType)obj);
                }
                else
                {
                    MessageBox.Show("can not mixing EI and IE in one list!");
                }
            }

            updateTreeViewModel();
        }

        private void remove(InternalElementSequence ieSeq, InternalElementType ie)
        {
            foreach (InternalElementType ieInSeq in ieSeq)
            {
                if (equals(ieInSeq, ie))
                    ieSeq.RemoveElement(ieInSeq);
            }
        }

        public bool containsPositiveExample(CAEXObject obj)
        {
            if (obj is InternalElementType)
            {
                return contains(IhPos.InternalElement, (InternalElementType)obj) && contains(Positives, obj);
            }

            if (obj is ExternalInterfaceType)
            {
                if (IhPos.InternalElement.First.Name.Equals("PlaceHolder"))
                {
                    return contains(IhPos.InternalElement.First.ExternalInterface, (ExternalInterfaceType)obj);             
                }
            }

            return false;            
        }

        public bool containsNegativeExample(CAEXObject obj)
        {
            if (obj is InternalElementType)
            {
                return contains(IhNeg.InternalElement, (InternalElementType)obj) && contains(Negatives,obj);
            }

            if (obj is ExternalInterfaceType)
            {
                if (IhNeg.InternalElement.First.Name.Equals("PlaceHolder"))
                {
                    return contains(IhNeg.InternalElement.First.ExternalInterface, (ExternalInterfaceType)obj);
                }
            }

            return false;
        }

        private bool contains(List<CAEXObject> seq, CAEXObject obj)
        {
            foreach (CAEXObject o in seq)
            {
                if (equals(o, obj))
                    return true;
            }
            return false;
        }

        private bool contains(CAEXSequence<CAEXObject> seq, CAEXObject obj)
        {
            foreach (CAEXObject o in seq)
            {
                if (equals(o, obj))
                    return true;
            }
            return false;
        }

        private bool contains(ExternalInterfaceSequence eiSeq, ExternalInterfaceType ei)
        {
            foreach (ExternalInterfaceType eiInSeq in eiSeq)
            {
                if (equals(eiInSeq, ei))
                    return true;
            }
            return false;
        }

        private bool contains(InternalElementSequence ieSeq, InternalElementType ie)
        {
            foreach (InternalElementType ieInSeq in ieSeq)
            {
                if (equals(ieInSeq, ie))
                    return true;
            }
            return false;
        }

        private bool equals(CAEXObject obj1, CAEXObject obj2)
        {
            return obj1.ID.Equals(obj2.ID);
        }

        private bool equals(InternalElementType ie1, InternalElementType ie2)
        {
            return ie1.ID.Equals(ie2.ID);
        }

        private bool equals(ExternalInterfaceType ei1, ExternalInterfaceType ei2)
        {
            return ei1.ID.Equals(ei2.ID);
        }


        private void updateTreeViewModel()
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
