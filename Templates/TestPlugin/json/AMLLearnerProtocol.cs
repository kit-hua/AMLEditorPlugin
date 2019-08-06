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
    }

    class AMLLearnerProtocol
    {
        public static String MakeStartRequest(String path, int numResults) {
            AMLLearnerProtocolRequestStart start = new AMLLearnerProtocolRequestStart
            {
                Request = "start",
                Path = path,
                NumResults = numResults
            };

            return Newtonsoft.Json.JsonConvert.SerializeObject(start);
        }
    }
}
