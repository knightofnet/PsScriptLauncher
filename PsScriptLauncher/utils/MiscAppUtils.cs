using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PsScriptLauncher.utils
{
    internal static class MiscAppUtils
    {

        public static int IntParse(String s, int dft = -1)
        {
            int oInt;
            return Int32.TryParse(s, out oInt) ? oInt : dft;
        }

    }
}
