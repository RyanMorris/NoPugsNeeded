using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoPugsNeeded.Controllers
{
    interface IControl
    {
        void Run();
        void Stop();
        void SuspendOrResume();
        bool IsEnabled();
        bool IsHooked();
        int GetExitReason();
        bool LoadScript(string filename, string pidStr);
        List<ScriptRunner> ListScripts();
        string GetFailedReason();
    }
}