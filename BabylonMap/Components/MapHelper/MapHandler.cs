using Microsoft.JSInterop;
using System.Threading.Tasks;

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
   
}
