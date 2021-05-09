using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using PsScriptLauncher.dto;

namespace PsScriptLauncher.utils
{
    public class AppReflexionUtils
    {
        public static string GetXmlName(string propName)
        {
            PropertyInfo p = typeof(PowershellParams).GetProperty(propName);
            if (p == null)
            {
                throw new Exception("La propriété "+ propName + " n'existe pas.");
            }

            if (p.GetCustomAttributes().Any(r => r.GetType() == typeof(XmlElementAttribute)))
            {
                XmlElementAttribute xmlAttData = (XmlElementAttribute) p.GetCustomAttributes()
                    .FirstOrDefault(r => r.GetType() == typeof(XmlElementAttribute));
                return xmlAttData.ElementName ?? propName;
            }

            return propName;

        }
    }
}
