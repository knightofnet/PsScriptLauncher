using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PsScriptLauncher.dto
{
    internal class ScriptDto
    {
        public String Id { get; set; }
        public string Path { get; internal set; }
        public string Wd { get; internal set; }
        public string Args { get; internal set; }
        public PowershellParams PowershellParams { get; set; }

        public ScriptDto()
        {
            PowershellParams = new PowershellParams();

        }
    }
}
