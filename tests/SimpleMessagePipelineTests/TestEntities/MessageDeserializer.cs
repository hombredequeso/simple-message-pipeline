using System;
using System.Collections.Generic;
using LanguageExt;
using Newtonsoft.Json;

namespace SimpleMessagePipelineTests.TestEntities
{
    public static class MessageDeserializer
    {
        public static Option<object> Deserialize(
            IDictionary<string, Type> typeLookup,
            string msgType,
            string serializedMessage)
        {
            return 
                typeLookup.TryGetValue(msgType)
                    .Map(typ =>
                        JsonConvert.DeserializeObject(serializedMessage, typ));
        }
    }
}