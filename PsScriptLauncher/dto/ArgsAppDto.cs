using System.Collections.Generic;
using AryxDevLibrary.utils.xml;
using PsScriptLauncher.constant;

namespace PsScriptLauncher.dto
{
    internal class ArgsAppDto
    {
        public string ScriptId { get; set; }
        public List<string> ScriptArgsInput { get; set; }
        public XmlFile XmlFile { get; internal set; }
        public EnumModeLancement Mode { get; internal set; }
        public bool HaltOnError { get; set; }


        public ArgsAppDto()
        {
            ScriptArgsInput = new List<string>();
        }
        public override string ToString()
        {
            return $"{nameof(Mode)}: {Mode}, {nameof(ScriptId)}: {ScriptId}, {nameof(ScriptArgsInput)}: {ScriptArgsInput}, {nameof(XmlFile)}: {XmlFile}, {nameof(HaltOnError)}: {HaltOnError}";
        }
    }
}