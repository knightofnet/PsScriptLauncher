using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using PsScriptLauncher.constant;

namespace PsScriptLauncher.dto
{

    [Serializable()]
    [XmlRoot("powershellParams")]
    public class PowershellParams
    {
        [XmlElement("noExit")]
        public bool NoExit { get; set; }

        [XmlElement("noProfile")]
        public bool NoProfile { get; set; }

        [XmlElement("noLogo")]
        public bool NoLogo { get; set; }

        [XmlElement("outputToFile")]
        public string OutputToFile { get; set; }

        [XmlElement("windowStyle")]
        public string WindowStyle { get; set; }

        [XmlElement("waitSeconds")]
        public int WaitSeconds { get; set; }

        [XmlElement("withAdmin")]
        public bool WithAdmin { get; set; }

        public EnumTypeOutput OutputToFileEnum
        {
            get
            {
                if (String.IsNullOrWhiteSpace(OutputToFile)) return EnumTypeOutput.No;
                if (OutputToFile.ToUpper().Equals("NO")) return EnumTypeOutput.No;
                if (OutputToFile.ToUpper().Equals("YES")) return EnumTypeOutput.Yes;
                if (OutputToFile.ToUpper().Equals("NOIFNOTHING")) return EnumTypeOutput.NoIfNothing;
                return EnumTypeOutput.No;
            }
        }

        public PowershellParams()
        {
            NoExit = false;
            NoProfile = true;
            NoLogo = true;
            OutputToFile = "NO";
            WindowStyle = "Normal";
            WithAdmin = false;
        }


        public override string ToString()
        {
            return $"noExit: {NoExit}, noProfile: {NoProfile}, noLogo: {NoLogo}, windowStyle: {WindowStyle}, withAdmin: {WithAdmin}, waitSeconds: {WaitSeconds}, outputToFile: {OutputToFile}";
        }
    }
}
