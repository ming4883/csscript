#region Licence...

//-----------------------------------------------------------------------------
// Date:	17/10/04	Time: 2:33p
// Module:	CSExecutionClient.cs
// Classes:	CSExecutionClient
//			AppInfo
//
// This module contains the definition of the CSExecutionClient class. Which implements
// compiling C# code and executing 'Main' method of compiled assembly
//
// Written by Oleg Shilo (oshilo@gmail.com)
//----------------------------------------------
// The MIT License (MIT)
// Copyright (c) 2014 Oleg Shilo
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software 
// and associated documentation files (the "Software"), to deal in the Software without restriction, 
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial 
// portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT 
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE 
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//----------------------------------------------

#endregion Licence...

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Collections;

namespace csscript
{
    delegate void PrintDelegate(string msg);

    /// <summary>
    /// Wrapper class that runs CSExecutor within console application context.
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
            Encoding originalEncoding = Console.OutputEncoding;
            try
            {
                //Debug.Assert(false);
                SetInitialConsoleEncoding();

                //work around of nasty Win7x64 problem.
                //http://superuser.com/questions/527728/cannot-resolve-windir-cannot-modify-path-or-path-being-reset-on-boot
                if (Environment.GetEnvironmentVariable("windir") == null)
                    Environment.SetEnvironmentVariable("windir", Environment.GetEnvironmentVariable("SystemRoot"));

                Profiler.Stopwatch.Start();

                string[] args = rawArgs;

                //Debug.Assert(false);

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
#if !net4
            throw new ApplicationException("Cannot build surrogate host application because this script engine is build against early version of CLR.");
#else
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
#endif
                }
                catch (SurrogateHostProcessRequiredException e)
                {
#if !net4
                object dummy = e;
                throw new ApplicationException("Cannot build surrogate host application because this script engine is build against early version of CLR.");
#else

                    try
                    {
                        string assemblyHost = ScriptLauncherBuilder.GetLauncherName(e.ScriptAssembly);
                        string appArgs = CSSUtils.cmdFlagPrefix + "css_host_parent:" + Process.GetCurrentProcess().Id + " \"" + CSSUtils.cmdFlagPrefix + "css_host_asm:" + e.ScriptAssembly + "\" " + GenerateCommandLineArgumentsString(e.ScriptArgs);
                        if (e.StartDebugger)
                            appArgs = CSSUtils.cmdFlagPrefix + "css_host_dbg:true " + appArgs;

                        RunConsoleApp(assemblyHost, appArgs);
                    }
                    catch (Exception e1)
                    {
                        Console.WriteLine("Cannot execute Surrogate Host Process: " + e1);
                    }
#endif
                }
            }
            finally
            {
                Console.OutputEncoding = originalEncoding;
            }
        }

        /// <summary>
        /// Implementation of displaying application messages.
        /// </summary>
        static void Print(string msg)
        {
            Console.WriteLine(msg);
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
                sb.Append("\" ");
            }

            return sb.ToString();
        }

        static void SetInitialConsoleEncoding()
        {
            //consider: https://social.msdn.microsoft.com/Forums/vstudio/en-US/e448b241-e250-4dcb-8ecd-361e00920dde/consoleoutputencoding-breaks-batch-files?forum=netfxbcl
            string consoleEncoding = Utils.GetConsoleEncodingOverwrite();
            if (consoleEncoding == null)
                Console.OutputEncoding = System.Text.Encoding.UTF8;
            else if (!consoleEncoding.Equals("default", StringComparison.OrdinalIgnoreCase))
                try { Console.OutputEncoding = System.Text.Encoding.GetEncoding(consoleEncoding); }
                catch { }
        }
#if net4
        static void RunConsoleApp(string app, string args)
        {
            Process process = new Process();
            process.StartInfo.FileName = app;
            process.StartInfo.Arguments = args;
            process.StartInfo.WorkingDirectory = Environment.CurrentDirectory;

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.ErrorDialog = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            bool outputDrained = false;
            bool errorOutputDrained = false;
            Action<StreamReader, Stream> redirect = (src, dest) =>
            {
                while (true)
                {
                    int nextChar = src.Read();
                    if (nextChar == -1)
                    {
                        outputDrained = outputDrained || (src == process.StandardOutput);
                        errorOutputDrained = errorOutputDrained || (src == process.StandardError);
                        break;
                    }

                    dest.WriteByte((byte)nextChar);
                    dest.Flush();
                }
            };

            ThreadPool.QueueUserWorkItem(x =>
                redirect(process.StandardOutput, Console.OpenStandardOutput()));

            ThreadPool.QueueUserWorkItem(x =>
                redirect(process.StandardError, Console.OpenStandardError()));

            ThreadPool.QueueUserWorkItem(x =>
            {
                while (true)
                {
                    int nextChar = Console.Read();
                    process.StandardInput.Write((char)nextChar);
                    process.StandardInput.Flush();
                }
            });

            process.WaitForExit();
            Environment.ExitCode = process.ExitCode;

            while (!outputDrained || !errorOutputDrained) //the output buffer may still contain some data just after the process exited
                Thread.Sleep(1);
        }

#endif
    }

    /// <summary>
    /// Repository for application specific data
    /// </summary>
    class AppInfo
    {
        public static string appName = "cscscript";
        public static bool appConsole = true;

        public static string appLogo
        {
            get { return "C# Script execution engine. Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + ".\nCopyright (C) 2004-2014 Oleg Shilo.\n"; }
        }

        public static string appLogoShort
        {
            get { return "C# Script execution engine. Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + ".\n"; }
        }

        //#pragma warning disable 414
        //public static string appParams = "[/nl]:";
        //#pragma warning restore 414
        public static string appParamsHelp = "nl   - No logo mode: No banner will be shown/printed at execution time.\n";
    }
}