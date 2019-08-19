using Aml.Engine.CAEX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aml.Editor.PlugIn.AMLLearner
{
    public class CaexToAcm
    {
        public static void setAcmConfigToCaexAttribute(AttributeType attr, AMLConceptConfig config)
        {
            attr.Name = AMLConceptConfig.CONFIG;

            AttributeType distinguished = attr.Attribute.Append();
            distinguished.Name = AMLConceptConfig.CONFIG_DISTINGUISHED;
            distinguished.AttributeDataType = "xs:boolean";
            distinguished.Value = config.IsDistinguished.ToString();

            AttributeType id = attr.Attribute.Append();
            id.Name = AMLConceptConfig.CONFIG_ID;
            id.AttributeDataType = "xs:boolean";
            id.Value = config.IsIdentifiedById.ToString();

            AttributeType name = attr.Attribute.Append();
            name.Name = AMLConceptConfig.CONFIG_NAME;
            name.AttributeDataType = "xs:boolean";
            name.Value = config.IsIdentifiedByName.ToString();

            AttributeType min = attr.Attribute.Append();
            min.Name = AMLConceptConfig.CONFIG_MIN;
            min.AttributeDataType = "xs:integer";
            min.Value = config.MinCardinality.ToString();

            AttributeType max = attr.Attribute.Append();
            max.Name = AMLConceptConfig.CONFIG_MAX;
            max.AttributeDataType = "xs:integer";
            max.Value = config.MaxCardinality.ToString();

            AttributeType negated = attr.Attribute.Append();
            negated.Name = AMLConceptConfig.CONFIG_NEGATED;
            negated.AttributeDataType = "xs:boolean";
            negated.Value = config.IsNegated.ToString();

            AttributeType descendant = attr.Attribute.Append();
            descendant.Name = AMLConceptConfig.CONFIG_DESCENDANT;
            descendant.AttributeDataType = "xs:boolean";
            descendant.Value = config.IsDescendant.ToString();
        }


        public static AMLConceptConfig toAcmConfig(AttributeType attr)
        {
            if (IsConfigAttribute(attr))
            {
                AMLConceptConfig config = new AMLConceptConfig();

                foreach (AttributeType child in attr.Attribute)
                {
                    if (child.Name.Equals(AMLConceptConfig.CONFIG_DISTINGUISHED))
                    {
                        config.IsDistinguished = bool.Parse(child.Value);
                    }

                    else if (child.Name.Equals(AMLConceptConfig.CONFIG_DESCENDANT))
                    {
                        config.IsDescendant = bool.Parse(child.Value);
                    }

                    else if(child.Name.Equals(AMLConceptConfig.CONFIG_ID))
                    {
                        config.IsIdentifiedById = bool.Parse(child.Value);
                    }

                    else if(child.Name.Equals(AMLConceptConfig.CONFIG_NAME))
                    {
                        config.IsIdentifiedByName = bool.Parse(child.Value);
                    }

                    else if(child.Name.Equals(AMLConceptConfig.CONFIG_MIN))
                    {
                        config.MinCardinality = int.Parse(child.Value);
                    }

                    else if(child.Name.Equals(AMLConceptConfig.CONFIG_MAX))
                    {
                        config.MaxCardinality = int.Parse(child.Value);
                    }

                    else if(child.Name.Equals(AMLConceptConfig.CONFIG_NEGATED))
                    {
                        config.IsNegated = bool.Parse(child.Value);
                    }
                }

                return config;
            }

            return null;
        }

        public static Boolean IsConfigAttribute(AttributeType attr)
        {
            return attr.Name.Equals("queryConfig");
        }

        public static Boolean HasAcmConfig(CAEXObject obj)
        {
            if (obj is InternalElementType)
            {
                foreach (AttributeType attr in ((InternalElementType)obj).Attribute)
                {
                    if (IsConfigAttribute(attr))
                    {
                        return true;
                    }
                }
            }

            else if (obj is ExternalInterfaceType)
            {
                foreach (AttributeType attr in ((ExternalInterfaceType)obj).Attribute)
                {
                    if (IsConfigAttribute(attr))
                    {
                        return true;
                    }
                }
            }

            else if (obj is AttributeType)
            {
                foreach (AttributeType attr in ((AttributeType)obj).Attribute)
                {
                    if (IsConfigAttribute(attr))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static AttributeType GetConfigAttribute(CAEXObject obj)
        {
            if (obj is InternalElementType)
            {
                InternalElementType ie = (InternalElementType)obj;
                foreach (AttributeType attr in ie.Attribute)
                {
                    if (IsConfigAttribute(attr))
                    {
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

        
        /// <summary>
        /// convert a caex attribute to an ACM obj:
        /// - append a standard ACM config to the caex attribute
        /// - for each nested caex attribtue, do the same
        /// </summary>
        /// <param name="obj"></param>
        public static void toAcm(ref AttributeType obj, bool distinguished)
        {             
            if (!HasAcmConfig(obj) && !obj.Name.Equals(AMLConceptConfig.CONFIG))
            {
                AMLConceptConfig attrConfig = new AMLConceptConfig();
                attrConfig.IsIdentifiedByName = true;
                attrConfig.IsDistinguished = distinguished;

                // append a standard ACM config to the attribute
                AttributeType acmAttr = obj.Attribute.Append();
                setAcmConfigToCaexAttribute(acmAttr, attrConfig);

                // for each attribute descendant, recursively append a standard ACM config
                for (int i = 0; i < obj.Attribute.Count; i++)
                {
                    AttributeType attrChild = obj.Attribute.At(i);
                    if(!attrChild.Name.Equals(AMLConceptConfig.CONFIG))
                        toAcm(ref attrChild, false); 
                }
            }
        }

        /// <summary>
        /// convert a caex external interface to an ACM obj
        /// - append a standard ACM config to the caex external interface
        /// - for each nested caex attribute, do the same
        /// </summary>
        /// <param name="obj"></param>
        public static void toAcm(ref ExternalInterfaceType obj, bool distinguished)
        {
            if (!HasAcmConfig(obj))
            {
                AMLConceptConfig eiConfig = new AMLConceptConfig();
                eiConfig.IsDistinguished = distinguished;
                AttributeType acmAttr = obj.Attribute.Append();
                setAcmConfigToCaexAttribute(acmAttr, eiConfig);

                for (int i = 0; i < obj.Attribute.Count; i++)
                {
                    AttributeType attrChild = obj.Attribute.At(i);
                    toAcm(ref attrChild, false);
                }
            }
        }

        /// <summary>
        /// convert a caex internal element to an ACM obj:
        /// - append a standard ACM config to the caex internal element
        /// - for each nested caex internal element, external interface and attribtue, do the same
        /// </summary>
        /// <param name="obj"></param>
        public static void toAcm(ref InternalElementType obj, bool distinguished)
        {
            if (!HasAcmConfig(obj))
            {
                AMLConceptConfig ieConfig = new AMLConceptConfig();
                ieConfig.IsDistinguished = distinguished;
                AttributeType acmAttr = obj.Attribute.Append();
                setAcmConfigToCaexAttribute(acmAttr, ieConfig);

                for (int i = 0; i < obj.Attribute.Count; i++)
                {                    
                    AttributeType attrChild = obj.Attribute.At(i);
                    toAcm(ref attrChild, false);
                }

                for (int i = 0; i < obj.ExternalInterface.Count; i++)
                {
                    ExternalInterfaceType eiChild = obj.ExternalInterface.At(i);
                    toAcm(ref eiChild, false);
                }

                for (int i = 0; i < obj.InternalElement.Count; i++)
                {
                    InternalElementType ieChild = obj.InternalElement.At(i);
                    toAcm(ref ieChild, false);
                }
            }
        }

    }
}
