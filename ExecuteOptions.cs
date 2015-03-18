#region Licence...

//-----------------------------------------------------------------------------
// Date:	17/10/04	Time: 2:33p
// Module:	csscript.cs
// Classes:	CSExecutor
//			ExecuteOptions
//
// This module contains the definition of the CSExecutor class. Which implements
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
using System.IO;
using System.Reflection;

#if net1
using System.Collections;
#else

using System.Collections.Generic;

#endif

using System.Text;
using CSScriptLibrary;
using System.Runtime.InteropServices;
using System.Threading;
using System.CodeDom.Compiler;
//using System.Windows.Forms;
using System.Globalization;
using System.Diagnostics;
using Microsoft.CSharp;


namespace csscript
{
    /// <summary>
    /// Application specific runtime settings
    /// </summary>
    internal class ExecuteOptions : ICloneable
    {
        public static ExecuteOptions options = new ExecuteOptions();

        public ExecuteOptions()
        {
            options = this;
        }

        public object Clone()
        {
            ExecuteOptions clone = new ExecuteOptions();
            clone.processFile = this.processFile;
            clone.scriptFileName = this.scriptFileName;
            clone.noLogo = this.noLogo;
            clone.useCompiled = this.useCompiled;
            clone.useSmartCaching = this.useSmartCaching;
            clone.DLLExtension = this.DLLExtension;
            clone.forceCompile = this.forceCompile;
            clone.supressExecution = this.supressExecution;
            clone.DBG = this.DBG;
            clone.TargetFramework = this.TargetFramework;
            clone.verbose = this.verbose;
            clone.startDebugger = this.startDebugger;
            clone.local = this.local;
            clone.buildExecutable = this.buildExecutable;
#if net1
                clone.refAssemblies = (string[])new ArrayList(this.refAssemblies).ToArray(typeof(string));
                clone.searchDirs = (string[])new ArrayList(this.searchDirs).ToArray(typeof(string));

#else
            clone.refAssemblies = new List<string>(this.refAssemblies).ToArray();
            clone.searchDirs = new List<string>(this.searchDirs).ToArray();
#endif

            clone.buildWinExecutable = this.buildWinExecutable;
            clone.useSurrogateHostingProcess = this.useSurrogateHostingProcess;
            clone.altCompiler = this.altCompiler;
            clone.preCompilers = this.preCompilers;
            clone.postProcessor = this.postProcessor;
            clone.compilerOptions = this.compilerOptions;
            clone.reportDetailedErrorInfo = this.reportDetailedErrorInfo;
            clone.hideCompilerWarnings = this.hideCompilerWarnings;
            clone.apartmentState = this.apartmentState;
            clone.openEndDirectiveSyntax = this.openEndDirectiveSyntax;
            clone.forceOutputAssembly = this.forceOutputAssembly;
            clone.cleanupShellCommand = this.cleanupShellCommand;
            clone.versionOnly = this.versionOnly;
            clone.noConfig = this.noConfig;
            //clone.suppressExternalHosting = this.suppressExternalHosting;
            clone.altConfig = this.altConfig;
            clone.defaultRefAssemblies = this.defaultRefAssemblies;
            clone.hideTemp = this.hideTemp;
            clone.autoClass = this.autoClass;
            clone.initContext = this.initContext;
            clone.customHashing = this.customHashing;
            clone.compilationContext = this.compilationContext;
            clone.useScriptConfig = this.useScriptConfig;
            clone.customConfigFileName = this.customConfigFileName;
            clone.scriptFileNamePrimary = this.scriptFileNamePrimary;
            clone.doCleanupAfterNumberOfRuns = this.doCleanupAfterNumberOfRuns;
            clone.inMemoryAsm = this.inMemoryAsm;
            clone.shareHostRefAssemblies = this.shareHostRefAssemblies;
            return clone;
        }

        public object Derive()
        {
            ExecuteOptions clone = new ExecuteOptions();
            clone.processFile = this.processFile;
            //clone.scriptFileName = this.scriptFileName;
            //clone.noLogo = this.noLogo;
            //clone.useCompiled = this.useCompiled;
            clone.useSmartCaching = this.useSmartCaching;
            //clone.DLLExtension = this.DLLExtension;
            //clone.forceCompile = this.forceCompile;
            clone.supressExecution = this.supressExecution;
            clone.InjectScriptAssemblyAttribute = this.InjectScriptAssemblyAttribute;
            clone.DBG = this.DBG;
            clone.TargetFramework = this.TargetFramework;
            clone.verbose = this.verbose;
            clone.local = this.local;
            clone.buildExecutable = this.buildExecutable;
#if net1
                clone.refAssemblies = (string[])new ArrayList(this.refAssemblies).ToArray(typeof(string));
                clone.searchDirs = (string[])new ArrayList(this.searchDirs).ToArray(typeof(string));
#else
            clone.refAssemblies = new List<string>(this.refAssemblies).ToArray();
            clone.searchDirs = new List<string>(this.searchDirs).ToArray();
#endif
            clone.buildWinExecutable = this.buildWinExecutable;
            clone.altCompiler = this.altCompiler;
            clone.preCompilers = this.preCompilers;
            clone.defaultRefAssemblies = this.defaultRefAssemblies;
            clone.postProcessor = this.postProcessor;
            clone.compilerOptions = this.compilerOptions;
            clone.reportDetailedErrorInfo = this.reportDetailedErrorInfo;
            clone.hideCompilerWarnings = this.hideCompilerWarnings;
            clone.openEndDirectiveSyntax = this.openEndDirectiveSyntax;
            clone.apartmentState = this.apartmentState;
            clone.forceOutputAssembly = this.forceOutputAssembly;
            clone.versionOnly = this.versionOnly;
            clone.cleanupShellCommand = this.cleanupShellCommand;
            clone.noConfig = this.noConfig;
            //clone.suppressExternalHosting = this.suppressExternalHosting;
            clone.compilationContext = this.compilationContext;
            clone.autoClass = this.autoClass;
            clone.customHashing = this.customHashing;
            clone.altConfig = this.altConfig;
            clone.hideTemp = this.hideTemp;
            clone.initContext = this.initContext;
            clone.scriptFileNamePrimary = this.scriptFileNamePrimary;
            clone.doCleanupAfterNumberOfRuns = this.doCleanupAfterNumberOfRuns;
            clone.shareHostRefAssemblies = this.shareHostRefAssemblies;
            clone.inMemoryAsm = this.inMemoryAsm;

            return clone;
        }

        public bool inMemoryAsm = false;
        public bool processFile = true;
        public int compilationContext = 0;
        public string scriptFileName = "";
        public object initContext= null;
        public string scriptFileNamePrimary = null;
        public bool noLogo = false;
        public bool useCompiled = false;
        public bool useScriptConfig = false;
        public string customConfigFileName = "";
        public bool useSmartCaching = true; //hardcoded true but can be set from config file in the future
        public bool DLLExtension = false;
        public bool forceCompile = false;
        public bool supressExecution = false;
        public bool DBG = false;
#if net35
        public string TargetFramework = "v3.5";
#else
        public string TargetFramework = "v4.0";
#endif
        internal bool InjectScriptAssemblyAttribute = true;
        public bool verbose = false;
        public bool startDebugger = false;
        public bool local = false;
        public bool buildExecutable = false;
        public string[] refAssemblies = new string[0];
        public string[] searchDirs = new string[0];
        public bool shareHostRefAssemblies = false;
        public bool buildWinExecutable = false;
        public bool openEndDirectiveSyntax = true;
        public bool useSurrogateHostingProcess = false;
        public string altCompiler = "";
        public string preCompilers = "";
        public string defaultRefAssemblies = "";
        public string postProcessor = "";
        public bool reportDetailedErrorInfo = false;
        public bool hideCompilerWarnings = false;
        public ApartmentState apartmentState = ApartmentState.STA;
        public string forceOutputAssembly = "";
        public string cleanupShellCommand = "";
        public bool noConfig = false;
        //public bool suppressExternalHosting = true;
        public bool customHashing = true;
        public bool autoClass = false;
        public bool versionOnly = false;
        public string compilerOptions = "";
        public string altConfig = "";
        public Settings.HideOptions hideTemp = Settings.HideOptions.HideMostFiles;
        public uint doCleanupAfterNumberOfRuns = 20;

        public void AddSearchDir(string dir)
        {
#if net1
                foreach (string item in this.searchDirs)
                    if (item == dir)
                        return;
#else
            if (Array.Find(this.searchDirs, (x) => x == dir) != null)
                return;
#endif
            string[] newSearchDirs = new string[this.searchDirs.Length + 1];
            this.searchDirs.CopyTo(newSearchDirs, 0);
            newSearchDirs[newSearchDirs.Length - 1] = dir;
            this.searchDirs = newSearchDirs;
        }

        public string[] ExtractShellCommand(string command)
        {
            int pos = command.IndexOf("\"");
            string endToken = "\"";
            if (pos == -1 || pos != 0) //no quotation marks
                endToken = " ";

            pos = command.IndexOf(endToken, pos + 1);
            if (pos == -1)
                return new string[] { command };
            else
                return new string[] { command.Substring(0, pos).Replace("\"", ""), command.Substring(pos + 1).Trim() };
        }
    }

}