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

        //static void Test()
        //{
        //    string code = File.ReadAllText(@"C:\Users\----\Documents\C# Scripts\New Script7.cs");
        //    //AutoclassPrecompiler.Compile(ref code, null, true, null);
        //    int pos = 88;
        //    AutoclassGenerator.Process(code, ref pos);
        //}

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] rawArgs)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            //work around of nusty Win7x64 problem.
            //http://superuser.com/questions/527728/cannot-resolve-windir-cannot-modify-path-or-path-being-reset-on-boot
            if (Environment.GetEnvironmentVariable("windir") == null)
                Environment.SetEnvironmentVariable("windir", Environment.GetEnvironmentVariable("SystemRoot"));
            //var tt = Assembly.GetExecutingAssembly().Location;
            //Test();
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

            //Console.WriteLine("<--------------------------");
            //foreach (var item in args)
            //    Console.WriteLine(item);
            //Console.WriteLine("<--------------------------");

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
                    string appArgs = "\"" + CSSUtils.cmdFlagPrefix + "css_host_asm:" + e.ScriptAssembly + "\" " + GenerateCommandLineArgumentsString(e.ScriptArgs);
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
            get { return "C# Script execution engine. Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() + ".\nCopyright (C) 2004-2013 Oleg Shilo.\n"; }
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