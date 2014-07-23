#region Licence...

//-----------------------------------------------------------------------------
// Date:	24/01/13	Time: 9:00
// Module:	CSScriptLib.Eval.cs
// Classes:	CSScript
//			Evaluator
//
// This module contains the definition of the Evaluator class. Which wraps the common functionality
// of the Mono.CScript.Evaluator class (compiler as service)
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

using csscript;
using Mono.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using MCS = Mono.CSharp;

namespace CSScriptLibrary
{
    /// <summary>
    /// Type of the assemblies to be loaded/referenced.
    /// </summary>
    public enum DomainAssemblies
    {
        /// <summary>
        /// No assemblies
        /// </summary>
        None,

        /// <summary>
        /// All static current AppDomain assemblies
        /// </summary>
        AllStatic,

        /// <summary>
        /// All static and non-GAC current AppDomain assemblies
        /// </summary>
        AllStaticNonGAC,

        /// <summary>
        /// All current AppDomain assemblies
        /// </summary>
        All
    }

    /// <summary>
    /// Type of the build (compile) configuration
    /// </summary>
    public enum BuildConfiguration
    {
        /// <summary>
        /// The typical Debug build configuration
        /// </summary>
        Debug,

        /// <summary>
        /// The typical Release build configuration
        /// </summary>
        Release
    }

    /// <summary>
    /// A wrapper class that encapsulates the functionality of the Mono.CSharp.Evaluator.
    /// </summary>
    public class Evaluator
    {
        /// <summary>
        /// Gets or sets the compiler settings.
        /// </summary>
        /// <value>The compiler settings.</value>
        public CompilerSettings CompilerSettings { get; set; }

        /// <summary>
        /// Gets or sets the compiling result.
        /// </summary>
        /// <value>The compiling result.</value>
        public CompilingResult CompilingResult { get; set; }

        /// <summary>
        /// Gets or sets the flag indicating if the compilation error should throw an exception.
        /// </summary>
        /// <value>The throw on error.</value>
        public bool ThrowOnError { get; set; }

        /// <summary>
        /// Gets or sets the warnings as errors.
        /// </summary>
        /// <value>The warnings as errors.</value>
        public bool WarningsAsErrors { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Evaluator" /> class.
        /// </summary>
        public Evaluator()
        {
            ThrowOnError = true;
            Reset(true);
        }

        /// <summary>
        /// Gets or sets the flag for defining the conditional compiling symbol "DEBUG".
        /// </summary>
        /// <value>The flag indicating if the "DEBUG" symbol defined.</value>
        public bool IsDebugSymbolDefined
        {
            get
            {
                return ConditionalSymbols.Contains("DEBUG");
            }

            set
            {
                if (value)
                {
                    if (!ConditionalSymbols.Contains("DEBUG"))
                        ConditionalSymbols.Add("DEBUG");
                }
                else
                {
                    if (ConditionalSymbols.Contains("DEBUG"))
                        ConditionalSymbols.Remove("DEBUG");
                }
            }
        }

        BuildConfiguration configuration;

        /// <summary>
        /// Gets or sets the build configuration.
        /// </summary>
        /// <value>The configuration value.</value>
        public BuildConfiguration Configuration
        {
            get
            {
                return configuration;
            }

            set
            {
                configuration = value;
                IsDebugSymbolDefined = (value == BuildConfiguration.Debug);
                IsTraceSymbolDefined = (value == BuildConfiguration.Debug);
                CompilerSettings.GenerateDebugInfo = (value == BuildConfiguration.Debug);
            }
        }

        /// <summary>
        /// Gets or sets the flag for defining the conditional compiling symbol "TRACE".
        /// </summary>
        /// <value>The flag indicating if the "TRACE" symbol defined.</value>
        public bool IsTraceSymbolDefined
        {
            get
            {
                return ConditionalSymbols.Contains("TRACE");
            }

            set
            {
                if (value)
                {
                    if (!ConditionalSymbols.Contains("TRACE"))
                        ConditionalSymbols.Add("TRACE");
                }
                else
                {
                    if (ConditionalSymbols.Contains("TRACE"))
                        ConditionalSymbols.Remove("TRACE");
                }
            }
        }

#if net35
        /// <summary>
        /// Resets Evaluator.
        /// <para>
        /// The <see cref="T:Mono.CSharp.CompilerSettings"/> and <see cref="CompilingResult"/> are reinitialized.
        /// All reference assemblies are also cleared.
        /// </para>
        /// <para>The all current AppDomain assemblies will be referenced automatically.</para>
        /// </summary>
        public void Reset()
        {
            Reset(true);
        }
#endif
        /// <summary>
        /// Resets Evaluator.
        /// <para>
        /// The <see cref="T:Mono.CSharp.CompilerSettings"/> and <see cref="CompilingResult"/> are reinitialized.
        /// All reference assemblies are also cleared.
        /// </para>
        /// <para>Optionally the default current AppDomain assemblies can be referenced automatically.</para>
        /// </summary>
        /// <param name="referenceDomainAssemblies">if set to <c>true</c> the default assemblies of the current AppDomain
        /// will be referenced (see <see cref="ReferenceDomainAssemblies(DomainAssemblies)"/> method).
        /// </param>
#if net35
        public void Reset(bool referenceDomainAssemblies)
#else
        public void Reset(bool referenceDomainAssemblies = true)
#endif
        {
            CompilingResult = new CompilingResult();

            //This is how CompilerSettings supposed to be created if you don't
            //want non default settings:
            //var cmd = new CommandLineParser(new Report(CompilingResult));
            //CompilerSettings settings = cmd.ParseArguments(new string[] { "-debug" });
            //
            // Weird I know...
            //
            //Fortunately the defaults are OK.
            CompilerSettings = new CompilerSettings();
            service = new MCS.Evaluator(new CompilerContext(CompilerSettings, CompilingResult));

            if (referenceDomainAssemblies)
                ReferenceDomainAssemblies();
        }

#if net35
        /// <summary>
        /// References the all non-GAC domain assemblies.
        /// </summary>
        public void ReferenceDomainAssemblies() //if GAC assemblies are loaded the duplicated object definitions are reported even if CompilerSettings.LoadDefaultReferences = false
        {
            ReferenceDomainAssemblies(DomainAssemblies.AllStaticNonGAC);
        }
#endif
        /// <summary>
        /// References the domain assemblies.
        /// </summary>
        /// <param name="assemblies">The type of assemblies to be referenced.</param>
#if net35
        public Evaluator ReferenceDomainAssemblies(DomainAssemblies assemblies) //if GAC assemblies are loaded the duplicated object definitions are reported even if CompilerSettings.LoadDefaultReferences = false
#else
        public Evaluator ReferenceDomainAssemblies(DomainAssemblies assemblies = DomainAssemblies.AllStaticNonGAC) //if GAC assemblies are loaded the duplicated object definitions are reported even if CompilerSettings.LoadDefaultReferences = false
#endif
        {
            var relevantAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            if (assemblies == DomainAssemblies.AllStatic)
            {
                relevantAssemblies = relevantAssemblies.Where(x => !CSSUtils.IsDynamic(x)).ToArray();
            }
            else if (assemblies == DomainAssemblies.AllStaticNonGAC)
            {
                relevantAssemblies = relevantAssemblies.Where(x => !x.GlobalAssemblyCache && !CSSUtils.IsDynamic(x)).ToArray();
            }
            else if (assemblies == DomainAssemblies.None)
            {
                relevantAssemblies = new Assembly[0];
            }

            foreach (var asm in relevantAssemblies)
                ReferenceAssembly(asm);

            return this;
        }

        /// <summary>
        /// References the assembly.
        /// <para>It is safe to call this method multiple times
        /// for the same assembly. If the assembly already referenced it will not
        /// be referenced again.
        /// </para>
        /// </summary>
        /// <param name="assembly">The path to the assembly file.</param>
        /// <returns>The instance of the <see cref="T:CSScriptLibrary.Evluator"/>.</returns>
        public Evaluator ReferenceAssembly(string assembly)
        {
            ReferenceAssembly(Assembly.LoadFrom(assembly));
            return this;
        }

        /// <summary>
        /// References the assembly.
        /// <para>It is safe to call this method multiple times
        /// for the same assembly. If the assembly already referenced it will not
        /// be referenced again.
        /// </para>
        /// </summary>
        /// <param name="assembly">The assembly instance.</param>
        /// <returns>The instance of the <see cref="T:CSScriptLibrary.Evluator"/>.</returns>
        public Evaluator ReferenceAssembly(Assembly assembly)
        {
            if (!Assembly2Definition.ContainsKey(assembly))
                service.ReferenceAssembly(assembly);
            return this;
        }

        /// <summary>
        /// References the name of the assembly by its partial name.
        /// <para>Note that the referenced assembly will be loaded into the host AppDomain in order to resolve assembly partial name.</para>
        /// <para>It is an equivalent of <code>Evaluator.ReferenceAssembly(Assembly.LoadWithPartialName(assemblyPartialName))</code></para>
        /// </summary>
        /// <param name="assemblyPartialName">Partial name of the assembly.</param>
        /// <returns>The instance of the <see cref="T:CSScriptLibrary.Evluator"/>.</returns>
        public Evaluator ReferenceAssemblyByName(string assemblyPartialName)
        {
            return ReferenceAssembly(Assembly.LoadWithPartialName(assemblyPartialName));
        }

        /// <summary>
        /// References the assembly by the object, which belongs to this assembly.
        /// <para>It is safe to call this method multiple times
        /// for the same assembly. If the assembly already referenced it will not
        /// be referenced again.
        /// </para>
        /// </summary>
        /// <param name="obj">The object, which belongs to the assembly to be referenced.</param>
        /// <returns>The instance of the <see cref="T:CSScriptLibrary.Evluator"/>.</returns>
        public Evaluator ReferenceAssemblyOf(object obj)
        {
            ReferenceAssembly(obj.GetType().Assembly);
            return this;
        }

        /// <summary>
        /// References the assembly by the namespace it implements.
        /// </summary>
        /// <param name="namespace">The namespace.</param>
        /// <returns><c>true</c> if namespace was successfully resolved and
        /// the reference was added; otherwise, <c>false</c>.</returns>
        public bool ReferenceAssemblyByNamespace(string @namespace)
        {
            bool retval = false;
            foreach (string asm in AssemblyResolver.FindGlobalAssembly(@namespace))
            {
                retval = true;
                ReferenceAssembly(asm);
            }
            return retval;
        }

        /// <summary>
        /// References the assemblies from the script code.
        /// </summary>
        /// <param name="code">The script code.</param>
        /// <param name="searchDirs">The assembly search/probing directories.</param>
        /// <returns>The instance of the <see cref="T:CSScriptLibrary.Evluator"/>.</returns>
        public Evaluator ReferenceAssembliesFromCode(string code, params string[] searchDirs)
        {
            foreach (var asm in GetReferencedAssemblies(code, searchDirs))
                ReferenceAssembly(asm);
            return this;
        }

        /// <summary>
        /// References the assembly by the object, which belongs to this assembly.
        /// <para>It is safe to call this method multiple times
        /// for the same assembly. If the assembly already referenced it will not
        /// be referenced again.
        /// </para>
        /// </summary>
        /// <typeparam name="T">Te type which is implemented in the assembly to be referenced.</typeparam>
        /// <returns>The instance of the <see cref="T:CSScriptLibrary.Evluator"/>.</returns>
        public Evaluator ReferenceAssemblyOf<T>()
        {
            return ReferenceAssembly(typeof(T).Assembly);
        }

        ReflectionImporter Importer
        {
            get
            {
                FieldInfo info = service.GetType().GetField("importer", BindingFlags.Instance | BindingFlags.NonPublic);
                return (ReflectionImporter)info.GetValue(service);
            }
        }

        List<string> ConditionalSymbols
        {
            get
            {
                FieldInfo info = CompilerSettings.GetType().GetField("conditional_symbols", BindingFlags.Instance | BindingFlags.NonPublic);
                return (List<string>)info.GetValue(CompilerSettings);
            }
        }

        Dictionary<Assembly, IAssemblyDefinition> Assembly2Definition
        {
            get
            {
                FieldInfo info = Importer.GetType().GetField("assembly_2_definition", BindingFlags.Instance | BindingFlags.NonPublic);
                return (Dictionary<Assembly, IAssemblyDefinition>)info.GetValue(Importer);
            }
        }

        /// <summary>
        /// Evaluates C# code and loads (returns instance) the first class defined in the code to the current AppDomain.
        /// </summary>
        /// <example>The following is the simple example of the LoadCode usage:
        ///<code>
        /// dynamic script = CSScript.Evaluator
        ///                          .LoadCode(@"using System;
        ///                                      public class Script
        ///                                      {
        ///                                          public int Sum(int a, int b)
        ///                                          {
        ///                                              return a+b;
        ///                                          }
        ///                                      }");
        /// int result = script.Sum(1, 2);
        /// </code>
        /// </example>
        /// <param name="scriptText">The C# script text.</param>
        /// <returns>Instance of the class defined in the script.</returns>
        public object LoadCode(string scriptText)
        {
            //Starting with from Mono v3.3.0 Mono.CSharp.Evaluator does not 
            //return compiled class reliably (as the first '*' type).
            //This is because Evaluator now injects "<InteractiveExpressionClass>" as the first class.

            return CompileCode(scriptText)
                      .GetCompiledAssembly()
                      .CreateObject("*");
        }

        /// <summary>
        /// Evaluates C# file from the specified file and loads (returns instance) the first class defined in the script file
        /// to the current AppDomain.
        /// </summary>
        /// <example>The following is the simple example of the interface alignment:
        ///<code>
        /// dynamic script = CSScript.Evaluator
        ///                          .LoadFile("calc.cs");
        /// int result = script.Sum(1, 2);
        /// </code>
        /// </example>/// <param name="scriptFile">The C# script file.</param>
        /// <returns>Instance of the class defined in the script file.</returns>
        public object LoadFile(string scriptFile)
        {
            return LoadCode(File.ReadAllText(scriptFile));
        }

        /// <summary>
        /// Evaluates C# code and loads (returns instance) the first class defined in the code to the current AppDomain.
        /// After initializing the class instance it is aligned to the interface specified by the parameter <c>T</c>.
        /// <para><c>Note:</c> the script class does not have to inherit from the <c>T</c> parameter as the proxy type
        /// will be generated anyway.</para>
        /// </summary>
        /// <example>The following is the simple example of the interface alignment:
        ///<code>
        /// public interface ICalc
        /// {
        ///     int Sum(int a, int b);
        /// }
        /// ....
        /// ICalc calc = CSScript.Evaluator
        ///                      .LoadCode&lt;ICalc&gt;(@"using System;
        ///                                         public class Script
        ///                                         {
        ///                                             public int Sum(int a, int b)
        ///                                             {
        ///                                                 return a+b;
        ///                                             }
        ///                                         }");
        /// int result = calc.Sum(1, 2);
        /// </code>
        /// </example>
        /// <typeparam name="T">The type of the interface type the script class instance should be aligned to.</typeparam>
        /// <param name="scriptText">The C# script text.</param>
        /// <returns>Aligned to the <c>T</c> interface instance of the class defined in the script.</returns>
        public T LoadCode<T>(string scriptText) where T : class
        {
            var script = LoadCode(scriptText);

            string type = "";
            string proxyClass = script.BuildAlignToInterfaceCode<T>(out type);

            CompileCode(proxyClass);
            var proxyType = GetCompiledType(type);

            return (T)Activator.CreateInstance(proxyType, script);
        }

        /// <summary>
        /// Gets referenced assemblies from the script code.
        /// </summary>
        /// <param name="code">The script code.</param>
        /// <param name="searchDirs">The assembly search/probing directories.</param>
        /// <returns>Array of the referenced assemblies</returns>
        public string[] GetReferencedAssemblies(string code, params string[] searchDirs)
        {
            var retval = new List<string>();

            var dirs = searchDirs.Concat(new string[] { Path.GetDirectoryName(Assembly.GetCallingAssembly().Location) }).ToArray();

            var parser = new csscript.CSharpParser(code);

            foreach (var asm in parser.RefAssemblies.Concat(parser.RefNamespaces))
                foreach (string asmFile in AssemblyResolver.FindAssembly(asm, dirs))
                    retval.Add(asmFile);

            return retval.Distinct().ToArray();
        }

        /// <summary>
        /// Evaluates C# file and loads (returns instance) the first class defined in the script file to the current AppDomain.
        /// After initializing the class instance it is aligned to the interface specified by the parameter <c>T</c>.
        /// <para><c>Note:</c> the script class does not have to inherit from the <c>T</c> parameter as the proxy type
        /// will be generated anyway.</para>
        /// </summary>
        /// <example>The following is the simple example of the interface alignment:
        ///<code>
        /// public interface ICalc
        /// {
        ///     int Sum(int a, int b);
        /// }
        /// ....
        /// ICalc calc = CSScript.Evaluator
        ///                      .LoadFile&lt;ICalc&gt;("calc.cs");
        /// int result = calc.Sum(1, 2);
        /// </code>
        /// </example>
        /// <typeparam name="T">The type of the interface type the script class instance should be aligned to.</typeparam>
        /// <param name="scriptFile">The C# script text.</param>
        /// <returns>Aligned to the <c>T</c> interface instance of the class defined in the script file.</returns>
        public T LoadFile<T>(string scriptFile) where T : class
        {
            return LoadCode<T>(File.ReadAllText(scriptFile));
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>Scripting.DynamicClass</c>), evaluates it and loads (returns instance) the class to the current AppDomain.
        /// </summary>
        /// <example>The following is the simple example of the LoadMethod usage:
        /// <code>
        /// dynamic script = CSScript.Evaluator
        ///                          .LoadMethod(@"int Product(int a, int b)
        ///                                        {
        ///                                            return a * b;
        ///                                        }");
        ///
        /// int result = script.Product(3, 2);
        /// </code>
        /// </example>
        /// <param name="code">The C# script text.</param>
        /// <returns>Instance of the first class defined in the script.</returns>
        public object LoadMethod(string code)
        {
            string scriptText = CSScript.WrapMethodToAutoClass(code, false);

            return LoadCode(scriptText);
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>Scripting.DynamicClass</c>), evaluates it and loads (returns instance) the class to the current AppDomain.
        /// <para>
        /// After initializing the class instance it is aligned to the interface specified by the parameter <c>T</c>.
        /// </para>
        /// </summary>
        /// <example>The following is the simple example of the interface alignment:
        /// <code>
        /// public interface ICalc
        /// {
        ///     int Sum(int a, int b);
        ///     int Div(int a, int b);
        /// }
        /// ....
        /// ICalc script = CSScript.Evaluator
        ///                        .LoadMethode&lt;ICalc&gt;(@"public int Sum(int a, int b)
        ///                                              {
        ///                                                  return a + b;
        ///                                              }
        ///                                              public int Div(int a, int b)
        ///                                              {
        ///                                                  return a/b;
        ///                                              }");
        /// int result = script.Div(15, 3);
        /// </code>
        /// </example>
        /// <typeparam name="T">The type of the interface type the script class instance should be aligned to.</typeparam>
        /// <param name="code">The C# script text.</param>
        /// <returns>Aligned to the <c>T</c> interface instance of the auto-generated class defined in the script.</returns>
        public T LoadMethod<T>(string code) where T : class
        {
            string scriptText = CSScript.WrapMethodToAutoClass(code, false);

            return LoadCode<T>(scriptText);
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>Scripting.DynamicClass</c>), evaluates it and loads the class to the current AppDomain.
        /// <para>Returns instance of <c>T</c> delegate for the first method in the auto-generated class.</para>
        /// </summary>
        ///  <example>The following is the simple example of the interface alignment:
        /// <code>
        /// var Product = CSScript.Evaluator.LoadDelegate&lt;Func&lt;int, int, int&gt;&gt;(
        ///                                 @"int Product(int a, int b)
        ///                                   {
        ///                                       return a * b;
        ///                                   }");
        ///
        /// int result = Product(3, 2);
        /// </code>
        /// </example>
        /// <param name="code">The C# code.</param>
        /// <returns>Instance of <c>T</c> delegate.</returns>
        public MethodDelegate CreateDelegate(string code)
        {
            string scriptText = CSScript.WrapMethodToAutoClass(code, true);
            CompileCode(scriptText);
            return GetCompiledAssembly().GetStaticMethod();
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>Scripting.DynamicClass</c>), evaluates it and loads the class to the current AppDomain.
        /// <para>Returns <see cref="T:CSScriptLibrary.MethodDelegate"/> for class-less style of invoking.</para>
        /// </summary>
        ///  <example>The following is the simple example of the interface alignment:
        /// <code>
        /// var Product = CSScript.Evaluator
        ///                       .CreateDelegate(@"int Product(int a, int b)
        ///                                         {
        ///                                             return a * b;
        ///                                         }");
        ///
        /// int result = (int)Product(3, 2);
        /// </code>
        /// </example>
        /// <param name="code">The C# code.</param>
        /// <returns></returns>
        public T LoadDelegate<T>(string code) where T : class
        {
            string scriptText = CSScript.WrapMethodToAutoClass(code, true);
            Assembly asm = CompileCode(scriptText).GetCompiledAssembly();
            var method = asm.GetType("Scripting.DynamicClass").GetMethods().First();
            return System.Delegate.CreateDelegate(typeof(T), method) as T;
        }

        /// <summary>
        /// Wraps C# code fragment into auto-generated class (type name <c>Scripting.DynamicClass</c>), evaluates it.
        /// <para>
        /// Returns instance of the <see cref="T:CSScriptLibrary.Evluator"/> for further access to the compilation result.</para>
        /// </summary>
        /// <param name="code">The C# code.</param>
        /// <returns>The instance of the <see cref="T:CSScriptLibrary.Evluator"/>.</returns>
        public Evaluator CompileMethod(string code)
        {
            string scriptText = CSScript.WrapMethodToAutoClass(code, false);
            CompileCode(scriptText);
            return this;
        }

        /// <summary>
        /// Gets the assembly compiled with the last Compile*/Load* call.
        /// </summary>
        /// <returns>Instance of the <see cref="T:System:Reflection.Assembly"/>.</returns>
        public Assembly GetCompiledAssembly()
        {
            return ((Type)service.Evaluate(ConnectionPointGetTypeExpression)).Assembly;
        }

        const string ConnectionPointClassName = "CSS_ConnectionPoint";
        const string ConnectionPointClassDeclaration = "\n public struct CSS_ConnectionPoint {}";
        const string ConnectionPointGetTypeExpression = "typeof(CSS_ConnectionPoint);";

        /// <summary>
        /// Gets a type from the last Compile/Evaluate/Load call.
        /// </summary>
        /// <param name="type">The type name.</param>
        /// <returns>The type instance</returns>
        public Type GetCompiledType(string type)
        {
            return (Type)service.Evaluate("typeof(" + type + ");");
        }

        /// <summary>
        /// Evaluates (compiles) C# code.
        /// </summary>
        /// <param name="scriptText">The C# script text.</param>
        /// <returns>The instance of the <see cref="T:CSScriptLibrary.Evluator"/>.</returns>
        public Evaluator CompileCode(string scriptText)
        {
            HandleCompilingErrors(() =>
            {
                var method = service.Compile(scriptText + ConnectionPointClassDeclaration);
                //cannot rely on 'method' as it is null in CS-Script scenarios 
            });
            return this;
        }

        MCS.Evaluator service;

        /// <summary>
        /// Evaluates the specified C# statement and returns the result of the execution.
        /// </summary>
        /// <example>
        /// <code>
        /// string upperCaseText = (string)CSScript.Evaluator.Evaluate("\"Hello\".ToUpper();");
        /// int sum = (int)CSScript.Evaluator.Evaluate("1+2;");
        /// </code>
        /// </example>
        /// <param name="scriptText">The C# statement.</param>
        /// <returns>Result of the evaluation (execution).</returns>
        public object Evaluate(string scriptText)
        {
            object retval = null;

            HandleCompilingErrors(() =>
                {
                    retval = service.Evaluate(scriptText);
                });

            return retval;
        }

        /// <summary>
        /// Evaluates the specified C# statement. The statement must be "void" (returning no result).
        /// </summary>
        /// <example>
        /// <code>
        /// CSScript.Evaluator.Run("using System;");
        /// CSScript.Evaluator.Run("Console.WriteLine(\"Hello World!\");");
        /// </code>
        /// </example>
        /// <param name="scriptText">The C# statement.</param>
        public void Run(string scriptText)
        {
            HandleCompilingErrors(() =>
                {
                    service.Run(scriptText);
                });
        }

        void HandleCompilingErrors(Action action)
        {
            CompilingResult.Reset();

            try
            {
                action();
            }
            catch (Exception)
            {
                if (!CompilingResult.HasErrors)
                {
                    throw;
                }
                else
                {
                    //The exception is most likely related to te compilation error
                    //so do noting. Alternatively (ay be in the future) we can add
                    //it to the errors collection.
                    //CompilingResult.Errors.Add(e.ToString());
                }
            }

            if (ThrowOnError)
            {
                if (CompilingResult.HasErrors || (WarningsAsErrors && CompilingResult.HasWarnings))
                {
                    throw CompilingResult.CreateException();
                }
            }
        }

        /// <summary>
        /// Gets the underlying <see cref="T:Mono.CSharp.Evaluator"/>.It is the actual Mono "compiler as service".
        /// </summary>
        /// <returns>Instance of <see cref="T:Mono.CSharp.Evaluator"/>.</returns>
        public MCS.Evaluator GetService()
        {
            return service;
        }
    }

    public partial class CSScript
    {
        static Evaluator evaluator;

        /// <summary>
        /// Gets the <see cref="T:CSScriptLibrary.Evaluator"/> instance. This object is to be used for
        /// dynamic loading of the  C# code by using Mono "compiler as service".
        /// <para>For the majority of the CS-Script script engine hosting scenarios the Mono compiler
        /// is a preferred runtime. The major advantage is the compilation speed and superior (comparing to CodeDOM)
        /// memory management.</para>
        /// </summary>
        /// <value>
        /// The <see cref="T:CSScriptLibrary.Evaluator"/> instance.
        /// </value>
        static public Evaluator Evaluator
        {
            get
            {
                if (evaluator == null)
                {
                    evaluator = new Evaluator();
                }
                return evaluator;
            }
        }
    }

    /// <summary>
    /// Custom implementation of <see cref="T:Mono.CSharp.ReportPrinter"/> required by
    /// <see cref="T:Mono.CSharp"/> API model for handling (reporting) compilation errors.
    /// <para><see cref="T:Mono.CSharp"/> default compiling error reporting (e.g. <see cref="T:Mono.CSharp.ConsoleReportPrinter"/>)
    /// is not dev-friendly, thus <c>CompilingResult</c> is acting as an adapter bringing the Mono API close to the
    /// traditional CodeDOM error reporting model.</para>
    /// </summary>
    public class CompilingResult : ReportPrinter
    {
        /// <summary>
        /// The collection of compiling errors.
        /// </summary>
        public List<string> Errors = new List<string>();

        /// <summary>
        /// The collection of compiling warnings.
        /// </summary>
        public List<string> Warnings = new List<string>();

        /// <summary>
        /// Indicates if the last compilation yielded any errors.
        /// </summary>
        /// <value>If set to <c>true</c> indicates presence of compilation error(s).</value>
        public bool HasErrors
        {
            get
            {
                return Errors.Count > 0;
            }
        }

        /// <summary>
        /// Indicates if the last compilation yielded any warnings.
        /// </summary>
        /// <value>If set to <c>true</c> indicates presence of compilation warning(s).</value>
        public bool HasWarnings
        {
            get
            {
                return Warnings.Count > 0;
            }
        }

#if net35
        /// <summary>
        /// Creates the <see cref="T:System.Exception"/> containing combined error information.
        /// </summary>
        /// <returns>Instance of the <see cref="CompilerException"/>.</returns>
        public CompilerException CreateException()
        {
            return CreateException(false);
        }
#endif
        /// <summary>
        /// Creates the <see cref="T:System.Exception"/> containing combined error information.
        /// Optionally warnings can also be included in the exception info.
        /// </summary>
        /// <param name="hideCompilerWarnings">The flag indicating if compiler warnings should be included in the error (<see cref="T:System.Exception"/>) info.</param>
        /// <returns>Instance of the <see cref="CompilerException"/>.</returns>
#if net35
        public CompilerException CreateException(bool hideCompilerWarnings)
#else
        public CompilerException CreateException(bool hideCompilerWarnings = false)
#endif
        {
            var compileErr = new StringBuilder();
            foreach (string err in Errors)
                compileErr.AppendLine(err);

            if (!hideCompilerWarnings)
                foreach (string item in Warnings)
                    compileErr.AppendLine(item);

            CompilerException retval = new CompilerException(compileErr.ToString());

            retval.Data.Add("Errors", Errors);

            if (!hideCompilerWarnings)
                retval.Data.Add("Warnings", Warnings);

            return retval;
        }

        /// <summary>
        /// Clears all errors and warnings.
        /// </summary>
        public new void Reset()
        {
            Errors.Clear();
            Warnings.Clear();
            base.Reset();
        }

        /// <summary>
        /// Handles compilation event message.
        /// </summary>
        /// <param name="msg">The compilation event message.</param>
        /// <param name="showFullPath">if set to <c>true</c> [show full path].</param>
        public override void Print(Mono.CSharp.AbstractMessage msg, bool showFullPath)
        {
            string msgInfo = string.Format("{0} {1} CS{2:0000}: {3}", msg.Location, msg.MessageType, msg.Code, msg.Text);
            if (!msg.IsWarning)
            {
                Errors.Add(msgInfo);
            }
            else
            {
                Warnings.Add(msgInfo);
            }
        }
    }
}