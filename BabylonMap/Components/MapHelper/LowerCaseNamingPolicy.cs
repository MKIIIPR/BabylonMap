using System.Text.Json;

namespace AOCPwa.MapHelper
{
    public class LowerCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return name.ToLowerInvariant();
        }
    }

}
