using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aml.Editor.PlugIn.TestPlugin.json
{

    class AMLLearnerOperatorConfig
    {
        [JsonProperty("type")]
        public String Type { get; set; }

        [JsonProperty("useNegation")]
        public bool UseNegation { get; set; }

        [JsonProperty("useAllConstructor")]
        public bool UseAll { get; set; }

        [JsonProperty("useCardinalityRestriction")]
        public bool UseCardinality { get; set; }

        [JsonProperty("useDataHasValueConstructor")]
        public bool UseDataHasValue { get; set; }

        [JsonProperty("useNumericcDatatypes")]
        public bool UseNumeric { get; set; }

        public AMLLearnerOperatorConfig()
        {
            this.Type = "aml";
            this.UseNegation = false;
            this.UseAll = false;
            this.UseCardinality = true;
            this.UseDataHasValue = true;
            this.UseNumeric = true;
        }
    }

    class AMLLearnerHeuristicConfig
    {
        [JsonProperty("type")]
        public String Type { get; set; }

        [JsonProperty("expansionPenaltyFactor")]
        public double ExpansionPenaltyFactor { get; set; }

        [JsonProperty("nodeRefinementPenalty")]
        public double NodeRefinementPenalty { get; set; }

        [JsonProperty("startNodeBonus")]
        public double StartNodeBonus { get; set; }

        [JsonProperty("gainBonusFactor")]
        public double GainBonusFactor { get; set; }

        public AMLLearnerHeuristicConfig()
        {
            this.Type = "celoe_heuristic_lw";
            this.ExpansionPenaltyFactor = 0.02;
            this.NodeRefinementPenalty = 0;
            this.StartNodeBonus = 0;
            this.GainBonusFactor = 0.2;
        }
    }


    class AMLLearnerTreeConfig
    {
        [JsonProperty("write")]
        public bool Write { get; set; }

        [JsonProperty("file")]
        public String File { get; set; }

        public AMLLearnerTreeConfig()
        {
            this.Write = false;
            this.File = "D:/repositories/aml/aml_framework/src/test/resources/tree";
        }
    }

    class AMLLearnerACMConfig
    {
        [JsonProperty("file")]
        public String File { get; set; }

        [JsonProperty("id")]
        public String Id { get; set; }

        public AMLLearnerACMConfig()
        {
            //this.File = "D:/repositories/aml/aml_framework/src/test/resources/acm.aml";
        }
    }

    class AMLLearnerAlgConfig
    {
        [JsonProperty("type")]
        public String Type { get; set; }

        [JsonProperty("time")]
        public int Time { get; set; }

        [JsonProperty("size")]
        public int Size { get; set; }

        [JsonProperty("tree")]
        public AMLLearnerTreeConfig Tree { get; set; }

        public AMLLearnerACMConfig Acm { get; set; }

        public AMLLearnerAlgConfig()
        {
            this.Type = "rrhc";
            this.Time = 10;
            this.Size = 10;
            this.Tree = new AMLLearnerTreeConfig();
        }

        public AMLLearnerAlgConfig(AMLLearnerACMConfig acm)
        {
            this.Type = "rrhc";
            this.Time = 10;
            this.Size = 10;
            this.Tree = new AMLLearnerTreeConfig();
            this.Acm = acm;
        }
    }

    class AMLLearnerExamplesConfig
    {
        [JsonProperty("positives")]
        public String[] Positives { get; set; }

        [JsonProperty("negatives")]
        public String[] Negatives { get; set; }

        public AMLLearnerExamplesConfig()
        {

        }

        public AMLLearnerExamplesConfig(String[] positives, String[] negatives)
        {
            this.Positives = positives;
            this.Negatives = negatives;
        }
    }

    class AMLLearnerConfig
    {
        [JsonProperty("aml")]
        public String Aml { get; set; }

        [JsonProperty("reasoner")]
        public String Reasoner { get; set; }

        [JsonProperty("type")]
        public String Type { get; set; }

        [JsonProperty("op")]
        public AMLLearnerOperatorConfig Operator { get; set; }

        [JsonProperty("heu")]
        public AMLLearnerHeuristicConfig Heuristic { get; set; }

        [JsonProperty("alg")]
        public AMLLearnerAlgConfig Algorithm { get; set; }

        [JsonProperty("examples")]
        public AMLLearnerExamplesConfig Examples { get; set; }

        public AMLLearnerConfig(String aml, String type, AMLLearnerExamplesConfig examples)
        {
            this.Aml = aml;
            this.Reasoner = "closed world reasoner";
            this.Type = type;
            this.Operator = new AMLLearnerOperatorConfig();
            this.Heuristic = new AMLLearnerHeuristicConfig();
            this.Algorithm = new AMLLearnerAlgConfig();
            this.Examples = examples;
        }

    }
}
