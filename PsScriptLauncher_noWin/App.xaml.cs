using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;

namespace PsScriptLauncher_noWin
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : Application
    {

        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            FileInfo mainExe = new FileInfo("PsScriptLauncher.exe");
            if (!mainExe.Exists) Environment.Exit(0);

            ProcessStartInfo pStartInfo = new ProcessStartInfo
            {
                FileName = mainExe.FullName,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = false,
                Arguments = Environment.CommandLine
                    .Replace("\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"", "").Trim()
            };

            Process process = new Process {StartInfo = pStartInfo};

            process.Start();
            process.WaitForExit();

            int exitCode = 1;
            try
            {
                exitCode = process.ExitCode;
            }
            catch (Exception)
            {
                exitCode = -1;
            }

            Environment.Exit(exitCode);

        }
    }
}
