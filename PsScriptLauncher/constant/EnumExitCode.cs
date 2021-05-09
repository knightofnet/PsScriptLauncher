using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PsScriptLauncher.constant
{
    internal enum EnumExitCode
    {

        Ok = 0,
        ShowSyntax = 2,


        NoScriptOrNotKnown = 50,

        NotEnoughtArgs = 51,
        ErrorWhenRunningScript = 52,
        ErrorScriptRunner = 53,
        ErrorsPowershellParams
    }
}
