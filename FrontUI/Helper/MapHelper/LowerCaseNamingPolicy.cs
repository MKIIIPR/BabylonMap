using System.Text.Json;

namespace FrontUI.Helper.MapHelper
{
    public class LowerCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return name.ToLowerInvariant();
        }
    }

}
