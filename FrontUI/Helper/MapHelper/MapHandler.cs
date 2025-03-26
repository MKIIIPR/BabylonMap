using FrontUI.AppStates;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace FrontUI.Helper.MapHelper
{
    public class MapHandler
    {

        public static double Lat { get; set; }
        public static double Lng { get; set; }
     
        [JSInvokable]
        public static Task UpdateCoordinates(double lat, double lng)
        {
            Lat = lat;
            Lng = lng;
            Console.WriteLine($"Neue Koordinaten: {lat}, {lng}");
            return Task.CompletedTask;
        }
        public static string SelectedPositionId { get; set; }

        // Event, das ausgelöst wird, wenn sich die ID ändert
        public static event Action<string> OnNodeIdChanged;

        // Statische Methode, die aufgerufen wird, wenn sich die NodeId ändert
        [JSInvokable]
        public static Task ReceiveNodeData(string nodeId)
        {
            // Update der ID
            SelectedPositionId = nodeId;

            // Event auslösen, dass sich die ID geändert hat
            OnNodeIdChanged?.Invoke(nodeId);

            return Task.CompletedTask;
        }

    }
}