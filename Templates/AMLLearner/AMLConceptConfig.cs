using Aml.Engine.CAEX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aml.Editor.PlugIn.AMLLearner
{
    public class AMLConceptConfig
    {
        public static readonly string CONFIG = "queryConfig";
        public static readonly string CONFIG_DISTINGUISHED = "distinguished"; 	 
	    public static readonly string CONFIG_ID = "identifiedById";	
	    public static readonly string CONFIG_NAME = "identifiedByName"; 	
	    public static readonly string CONFIG_MIN = "minCardinality";
	    public static readonly string CONFIG_MAX = "maxCardinality";
        public static readonly string CONFIG_NEGATED = "negated";
	    public static readonly string CONFIG_DESCENDANT = "descendant";


        public Boolean IsDistinguished { get; set; } = false;
        public Boolean IsIdentifiedById { get; set; } = false;
        public Boolean IsIdentifiedByName { get; set; } = false;
        public Boolean IsNegated { get; set; } = false;
        public Boolean IsDescendant { get; set; } = false;

        public int MinCardinality { get; set; } = 1;
        public int MaxCardinality { get; set; } = -1;            
       
    }
}
