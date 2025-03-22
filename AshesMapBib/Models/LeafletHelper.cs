using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AshesMapBib.Models
{
   
        public class MapTileSrc
        {
            public string Name { get; set; }
            public string TilePath { get; set; }
            public List<TileSet> Tiles { get; set; }

        }
        public class TileSet
        {
            public int Z { get; set; }
            public int X { get; set; }
            public int Y { get; set; }

        }
  
}
