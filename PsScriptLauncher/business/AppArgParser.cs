using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;
using AryxDevLibrary.utils.cliParser;
using AryxDevLibrary.utils.logger;
using AryxDevLibrary.utils.xml;
using PsScriptLauncher.constant;
using PsScriptLauncher.dto;

namespace PsScriptLauncher.business
{
    internal class AppArgParser : CliParser<ArgsAppDto>
    {
        private static Logger log = Logger.LastLoggerInstance;


        private static readonly Option _pwScriptId = new Option()
        {
            ShortOpt = "i",
            LongOpt = "powershellScriptId",
            Description = "Id du script PowerShell à lancer",
            HasArgs = true,
            IsMandatory = false,
            Name = "pwScriptId"
        };

        private static readonly Option _listId = new Option()
        {
            ShortOpt = "l",
            LongOpt = "listScripts",
            Description = "Liste les scripts enregistrés",
            HasArgs = false,
            IsMandatory = false,
            Name = "listId"
        };

        private static readonly Option _sArgs = new Option()
        {
            ShortOpt = "a",
            LongOpt = "args",
            Description = String.Format("Si -{0}, arguments des scripts", _pwScriptId.ShortOpt),
            HasArgs = true,
            IsMandatory = false,
            Name = "sArgs",

        };

        private static readonly Option _haltOnError = new Option()
        {
            ShortOpt = "h",
            LongOpt = "haltOnError",
            Description = "Marque une pause en cas d'erreur",
            HasArgs = false,
            IsMandatory = false,
            Name = "haltOnError"
        };

        private static readonly Option _xmlScriptFile = new Option()
        {
            ShortOpt = "x",
            LongOpt = "xmlFile",
            Description = "Précise le fichier xml de référence à utiliser (par défaut : scripts.xml)",
            HasArgs = true,
            IsMandatory = false,
            Name = "xmlScriptFile",
            DefaultValue = "scripts.xml",
        };

        private static readonly Option _initNewScriptXml = new Option()
        {
            ShortOpt = "n",
            LongOpt = "initNewScriptXml",
            Description = "Initialise une nouvelle entrée dans le fichier xml",
            HasArgs = false,
            IsMandatory = false,
            Name = "initNewScriptXml"

        };

        private static readonly Option _interactiveLaunch = new Option()
        {
            ShortOpt = "c",
            LongOpt = "interactifLaunch",
            Description = "Permet le lancement de script par un menu",
            HasArgs = false,
            IsMandatory = false,
            Name = "interactiveLaunch"

        };


        public AppArgParser()
        {
            AddOption(_pwScriptId);
            AddOption(_listId);
            AddOption(_initNewScriptXml);
            AddOption(_sArgs);
            AddOption(_xmlScriptFile);
            AddOption(_interactiveLaunch);
            AddOption(_haltOnError);


        }

        public override ArgsAppDto ParseDirect(string[] args)
        {

            return Parse(args, ParseTrt);

        }

        private ArgsAppDto ParseTrt(Dictionary<string, Option> arg)
        {
            ArgsAppDto appArgs = new ArgsAppDto();

            if (HasOption(_pwScriptId, arg))
            {
                appArgs.Mode = EnumModeLancement.RunScript;
                appArgs.ScriptId = GetSingleOptionValue(_pwScriptId, arg);
            }
            else if (HasOption(_listId, arg))
            {
                appArgs.Mode = EnumModeLancement.ListScript;
            }
            else if (HasOption(_initNewScriptXml, arg))
            {
                appArgs.Mode = EnumModeLancement.NewScriptInit;
            }
            else if (HasOption(_interactiveLaunch, arg))
            {
                appArgs.Mode = EnumModeLancement.InteractiveLaunch;
            }

            if (appArgs.Mode == EnumModeLancement.RunScript)
            {
                if (HasOption(_sArgs, arg))
                {
                    appArgs.ScriptArgsInput = _sArgs.Value;
                }
            }

            appArgs.HaltOnError = HasOption(_haltOnError, arg);

            String filepath = "scripts.xml";
            if (!File.Exists(filepath))
            {
                FileInfo fEa = new FileInfo(Assembly.GetExecutingAssembly().Location);
                filepath = Path.Combine(fEa.DirectoryName, filepath);
            }
            if (HasOption(_xmlScriptFile, arg))
            {
                filepath = GetSingleOptionValue(_xmlScriptFile, arg);

            }

            if (!File.Exists(filepath))
            {
                if (appArgs.Mode == EnumModeLancement.NewScriptInit)
                {
                    XmlFile xmlF = XmlFile.NewFromEmpty(filepath, "scripts");
                    xmlF.Save();
                }
                else
                {
                    throw new CliParsingException($"Le fichier {filepath} n'existe pas ou n'est pas accessible.");
                }
            }

            log.Debug("Lecture fichier xml");
            try
            {
                appArgs.XmlFile = XmlFile.InitXmlFile(filepath);
            }
            catch (XmlException e)
            {
                log.Error("Erreur lors de lecture du fichier xml {0} - {1}", filepath, e.Message);
                throw new CliParsingException($"Erreur lors de lecture du fichier xml {filepath} - {e.Message}", e);
            }

            return appArgs;

        }
    }
}
