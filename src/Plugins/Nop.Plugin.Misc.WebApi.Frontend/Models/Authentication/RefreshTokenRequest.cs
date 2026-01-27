using Newtonsoft.Json;

namespace Nop.Plugin.Misc.WebApi.Frontend.Models.Authentication
{
    public class RefreshTokenRequest
    {
        [JsonProperty("refresh_token", Required = Required.Always)]
        public string RefreshToken { get; set; }
    }
}
