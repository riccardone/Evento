using System;
using System.Collections.Generic;
using System.Text;
using EventStore.ClientAPI;
using Newtonsoft.Json;

namespace EventStore.Tools.Infrastructure
{
    public class SerializationUtils
    {

        public static string EventClrTypeHeader = "EventClrTypeName";

        public static T DeserializeObject<T>(byte[] data)
        {
            return (T)(DeserializeObject(data, typeof(T).AssemblyQualifiedName));
        }

        public static object DeserializeObject(byte[] data, string typeName)
        {
            try
            {
                var jsonString = Encoding.UTF8.GetString(data);
                return JsonConvert.DeserializeObject(jsonString, Type.GetType(typeName));
            }
            catch (Exception ex)
            {

                return null;
            }
        }

        public static object DeserializeEvent(RecordedEvent originalEvent)
        {
            if (originalEvent.Metadata != null && !originalEvent.EventStreamId.StartsWith("$"))
            {
                var metadata = DeserializeObject<Dictionary<string, dynamic>>(originalEvent.Metadata);
                if (metadata != null && metadata.ContainsKey(EventClrTypeHeader))
                {
                    var eventData = DeserializeObject(originalEvent.Data, metadata[EventClrTypeHeader]);
                    return eventData;
                }
                else
                {
                    var eventData = DeserializeObject(originalEvent.Data, EventClrTypeHeader);
                    return eventData;
                }
            }
            return null;
        }
    }
}
