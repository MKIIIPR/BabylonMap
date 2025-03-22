using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AshesMapBib.Models
{
    public class FileBase<T>
    {
        
        public string Name { get; set; }
        public List<T> FileContent { get; set; }
        public DateTime LastUpdated { get; set; }
        public string ItemType { get; set; }
        public string Version { get; set; }
    }
}
