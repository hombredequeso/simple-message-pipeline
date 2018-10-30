using Newtonsoft.Json.Linq;

namespace SimpleMessagePipelineTests.TestEntities
{
    public class TransportMessage
    {
        public JObject Metadata { get; set; }
        public string Message { get; set; }    // typically event or command
    }
}