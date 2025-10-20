using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Obfuscator
{
    


    public class Spec
    {
        [JsonPropertyName("type")]
        public TokenType Type { get; set; }

        [JsonPropertyName("pattern")]
        public string? Pattern { get; set; }

        [JsonPropertyName("precedence")]
        public int Precedence { get; set; }

    }
}
