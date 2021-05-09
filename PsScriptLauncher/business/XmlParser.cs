using AryxDevLibrary.utils.xml;
using PsScriptLauncher.dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using AryxDevLibrary.utils;

namespace PsScriptLauncher.business
{
    internal class XmlParser
    {
        private XmlFile xmlFile;

        public XmlParser(XmlFile xmlFile)
        {
            this.xmlFile = xmlFile;
        }

        internal List<ScriptDto> GetListScripts()
        {
            List<ScriptDto> retList = new List<ScriptDto>();

            XmlNodeList list = XmlUtils.GetElementsXpath(xmlFile.Root, ".//script");
            foreach (XmlElement xmlElement in list)
            {
                if (!xmlElement.HasAttribute("id"))
                {
                    Console.WriteLine("Balise script detectée sans attribut id. Ignorée.");
                    continue;
                }

                ScriptDto s = new ScriptDto()
                {
                    Id = xmlElement.Attributes["id"].Value,
                    Path = XmlUtils.GetElementXpath(xmlElement, "./path/text()")?.Value,
                    Wd = XmlUtils.GetElementXpath(xmlElement, "./wd/text()")?.Value,
                    Args = XmlUtils.GetElementXpath(xmlElement, "./args/text()")?.Value,

                };


                if (retList.Any(r => r.Id.Equals(s.Id)))
                {
                    Console.WriteLine("Balise script detectée sans attribut id. Ignorée.");
                    continue;
                }

                XmlNode psParams = XmlUtils.GetElementXpath(xmlElement, "./powershellParams");
                if (psParams != null)
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(PowershellParams));
                    PowershellParams p = null;
                    using (XmlNodeReader reader = new XmlNodeReader(psParams))
                    {
                        p = (PowershellParams) serializer.Deserialize(reader);
                    }

                    s.PowershellParams = p;
                }



                retList.Add(s);


            }

            return retList;

        }
    }
}
