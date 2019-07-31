﻿using Aml.Engine.CAEX;
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

        //private InstanceHierarchyType IhPos;
        //private InstanceHierarchyType IhNeg;

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

            DocumentPos.CAEXFile.InstanceHierarchy.Append("positives");
            DocumentNeg.CAEXFile.InstanceHierarchy.Append("negatives");
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
            InstanceHierarchyType ihPos = DocumentPos.CAEXFile.InstanceHierarchy.First;
            InstanceHierarchyType ihNeg = DocumentNeg.CAEXFile.InstanceHierarchy.First;
            if (obj is InternalElementType)
            {
                if (ihPos.InternalElement.Exists && ihPos.InternalElement.First.Name.Equals("PlaceHolder"))
                {
                    MessageBox.Show("can not mixing EI and IE in one list!");
                }
                else
                {
                    ihPos.InternalElement.Insert((InternalElementType)obj);                    
                    Positives.Add(obj);
                    removeNegative(obj);                        
                }                    
            }                

            if (obj is ExternalInterfaceType)
            {
                if (!ihPos.InternalElement.Exists)
                {
                    ihPos.InternalElement.Append("PlaceHolder");
                }
                else if (ihPos.InternalElement.First.Name.Equals("PlaceHolder"))
                {
                    ihPos.InternalElement.First.ExternalInterface.Insert((ExternalInterfaceType)obj);
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
            InstanceHierarchyType ihPos = DocumentPos.CAEXFile.InstanceHierarchy.First;
            InstanceHierarchyType ihNeg = DocumentNeg.CAEXFile.InstanceHierarchy.First;
            if (obj is InternalElementType)
            {
                if (ihNeg.InternalElement.Exists && ihNeg.InternalElement.First.Name.Equals("PlaceHolder"))
                {
                    MessageBox.Show("can not mixing EI and IE in one list!");
                }
                else
                {
                    ihNeg.InternalElement.Insert((InternalElementType)obj);
                    Negatives.Add(obj);                    
                    removePositive(obj);
                }
            }

            if (obj is ExternalInterfaceType)
            {
                if (!ihNeg.InternalElement.Exists)
                {
                    ihNeg.InternalElement.Append("PlaceHolder");
                }
                else if (ihNeg.InternalElement.First.Name.Equals("PlaceHolder"))
                {
                    ihNeg.InternalElement.First.ExternalInterface.Insert((ExternalInterfaceType)obj);
                }
                else
                {
                    MessageBox.Show("can not mixing EI and IE in one list!");
                }
            }

            updateTreeViewModel();
        }

        private void removePositive(CAEXObject obj)
        {
            var caexObj = DocumentPos.FindByID(obj.ID);
            if (!(caexObj is null))
            {
                Positives.Remove(obj);
                caexObj.Remove();
            }                
        }

        private void removeNegative(CAEXObject obj)
        {
            var caexObj = DocumentNeg.FindByID(obj.ID);
            if (!(caexObj is null))
            {
                Negatives.Remove(obj);
                caexObj.Remove();
            }
        }

        public bool containsPositiveExample(CAEXObject obj)
        {
            var caexObj = DocumentPos.FindByID(obj.ID);
            return (caexObj == null) ? false : true;  
        }

        public bool containsNegativeExample(CAEXObject obj)
        {
            var caexObj = DocumentNeg.FindByID(obj.ID);
            return (caexObj == null) ? false : true;
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
