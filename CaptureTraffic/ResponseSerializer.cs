using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CaptureTraffic
{
    public partial class ResponseSerializer
    {
        [JsonProperty("model_id")]
        public string ModelId { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("results")]
        public Result[] Results { get; set; }

        [JsonProperty("system")]
        public SystemClass System { get; set; }
    }

    public partial class Result
    {
        [JsonProperty("generated_text")]
        public string GeneratedText { get; set; }

        [JsonProperty("generated_token_count")]
        public long GeneratedTokenCount { get; set; }

        [JsonProperty("input_token_count")]
        public long InputTokenCount { get; set; }

        [JsonProperty("stop_reason")]
        public string StopReason { get; set; }
    }

    public partial class SystemClass
    {
        [JsonProperty("warnings")]
        public Warning[] Warnings { get; set; }
    }

    public partial class Warning
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public partial class ResponseSerializer
    {
        public static ResponseSerializer FromJson(string json) => JsonConvert.DeserializeObject<ResponseSerializer>(json, Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this ResponseSerializer self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}


