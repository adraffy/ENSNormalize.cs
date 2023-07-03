using Newtonsoft.Json;

namespace EnsNormalize.Tests
{
    public class NormTestCase
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("error")]
        public bool? Error { get; set; }

        [JsonProperty("comment")]
        public string Comment { get; set; }

        [JsonProperty("norm")]
        public string Norm { get; set; }

        public object[] ToObjectArray()
        {
            var error = Error == null ? false : Error.Value;
            return new object[]{ Name, error, Norm, Comment};
        }
    }
}