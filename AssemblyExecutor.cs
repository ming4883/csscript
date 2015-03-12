#region Licence...

//-----------------------------------------------------------------------------
// Date:	17/10/04	Time: 2:33p
// Module:	AssemblyExecutor.cs
// Classes:	AssemblyExecutor
//			RemoteExecutor
//
// This module contains the definition of the AssemblyExecutor class. Which implements
// executing 'public static void Main(..)' method of a assembly in a different AddDomain
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
using CSScriptLibrary;
using System.Threading;

namespace csscript
{
    /// <summary>
    /// Executes "public static void Main(..)" of assembly in a separate domain.
    /// </summary>
    class AssemblyExecutor
    {
        AppDomain appDomain;
        RemoteExecutor remoteExecutor;
        string assemblyFileName;

        public AssemblyExecutor(string fileNname, string domainName)
        {
            assemblyFileName = fileNname;
            AppDomainSetup setup = new AppDomainSetup();
            setup.ApplicationBase = Path.GetDirectoryName(assemblyFileName);
            setup.PrivateBinPath = AppDomain.CurrentDomain.BaseDirectory;
            setup.ApplicationName = Utils.GetAssemblyFileName(Assembly.GetExecutingAssembly());
            setup.ShadowCopyFiles = "true";
            setup.ShadowCopyDirectories = Path.GetDirectoryName(assemblyFileName);
            appDomain = AppDomain.CreateDomain(domainName, null, setup);

            remoteExecutor = (RemoteExecutor)appDomain.CreateInstanceFromAndUnwrap(Assembly.GetExecutingAssembly().FullName, typeof(RemoteExecutor).ToString());
            remoteExecutor.searchDirs = ExecuteOptions.options.searchDirs;
        }

        public void Execute(string[] args)
        {
            remoteExecutor.ExecuteAssembly(assemblyFileName, args);
        }

        public void Unload()
        {
            AppDomain.Unload(appDomain);
            appDomain = null;
        }
    }

    /// <summary>
    /// Invokes static method 'Main' from the assembly.
    /// </summary>
    class RemoteExecutor : MarshalByRefObject
    {
        public string[] searchDirs = new string[0];

        public RemoteExecutor(string[] searchDirs)
        {
            this.searchDirs = searchDirs;
        }

        public RemoteExecutor()
        {
        }

        /// <summary>
        /// AppDomain event handler. This handler will be called if CLR cannot resolve
        /// referenced local assemblies
        /// </summary>
        public Assembly ResolveEventHandler(object sender, ResolveEventArgs args)
        {
            Assembly retval = null;
            foreach (string dir in searchDirs)
            {
                //it is tempting to throw but should not as there can be other (e.g. host) ResolveEventHandler(s)
                //and throwing will prevent them from being invoked
                bool throwExceptions = false;

                retval = AssemblyResolver.ResolveAssembly(args.Name, dir, throwExceptions);
                if (retval != null)
                    break;
            }
            return retval;
        }

        public Assembly ResolveResEventHandler(object sender, ResolveEventArgs args)
        {
            return Assembly.LoadFrom(this.asmFile);
        }

        string asmFile = "";

        public void ExecuteAssembly(string filename, string[] args)
        {
            ExecuteAssembly(filename, args, null);
        }

        public void ExecuteAssembly(string filename, string[] args, Mutex asmLock)
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(ResolveEventHandler);
            AppDomain.CurrentDomain.ResourceResolve += new ResolveEventHandler(ResolveResEventHandler); //xaml

            asmFile = filename;

            Assembly assembly;
            if (!ExecuteOptions.options.inMemoryAsm)
            {
                assembly = Assembly.LoadFrom(filename);
            }
            else
            {
                //Load(byte[]) does not lock the assembly file as LoadFrom(filename) does
                using (FileStream fs = new FileStream(filename, FileMode.Open))
                {
                    byte[] data = new byte[fs.Length];
                    fs.Read(data, 0, data.Length);
                    string dbg = Path.ChangeExtension(filename, ".pdb");
                    if (ExecuteOptions.options.DBG && File.Exists(dbg))
                        using (FileStream fsDbg = new FileStream(dbg, FileMode.Open))
                        {
                            byte[] dbgData = new byte[fsDbg.Length];
                            fsDbg.Read(dbgData, 0, dbgData.Length);
                            assembly = Assembly.Load(data, dbgData);
                        }
                    else
                        assembly = Assembly.Load(data);
                }

                Utils.ReleaseFileLock(asmLock);
            }
            InvokeStaticMain(assembly, args);
        }

        public void InvokeStaticMain(Assembly compiledAssembly, string[] scriptArgs)
        {
            MethodInfo method = null;
            foreach (Module m in compiledAssembly.GetModules())
            {
                foreach (Type t in m.GetTypes())
                {
                    BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Static;
                    foreach (MemberInfo mi in t.GetMembers(bf))
                    {
                        if (mi.Name == "Main")
                        {
                            method = t.GetMethod(mi.Name, bf);
                        }
                        if (method != null)
                            break;
                    }
                    if (method != null)
                        break;
                }
                if (method != null)
                    break;
            }
            if (method != null)
            {
                object retval = null;
                if (method.GetParameters().Length != 0)
                    retval = method.Invoke(new object(), new object[] { (Object)scriptArgs });
                else
                    retval = method.Invoke(new object(), null);

                if (retval != null)
                {
                    try
                    {
                        Environment.ExitCode = int.Parse(retval.ToString());
                    }
                    catch { }
                }
            }
            else
            {
                throw new ApplicationException("Cannot find entry point. Make sure script file contains method: 'public static Main(...)'");
            }
        }
    }
}