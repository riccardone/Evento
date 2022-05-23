using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Evento.Repository
{
    internal class SerializationUtils
    {

        public static string EventClrTypeHeader = "EventClrTypeName";

        public static T DeserializeObject<T>(ReadOnlyMemory<byte> data)
        {
            return (T)(DeserializeObject(data, typeof(T).AssemblyQualifiedName));
        }

        public static object DeserializeObject(ReadOnlyMemory<byte> data, string typeName)
        {
            try
            {
                var jsonString = Encoding.UTF8.GetString(data.Span);
                return JsonConvert.DeserializeObject(jsonString, Type.GetType(typeName));
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static object DeserializeObject(ReadOnlyMemory<byte> data, ReadOnlyMemory<byte> metadata)
        {
            try
            {
                var dict = DeserializeObject<Dictionary<string, string>>(metadata);
                if (!dict.ContainsKey("$correlationId"))
                    throw new Exception("The metadata must contains a $correlationId");
                var bodyString = Encoding.UTF8.GetString(data.Span);
                var o1 = JObject.Parse(bodyString);
                var o2 = JObject.Parse(JsonConvert.SerializeObject(new { metadata = dict }));
                o1.Merge(o2, new JsonMergeSettings {MergeArrayHandling = MergeArrayHandling.Union});
                return JsonConvert.DeserializeObject(o1.ToString(),
                    Type.GetType(DeserializeObject<Dictionary<string, string>>(metadata)[EventClrTypeHeader]));
            }
            catch (Exception)
            {
                return null;
            }
        }

        //public static object DeserializeEvent(RecordedEvent originalEvent)
        //{
        //    if (originalEvent.Metadata != null && !originalEvent.EventStreamId.StartsWith("$"))
        //    {
        //        var metadata = DeserializeObject<Dictionary<string, dynamic>>(originalEvent.Metadata);
        //        if (metadata != null && metadata.ContainsKey(EventClrTypeHeader))
        //        {
        //            var eventData = DeserializeObject(originalEvent.Data, metadata[EventClrTypeHeader]);
        //            return eventData;
        //        }
        //        else
        //        {
        //            var eventData = DeserializeObject(originalEvent.Data, EventClrTypeHeader);
        //            return eventData;
        //        }
        //    }
        //    return null;
        //}
    }
}
