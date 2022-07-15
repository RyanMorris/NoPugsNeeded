using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoPugsNeeded.Settings
{
    public class KeySettings
    {
        public string Key { get; set; }
        public bool Allow { get; set; }
        public bool AllowShift { get; set; }
        public bool AllowCtrl { get; set; }
        public bool AllowAlt { get; set; }
    }
}
