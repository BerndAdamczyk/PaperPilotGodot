using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PaperPilot.Model
{
    public class PaperStack
    {
        public string Path = string.Empty;
        public string Name = string.Empty;
        public List<Paper> Papers { get; set; } = new();

        public int TotalPages => Papers.Count;
        public int KeptPages { get; set; }
        public int EmptyPages { get; set; }
        public int SplittingPointPages { get; set; }
    }
}

