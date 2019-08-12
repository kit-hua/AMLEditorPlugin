using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aml.Editor.PlugIn.AMLLearner.json
{
    class AMLLearnerProtocolRequestStart
    {
        [JsonProperty("type")]
        public String Type { get; set; }

        [JsonProperty("numResults")]
        public int NumResults { get; set; }

        [JsonProperty("config")]
        public AMLLearnerConfig Config { get; set; }

        public AMLLearnerProtocolRequestStart()
        {
            this.Type = "start";
        }
    }

    class AMLLearnerProtocolRequestStop
    {
        [JsonProperty("type")]
        public String Type { get; set; }

        public AMLLearnerProtocolRequestStop()
        {
            this.Type = "stop";
        }
    }

    class AMLLearnerProtocolResult
    {
        [JsonProperty("concept")]
        public String Concept { get; set; }

        [JsonProperty("idx")]
        public String Index { get; set; }

        [JsonProperty("negatives")]
        public String[] Negatives { get; set; }

        [JsonProperty("positives")]
        public String[] Positives { get; set; }
    }

    class AMLLearnerProtocolResults
    {
        [JsonProperty("results")]
        public AMLLearnerProtocolResult[] Data { get; set; }
    }

    class AMLLearnerProtocol
    {

        public static String MakeStartRequest(AMLLearnerConfig config, int numResults)
        {
            AMLLearnerProtocolRequestStart start = new AMLLearnerProtocolRequestStart
            {
                Config = config,
                NumResults = numResults
            };

            return Newtonsoft.Json.JsonConvert.SerializeObject(start);
        }

        public static String MakeStopRequest()
        {
            AMLLearnerProtocolRequestStop stop = new AMLLearnerProtocolRequestStop();
            return Newtonsoft.Json.JsonConvert.SerializeObject(stop);
        }

        public static AMLLearnerProtocolResults GetResults(String jsonStr)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<AMLLearnerProtocolResults>(jsonStr);
        }
    }
}
