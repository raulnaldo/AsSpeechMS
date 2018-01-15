// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using QuickType;
//
//    var data = ParseFromLuis.FromJson(jsonString);
//
namespace AsAsrMicrosoft
{
    using System;
    using System.Net;
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public partial class ParseFromLuis
    {
        [JsonProperty("entities")]
        public List<Entity> Entities { get; set; }

        [JsonProperty("intents")]
        public List<Ntent> Intents { get; set; }

        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("topScoringIntent")]
        public Ntent TopScoringIntent { get; set; }
    }

    public partial class Ntent
    {
        [JsonProperty("intent")]
        public string Intent { get; set; }

        [JsonProperty("score")]
        public decimal Score { get; set; }
    }

    public partial class Entity
    {
        [JsonProperty("endIndex")]
        public int EndIndex { get; set; }

        [JsonProperty("entity")]
        public string PurpleEntity { get; set; }

        [JsonProperty("score")]
        public decimal Score { get; set; }

        [JsonProperty("startIndex")]
        public int StartIndex { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public partial class ParseFromLuis
    {
        public static ParseFromLuis FromJson(string json)
        {
            return JsonConvert.DeserializeObject<ParseFromLuis>(json, Converter.Settings);
        }
    }

    public static class Serialize
    {
        public static string ToJson(this ParseFromLuis self)
        {
            return JsonConvert.SerializeObject(self, Converter.Settings);
        }
    }

    public class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
        };
    }
}

