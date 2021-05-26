using PsScriptLauncher.constant;
using PsScriptLauncher.dto;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AryxDevLibrary.utils;
using AryxDevLibrary.utils.logger;

namespace PsScriptLauncher.business
{
    internal class ScriptRunner
    {
        private static Logger log = Logger.LastLoggerInstance;

        private ScriptDto scriptDto;

        private string oufFilename { get; set; }
        private int oufFileLineDone { get; set; }
        private StringBuilder outputFile = new StringBuilder();
        private Process _runningProcess;

        public ScriptRunner(ScriptDto scriptDto)
        {
            this.scriptDto = scriptDto;
        }


        public EnumExitCode Run(List<string> scriptArgsTemplate)
        {
            List<string> scriptArgList = scriptArgsTemplate;
            int scriptArgListCount = scriptArgList.Count;
            scriptArgList.Insert(0, String.Join(" ", scriptArgList));

            /*
            if (scriptDto.PowershellParams.OutputToFile && scriptDto.PowershellParams.NoExit)
            {
                log.Error("Erreur de configuration du script : noExit et outputToFile sont mutuellement exclusifs.");
                return EnumExitCode.ErrorsPowershellParams;
            }
            */

            String scriptArgTpl = GetScriptArgsTpl(scriptDto);



            int scriptArgsSubsPresent = ExtractNbArgs(scriptArgTpl);

            if (scriptArgListCount < scriptArgsSubsPresent)
            {
                log.Error("Pas assez d'argument passé avec le paramètre -a.");
                return EnumExitCode.NotEnoughtArgs;


            }

            String scriptArgLine = null;
            if (scriptArgListCount > 0)
            {
                scriptArgLine = ReplaceArgsInTpl(scriptArgTpl, scriptArgList);
            }
            else
            {
                scriptArgLine = scriptArgTpl;
            }
            log.Debug("CommandLine: {0}", scriptArgLine);

            if (!LaunchProcess(scriptArgLine)) return EnumExitCode.ErrorWhenRunningScript;

            return EnumExitCode.Ok;
        }

       


        private bool IsOutputYes(EnumTypeOutput o)
        {
            return o == EnumTypeOutput.NoIfNothing || o == EnumTypeOutput.Yes;
        }

        private bool LaunchProcess(string scriptArgLine)
        {
            _runningProcess = null;
            try
            {
                ProcessStartInfo pInfo = new ProcessStartInfo();
                pInfo.FileName = "powershell.exe";
                if (scriptArgLine != null)
                {
                    pInfo.Arguments = scriptArgLine;
                }

                if (!String.IsNullOrWhiteSpace(scriptDto.Wd))
                {
                    pInfo.WorkingDirectory = scriptDto.Wd;
                }

                if (scriptDto.PowershellParams.WithAdmin)
                {
                    pInfo.Verb = "runas";
                }

                if (IsOutputYes(scriptDto.PowershellParams.OutputToFileEnum))
                {
                    pInfo.RedirectStandardOutput = true;
                    pInfo.RedirectStandardError = true;
                    pInfo.UseShellExecute = false;
                    oufFilename = String.Format("outFile-{0}-{1:yyyy-MM-dd HHmmss}.txt", scriptDto.Id, DateTime.Now);
                    log.Debug("Sortie enregistrée dansle fchier {0}", oufFilename);
                }


                _runningProcess = new Process();
                _runningProcess.StartInfo = pInfo;
                if (IsOutputYes(scriptDto.PowershellParams.OutputToFileEnum))
                {
                    _runningProcess.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
                    _runningProcess.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);
                }

                _runningProcess.Start();
                log.Info("Processus démarré -> id: {0}", _runningProcess.Id);
                if (IsOutputYes(scriptDto.PowershellParams.OutputToFileEnum))
                {
                    _runningProcess.BeginOutputReadLine();
                    _runningProcess.BeginErrorReadLine();
                }

                if (scriptDto.PowershellParams.WaitSeconds > 0)
                {
                    _runningProcess.WaitForExit(scriptDto.PowershellParams.WaitSeconds * 1000);
                    if (!_runningProcess.HasExited)
                    {
                        _runningProcess.Kill();
                    }
                }
                else
                {
                    _runningProcess.WaitForExit();
                }



                log.Info("Script exit-code: {0}", _runningProcess.ExitCode);
                return true;
            }
            catch (Exception e)
            {
                log.Error("Erreur lors du lancement du script : {0}", e.Message);
                return false;
            }
            finally
            {
                if (_runningProcess != null && !_runningProcess.HasExited)
                {
                    try
                    {
                        _runningProcess.Kill();
                    }
                    catch (Exception e)
                    {
                        ExceptionHandlingUtils.LogAndHideException(e, "Erreur lors de la tenative d'arrêt du processus :");
                    }
                }
                if (outputFile != null && scriptDto.PowershellParams.OutputToFileEnum == EnumTypeOutput.Yes)
                {
                    FlushOutputFile();
                }
            }

            return false;
        }



        private void OutputHandler(object sender, DataReceivedEventArgs e)
        {
            outputFile.AppendLine(e.Data);
            oufFileLineDone++;
            if (oufFileLineDone > 100)
            {
                log.Debug("Flush outFile");
                FlushOutputFile();
                outputFile = new StringBuilder();
                oufFileLineDone = 0;
            }
        }

        private async void FlushOutputFile()
        {
            using (StreamWriter sw = new StreamWriter(oufFilename, append: true))
            {
                await sw.WriteLineAsync(outputFile.ToString());
            }
        }


      

        public bool HandleUnusalExit(Program.CtrlType sig)
        {
            log.Info("Fermeture non prévue détectée");

            if (_runningProcess != null && !_runningProcess.HasExited)
            {
                try
                {
                    _runningProcess.Kill();
                }
                catch (Exception e)
                {
                    ExceptionHandlingUtils.LogAndHideException(e, "Erreur lors de la tenative d'arrêt du processus :");
                    throw e;
                }

            }

            return true;
        }




        public static string GetScriptArgsTpl(ScriptDto scriptDto)
        {
            StringBuilder str = new StringBuilder("-ExecutionPolicy RemoteSigned ");
            PowershellParams p = scriptDto.PowershellParams;

            str.AppendFormat("{0}", p.NoProfile ? "-NoProfile " : "");
            str.AppendFormat("{0}", p.NoExit ? "-NoExit " : "");
            str.AppendFormat("{0}", p.NoLogo ? "-NoLogo " : "");
            str.AppendFormat("{0}", p.WindowStyle != null ? "-WindowStyle " + p.WindowStyle + " " : "");

            str.AppendFormat("-File \"{0}\" {1}", scriptDto.Path, scriptDto.Args);

            return str.ToString();
        }

        public static int ExtractNbArgs(string scriptArgTpl)
        {
            int scriptArgsSubsPresent = 0;
            for (int i = 0; i < 100; i++)
            {
                string strI = "%" + i;
                if (scriptArgTpl.Contains(strI))
                {
                    scriptArgsSubsPresent++;
                }
            }

            return scriptArgsSubsPresent;
        }

        public static string ReplaceArgsInTpl(string argTpl, List<string> argsList)
        {
            int scriptArgLineNbSubsDone = 0;
            var scriptArgLine = argTpl;
            for (var index = 0; index < argsList.Count; index++)
            {
                string a = argsList[index];

                string strIndex = "%" + index;

                if (scriptArgLine.Contains(strIndex))
                {
                    scriptArgLine = scriptArgLine.Replace(strIndex, a);
                    log.Debug("{0} => {1}", strIndex, a);
                    scriptArgLineNbSubsDone++;
                }
            }

            return scriptArgLine;
        }
    }
}
