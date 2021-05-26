using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using AryxDevLibrary.extensions;
using AryxDevLibrary.utils;
using AryxDevLibrary.utils.logger;
using AryxDevLibrary.utils.xml;
using PsScriptLauncher.business;
using PsScriptLauncher.constant;
using PsScriptLauncher.dto;
using PsScriptLauncher.utils;
using XmlUtils = AryxDevLibrary.utils.XmlUtils;

namespace PsScriptLauncher
{
    class Program
    {
        private ArgsAppDto appArgs = null;

        private static Logger log = new Logger("log.log", Logger.LogLvl.INFO, Logger.LogLvl.DEBUG, "1 Mo");
        private EnumExitCode appExitCode = EnumExitCode.Ok;

        #region Trap application termination
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;

        public enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }
        #endregion

        static void Main(string[] args)
        {
            Program inst = new Program();

            inst.Run(args);

        }



        private void Run(string[] args)
        {
            ExceptionHandlingUtils.Logger = log;
            log.Debug("Démarrage du programme");

            AppArgParser parser = new AppArgParser();
            appArgs = parser.ParseDirect(args);
            log.Debug("Paramètres: {0}", appArgs);

            // Console.SetWindowPosition();

            try
            {

                XmlParser xmlParser = new XmlParser(appArgs.XmlFile);
                List<ScriptDto> listScript = xmlParser.GetListScripts();
                log.Debug("Scripts disponibles: {0}", String.Join(", ", listScript.Select(r => r.Id)));

                switch (appArgs.Mode)
                {
                    case EnumModeLancement.NoMode:
                        ShowSyntax(parser);
                        break;
                    case EnumModeLancement.RunScript:
                        RunScript(listScript);
                        break;
                    case EnumModeLancement.ListScript:
                        ListScript(listScript);
                        break;
                    case EnumModeLancement.NewScriptInit:
                        InitNewScript(listScript);
                        break;
                    case EnumModeLancement.InteractiveLaunch:
                        RunScriptInteractive(listScript);
                        break;
                }
            }
            catch (Exception e)
            {
                ExceptionHandlingUtils.LogAndHideException(e);
                if (appArgs.HaltOnError)
                {
                    Console.WriteLine("Appuyez sur une touche pour continuer");
                    Console.ReadKey();
                }
            }

            Environment.ExitCode = (int)appExitCode;
        }



        private void InitNewScript(List<ScriptDto> listScript)
        {
            String newId = StringUtils.RandomString(16);

            XmlFile xmlF = appArgs.XmlFile;

            XmlAttribute idAttribute = xmlF.Doc.CreateAttribute("id");
            idAttribute.Value = newId;

            XmlElement script = xmlF.CreateXmlElement("script");
            script.Attributes.Append(idAttribute);

            xmlF.CreateSetValueAndAppendTo("path", "mandatory", script);
            xmlF.CreateSetValueAndAppendTo("wd", "optionnal", script);
            xmlF.CreateSetValueAndAppendTo("args", "optionnal", script);

            XmlElement psParamsXml = xmlF.CreateAndAppendTo("powershellParams", script);
            xmlF.CreateSetValueAndAppendTo(AppReflexionUtils.GetXmlName(nameof(PowershellParams.NoExit)), "true|false", psParamsXml);
            xmlF.CreateSetValueAndAppendTo(AppReflexionUtils.GetXmlName(nameof(PowershellParams.NoLogo)), "true|false", psParamsXml);
            xmlF.CreateSetValueAndAppendTo(AppReflexionUtils.GetXmlName(nameof(PowershellParams.NoProfile)), "true|false", psParamsXml);
            xmlF.CreateSetValueAndAppendTo(AppReflexionUtils.GetXmlName(nameof(PowershellParams.WindowStyle)), "Normal|Hidden|Minimized|Maximized", psParamsXml);
            xmlF.CreateSetValueAndAppendTo(AppReflexionUtils.GetXmlName(nameof(PowershellParams.WaitSeconds)), "lt0:infinite", psParamsXml);
            xmlF.CreateSetValueAndAppendTo(AppReflexionUtils.GetXmlName(nameof(PowershellParams.WithAdmin)), "true|false", psParamsXml);
            xmlF.CreateSetValueAndAppendTo(AppReflexionUtils.GetXmlName(nameof(PowershellParams.OutputToFile)), "No|NoIfNothing|Yes", psParamsXml);


            XmlComment scriptComment = xmlF.Doc.CreateComment(XmlUtils.FormatXmlString(script.OuterXml));
            xmlF.Root.AppendChild(scriptComment);
            xmlF.Save();



            FileUtils.ShowFileInWindowsExplorer(xmlF.FileXmlPath);
        }

        private void ListScript(List<ScriptDto> listScript)
        {
            foreach (ScriptDto scriptDto in listScript)
            {
                Console.WriteLine("Script id : {0}", scriptDto.Id);
                Console.WriteLine("   File : {0}", scriptDto.Path);
                if (scriptDto.Wd != null)
                {
                    Console.WriteLine("   Wd : {0}", scriptDto.Wd);
                }
                if (scriptDto.Args != null)
                {
                    Console.WriteLine("   Args : {0}", scriptDto.Args);
                }

                if (scriptDto.PowershellParams != null)
                {
                    Console.WriteLine("   PowershellArgs : {0}", scriptDto.PowershellParams);
                }

                Console.WriteLine();
            }
        }

        private void ShowSyntax(AppArgParser parser)
        {
            appExitCode = EnumExitCode.ShowSyntax;
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine("   POWERSHELL SCRIPT LAUNCHER   ::  Lanceur de scripts PowerShell");
            Console.WriteLine("-------------------------------------------------------------------------------");
            Console.WriteLine();
            parser.ShowSyntax();
            Console.WriteLine();
        }

        private void RunScript(List<ScriptDto> listScript)
        {
            try
            {

                if (String.IsNullOrWhiteSpace(appArgs.ScriptId) || !listScript.Any(r => r.Id.Equals(appArgs.ScriptId)))
                {
                    log.Error("Identifiant du script absent ou non reconnu ('{0}' : {1})", appArgs.ScriptId,
                        String.Join(", ", listScript.Select(r => r.Id)));
                    appExitCode = EnumExitCode.NoScriptOrNotKnown;

                    return;
                }

                ScriptDto scriptDto = listScript.FirstOrDefault(r => r.Id.Equals(appArgs.ScriptId));
                log.Info("Script à lancer (id: {0}, script: {1})", scriptDto.Id, scriptDto.Path);

                ScriptRunner sRun = new ScriptRunner(scriptDto);
                _handler += new EventHandler(sRun.HandleUnusalExit);
                SetConsoleCtrlHandler(_handler, true);
                sRun.Run(appArgs.ScriptArgsInput);

            }
            catch (Exception e)
            {
                log.Error("Erreur inattendue : {0}", e.Message);
                ExceptionHandlingUtils.LogAndHideException(e, isWarnMsgAndDebugStack: true);
                appExitCode = EnumExitCode.ErrorScriptRunner;

                return;
            }

        }

        private void RunScriptInteractive(List<ScriptDto> listScript)
        {

            while (true)
            {
                int nbEltParLigne = 3;
                StringBuilder strShow = new StringBuilder();

                String tpl = "[{0:" + new String(' ', 2) + "}] {1,-25}";
                tpl = "[{0}] {1,-25}";

                int i = 0;
                for (; i < listScript.Count; i++)
                {
                    ScriptDto scriptDto = listScript[i];

                    strShow.AppendFormat(tpl, i, scriptDto.Id);

                    if ((i + 1) % nbEltParLigne == 0)
                    {
                        strShow.AppendLine();
                    }
                }


                strShow.AppendFormat(tpl, i, "Quitter");



                Console.WriteLine(strShow.ToString());
                Console.WriteLine();
                Console.Write("Faites votre choix : ");
                String choiceStr = Console.ReadLine();

                int choice = -1;
                if ((choice = MiscAppUtils.IntParse(choiceStr)) >= 0)
                {
                    if (choice == i)
                    {
                        break;
                    }

                    if (RunScriptInteractiveSub(listScript[choice]))
                    {
                        return;
                    }
                    Console.Clear();
                }


            }
        }



        private bool RunScriptInteractiveSub(ScriptDto scriptDto)
        {
            Console.WriteLine();

        

            bool isDoReturn = SubA();
            return isDoReturn;

            bool SubA()
            {
                while (true)
                {
                    String msgPresentScript = $"Script {scriptDto.Id} :";
                    Console.WriteLine(new String('=', msgPresentScript.Length + 2));
                    Console.WriteLine($" {msgPresentScript}");
                    Console.WriteLine(new String('=', msgPresentScript.Length + 2));

                    Console.WriteLine("[0] Lancer le script");
                    Console.WriteLine("[1] Revenir en arrière");

                    Console.WriteLine();
                    Console.Write("Faites votre choix : ");
                    String choiceStr = Console.ReadLine();

                    int choice = -1;
                    if ((choice = MiscAppUtils.IntParse(choiceStr)) >= 0)
                    {
                        if (choice == 1)
                        {
                            break;
                        }
                        else
                        {
                            if (SubB())
                            {
                                return true;
                            }
                            Console.Clear();
                        }
                    }
                }

                return false;
            }

            bool SubB()
            {
               
                while (true)
                {
                    Console.WriteLine();

                    string argScript = ScriptRunner.GetScriptArgsTpl(scriptDto);
                    int nbArgsScript = ScriptRunner.ExtractNbArgs(argScript);

                    if (nbArgsScript == 0)
                    {
                        Console.WriteLine("Aucun argument à fournir pour ce script");
                    }
                    else
                    {
                        Console.WriteLine("Paramètres de lancement :");
                        Console.WriteLine(argScript);
                        Console.WriteLine();

                        List<String> listArgs = new List<string>(nbArgsScript);
                        
                        for (int i = 0; i < nbArgsScript; i++)
                        {
                            Console.Write("Valorisez %1: ");
                            listArgs.Insert(i, Console.ReadLine());
                        }
                        listArgs.Insert(0, String.Join(" ", listArgs));

                        Console.WriteLine();
                        string argScriptRepl = ScriptRunner.ReplaceArgsInTpl(argScript, listArgs);
                        Console.WriteLine("Le script sera lancé avec ces paramètres :");
                        Console.WriteLine(argScriptRepl);
                        Console.WriteLine();

                        Console.WriteLine();
                        Console.Write("Voulez-vous continuer ? [o/n/a] : ");
                        String choiceStr = Console.ReadLine();

                        if (choiceStr == null || choiceStr.Equals("n", StringComparison.CurrentCultureIgnoreCase))
                        {
                            continue;

                        } else if (choiceStr.Equals("a", StringComparison.CurrentCultureIgnoreCase))
                        {
                            return false;
                        }


                        appArgs.ScriptArgsInput = listArgs;

                    }

                    try
                    {

                        ScriptRunner sRun = new ScriptRunner(scriptDto);
                        _handler += new EventHandler(sRun.HandleUnusalExit);
                        SetConsoleCtrlHandler(_handler, true);
                        sRun.Run(appArgs.ScriptArgsInput);
                    }
                    catch (Exception e)
                    {
                        Thread.Sleep(5000);
                        return false;
                    }

                    break;
                }

                return true;
            }

           
        }
    }
}
