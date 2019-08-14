using Aml.Engine.CAEX;
using Aml.Engine.CAEX.Extensions;
using Aml.Toolkit.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Aml.Editor.PlugIn.AMLLearner
{    
    public class AMLLearnerInstanceHierarchy
    {
        public InstanceHierarchyType Ih { get; set; }
        public InternalElementType Placeholder { get; private set; }
        public Boolean IsPlaceHolderAttached { get; private set; }        

        public AMLLearnerInstanceHierarchy(InstanceHierarchyType ih)
        {
            Ih = ih;
        }

        public void AddObject(CAEXObject obj)
        {
            if (obj is InternalElementType)
            {
                Ih.InternalElement.Insert((InternalElementType)obj);
            }

            if (obj is ExternalInterfaceType)
            {
                // if the Ih does not have any IE: place holder does not exist neither
                if (Placeholder == null)
                {
                    Placeholder = Ih.InternalElement.Append("PlaceHolder");
                }

                // if placeholder exists but not attached
                else if (Placeholder != null && !IsPlaceHolderAttached)
                {
                    Ih.InternalElement.Insert(Placeholder);
                }

                IsPlaceHolderAttached = true;
                Placeholder.ExternalInterface.Insert((ExternalInterfaceType)obj);
            }
        }

        public void RemovePlaceHolder()
        {
            if (Placeholder != null && IsPlaceHolderAttached)
            {
                MessageBox.Show("removing all EIs from the place holder object!");
                foreach (ExternalInterfaceType ei in Placeholder.ExternalInterface)
                {
                    RemoveObj(ei);
                }
                Placeholder.Remove();
                IsPlaceHolderAttached = false;
            }            
        }

        public void RemoveObj(CAEXObject obj)
        {
            if (IsSameObj(obj, Placeholder))
            {
                RemovePlaceHolder();
            }

            else if (obj is InternalElementType)
            {
                foreach (CAEXObject ele in Ih.InternalElement.ToList())
                {
                    if (IsSameObj(obj, ele))
                    {                        
                        ele.Remove();
                    }
                }
            }

            else if (obj is ExternalInterfaceType)
            {
                if (Ih.InternalElement.Exists && Placeholder != null)
                {
                    foreach (CAEXObject ele in Placeholder.ExternalInterface.ToList())
                    {
                        if (IsSameObj(obj, ele))
                        {
                            ele.Remove();
                        }
                    }
                }
            }
        }

        public bool ContainsObj(CAEXObject obj)
        {
            if (obj is InternalElementType)
            {
                foreach (CAEXObject ele in Ih.InternalElement)
                {
                    if (IsSameObj(obj, ele))
                        return true;
                }                
            }

            else if (obj is ExternalInterfaceType)
            {
                if (Placeholder != null && IsPlaceHolderAttached)
                {
                    foreach (CAEXObject ele in Placeholder.ExternalInterface)
                    {
                        if (IsSameObj(obj, ele))
                            return true;
                    }
                }
            }
            return false;
        }

        public bool IsSameObj(CAEXObject one, CAEXObject other)
        {
            if (one == null || other == null)
                return false;

            return one.ID.Equals(other.ID);
        }

        public void Clear()
        {
            //Ih.Remove();
            Ih.InternalElement.Remove();
            if (Placeholder != null)
            {
                Placeholder.ExternalInterface.Remove();
                Placeholder = null;
            }            
            IsPlaceHolderAttached = false;
        }

        public List<CAEXObject> GetAllObjects()
        {
            List<CAEXObject> all = new List<CAEXObject>();
            foreach (InternalElementType ie in Ih.InternalElement)
            {
                if (!ie.Equals(Placeholder))
                {
                    all.Add(ie);
                } 
                else
                {
                    foreach (ExternalInterfaceType ei in ie.ExternalInterface)
                    {
                        all.Add(ei);
                    }
                }
            }

            return all;
        }

    }

    public class AMLLearnerTree
    {        
        //public AMLTreeViewModel ViewModel { get; set; }
        public CAEXDocument Document { get; set; }
        public List<AMLLearnerInstanceHierarchy> Ihs { get; set; }

        public AMLLearnerTree()
        {
            Document = CAEXDocument.New_CAEXDocument();
            Ihs = new List<AMLLearnerInstanceHierarchy>();
        }

        public AMLLearnerTree(CAEXDocument doc)
        {
            Document = doc;
            Ihs = new List<AMLLearnerInstanceHierarchy>();
            foreach (InstanceHierarchyType ih in doc.CAEXFile.InstanceHierarchy)
            {
                Ihs.Add(new AMLLearnerInstanceHierarchy(ih));
            }
            //Update();
        }

        public InstanceHierarchyType AddInstanceHiearchy(String name)
        {
            InstanceHierarchyType ih = Document.CAEXFile.InstanceHierarchy.Append(name);
            Ihs.Add(new AMLLearnerInstanceHierarchy(ih));
            //Update();
            return ih;
        }

        public void AddObjectToIh(InstanceHierarchyType ih, CAEXObject obj)
        {
            foreach (AMLLearnerInstanceHierarchy amlIh in Ihs)
            {
                if (amlIh.Ih.Equals(ih))
                {
                    amlIh.AddObject(obj);
                    //Update();
                }
            }
        }

        public void AddObjectToIh(AMLLearnerInstanceHierarchy ih, CAEXObject obj)
        {
            if (Ihs.Contains(ih))
            {
                ih.AddObject(obj);
                //Update();
            }
            else 
                MessageBox.Show("The instance hierarchy is not contained in the tree!");
        }

        public void RemoveObject(CAEXObject obj)
        {
            foreach (AMLLearnerInstanceHierarchy ih in Ihs)
            {
                if (ih.ContainsObj(obj)) {
                    ih.RemoveObj(obj);
                }                    
            }
            //Update();
        }

        public void RemoveObjectFromIh(AMLLearnerInstanceHierarchy ih, CAEXObject obj)
        {
            if (Ihs.Contains(ih))
            {
                ih.RemoveObj(obj);
                //Update();
            }
            else 
                MessageBox.Show("The instance hierarchy is not contained in the tree!");
        }

        public void Clear()
        {
            foreach (AMLLearnerInstanceHierarchy ih in Ihs)
            {                
                ih.Clear();
            }
            //Update();
        }

        
        /// <summary>
        /// check if the given object exist in the tree as a data object for learning
        /// that is, an object that exists in the first tree level of the first instance hierarchy
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>
        /// </returns>
        public Boolean ContainsDataObject(CAEXObject obj)
        {
            return Ihs[0].ContainsObj(obj);
        }

        /// <summary>
        /// check if the given object exists in the tree as a caex basic object
        /// that is, an object that exists anywhere in the tree
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public Boolean ContainsObject(CAEXBasicObject obj)
        {
            if (obj is InstanceHierarchyType)
            {
                foreach (AMLLearnerInstanceHierarchy amlIh in Ihs)
                {
                    if (amlIh.Ih.Equals(obj))
                        return true;
                }
            }

            if (obj is CAEXObject)
            {
                return Document.FindByID(((CAEXObject)obj).ID) != null;
            }

            return false;
        }

        public Boolean HasPlaceHolder(CAEXObject obj)
        {
            foreach (AMLLearnerInstanceHierarchy ih in Ihs)
            {
                if (ih.Placeholder != null && ih.Placeholder.Equals(obj))
                    return true;
            }

            return false;
        }        

        //private void Update()
        //{            
        //    ViewModel = new AMLTreeViewModel(Document.CAEXFile.Node, AMLTreeViewTemplate.CompleteInstanceHierarchyTree);
        //    // expands the first level
        //    //ViewModel.Root.Children[0].IsExpanded = true;
        //    ViewModel.RefreshTree(true);
        //    ViewModel.RefreshNodeInformation(true);                        
        //}        

    }
    
}
