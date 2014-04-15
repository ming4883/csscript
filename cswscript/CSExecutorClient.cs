//-----------------------------------------------------------------------------
// Date:	17/10/04	Time: 2:33p
// Module:	CSExecutionClient.cs
// Classes:	CSExecutionClient
//
// This module contains the definition of the CSExecutor class. Which implements
// compiling C# code and executing 'Main' method of compiled assembly
//
// Written by Oleg Shilo (oshilo@gmail.com)
// Copyright (c) 2004-2013. All rights reserved.
//
// Redistribution and use of this code WITHOUT MODIFICATIONS are permitted provided that
// the following conditions are met:
// 1. Redistributions must retain the above copyright notice, this list of conditions
//  and the following disclaimer.
// 2. Neither the name of an author nor the names of the contributors may be used
//	to endorse or promote products derived from this software without specific
//	prior written permission.
//
// Redistribution and use of this code WITH MODIFICATIONS are permitted provided that all
// above conditions are met and software is not used or sold for profit.
//
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
// TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
//	Caution: Bugs are expected!
//----------------------------------------------
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Text;
using System.Collections.Generic;

namespace csscript
{
    delegate void PrintDelegate(string msg);

    /// <summary>
    /// Wrapper class that runs CSExecutor within windows application context.
    /// </summary>
    class CSExecutionClient
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern bool SetEnvironmentVariable(string lpName, string lpValue);

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] rawArgs)
        {
            //Debug.Assert();
            string[] args = rawArgs;

            if (Utils.IsLinux())
            {
                //because Linux shebang does not properly split arguments we need to take care of this
                //http://www.daniweb.com/software-development/c/threads/268382
                List<string> tempArgs = new List<string>();
                foreach (string arg in rawArgs)
                    if (arg.StartsWith(CSSUtils.cmdFlagPrefix))
                    {
                        foreach (string subArg in arg.Split(CSSUtils.cmdFlagPrefix.ToCharArray()))
                            if (subArg.Trim() != "")
                                tempArgs.Add(CSSUtils.cmdFlagPrefix + subArg.Trim());
                    }
                    else
                        tempArgs.Add(arg);

                args = tempArgs.ToArray();
            }

            try
            {
                SetEnvironmentVariable("CSScriptRuntime", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
                SetEnvironmentVariable("CSScriptRuntimeLocation", System.Reflection.Assembly.GetExecutingAssembly().Location);
            }
            catch { } //SetEnvironmentVariable will always throw an exception on Mono

            CSExecutor exec = new CSExecutor();

            if (AppDomain.CurrentDomain.FriendlyName != "ExecutionDomain") // AppDomain.IsDefaultAppDomain is more appropriate but it is not available in .NET 1.1
            {
                string configFile = exec.GetCustomAppConfig(args);
                if (configFile != "")
                {
                    AppDomainSetup setup = AppDomain.CurrentDomain.SetupInformation;
                    setup.ConfigurationFile = configFile;

                    AppDomain appDomain = AppDomain.CreateDomain("ExecutionDomain", null, setup);
#if !net4
                    appDomain.ExecuteAssembly(Assembly.GetExecutingAssembly().Location, null, args);
#else
                    appDomain.ExecuteAssembly(Assembly.GetExecutingAssembly().Location, args);
#endif
                    return;
                }
            }
            try
            {
                AppInfo.appName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
                exec.Execute(args, new PrintDelegate(Print), null);
            }
            catch (Surrogate86ProcessRequiredException)
            {
                try
                {
                    string thisAssembly = Assembly.GetExecutingAssembly().Location;
                    string runner = Path.Combine(Path.GetDirectoryName(thisAssembly), "lib\\runasm32.exe");

                    if (!File.Exists(runner))
                        runner = Path.Combine(Path.GetDirectoryName(thisAssembly), "runasm32.exe");

                    if (!File.Exists(runner))
                        runner = Environment.ExpandEnvironmentVariables("CSSCRIPT_32RUNNER");

                    if (!File.Exists(runner))
                    {
                        Print("This script requires to be executed as x86 process but no runner (e.g. runasm32.exe) can be found.");
                    }
                    else
                    {
                        RunConsoleApp(runner, "\"" + thisAssembly + "\" " + GetCommandLineArgumentsStringFromEnvironment());
                    }
                }
                catch { } //This will always throw an exception on Mono
            }
            catch (SurrogateHostProcessRequiredException e)
            {
                try
                {
                    string assemblyHost = ScriptLauncherBuilder.GetLauncherName(e.ScriptAssembly);
                    string appArgs = "/css_host_asm:" + e.ScriptAssembly + " " + GenerateCommandLineArgumentsString(e.ScriptArgs);
                    if (e.StartDebugger)
                        appArgs = "/css_host_dbg:true " + appArgs;

                    RunConsoleApp(assemblyHost, appArgs);
                }
                catch (Exception e1)
                {
                    Console.WriteLine("Cannot execute Surrogate Host Process: " + e1);
                }
            }
        }

        /// <summary>
        /// Implementation of displaying application messages.
        /// </summary>
        static void Print(string msg)
        {
            MessageBox.Show(msg, "cs-script");
        }

        static string GetCommandLineArgumentsStringFromEnvironment()
        {
            if (Environment.CommandLine.StartsWith("\""))
            {
                return Environment.CommandLine.Substring(Environment.CommandLine.IndexOf('"', 1) + 1).TrimStart();
            }
            else
            {
                return Environment.CommandLine.Substring(Environment.CommandLine.IndexOf(' ') + 1).TrimStart();
            }
        }

        static string GenerateCommandLineArgumentsString(string[] args)
        {
            StringBuilder sb = new StringBuilder();

            foreach (string arg in args)
            {
                sb.Append("\"");
                sb.Append(arg);
                sb.Append("\"");
            }

            return sb.ToString();
        }

        static void RunConsoleApp(string app, string args)
        {
            Process process = new Process();
            process.StartInfo.FileName = app;
            process.StartInfo.Arguments = args;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
        }
    }

    /// <summary>
    /// Repository for application specific data
    /// </summary>
    static class AppInfo
    {
        public static string appName = "cswscript";
        public static bool appConsole = false;

        public static string appLogo
        {
            get { return "C# Script execution engine. Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + ".\nCopyright (C) 2004-2013 Oleg Shilo.\n"; }
        }

        public static string appLogoShort
        {
            get { return "C# Script execution engine. Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + ".\n"; }
        }

        public static string appParamsHelp = "";
    }
}