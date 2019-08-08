using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aml.Editor.PlugIn.TestPlugin.json
{
    class AMLLearnerProtocolRequestStart
    {
        [JsonProperty("request")]
        public String Request { get; set; }

        [JsonProperty("path")]
        public String Path { get; set; }

        [JsonProperty("numResults")]
        public int NumResults { get; set; }

        public AMLLearnerProtocolRequestStart()
        {
            this.Request = "start";
        }
    }

    class AMLLearnerProtocolRequestStop
    {
        [JsonProperty("request")]
        public String Request { get; set; }

        public AMLLearnerProtocolRequestStop()
        {
            this.Request = "stop";
        }
    }

    class AMLLearnerProtocolRequestLoad
    {
        [JsonProperty("request")]
        public String Request { get; set; }

        public AMLLearnerProtocolRequestLoad()
        {
            this.Request = "load";
        }
    }

    class AMLLearnerProtocolResult
    {
        [JsonProperty("concept")]
        public String Concept { get; set; }

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
        public static String MakeStartRequest(String path, int numResults) {
            AMLLearnerProtocolRequestStart start = new AMLLearnerProtocolRequestStart
            {
                Path = path,
                NumResults = numResults
            };

            return Newtonsoft.Json.JsonConvert.SerializeObject(start);
        }

        public static String MakeStopRequest()
        {
            AMLLearnerProtocolRequestStop stop = new AMLLearnerProtocolRequestStop();
            return Newtonsoft.Json.JsonConvert.SerializeObject(stop);
        }

        public static String MakeLoadRequest()
        {
            AMLLearnerProtocolRequestLoad load = new AMLLearnerProtocolRequestLoad();
            return Newtonsoft.Json.JsonConvert.SerializeObject(load);
        }

        public static AMLLearnerProtocolResults GetResults(String jsonStr)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<AMLLearnerProtocolResults>(jsonStr);
        }
    }
}
