@set oldPATH=%PATH%;
@set PATH=%windir%\Microsoft.NET\Framework\v1.1.4322;%PATH%;
@set net4_tools=C:\Program Files (x86)\MSBuild\12.0\Bin
@set net451_tools=%windir%\Microsoft.NET\Framework\v4.0.30319

@set net4_asms=C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5

ECHO off
ECHO Preparing to build...

ECHO You may need to adjust this file to exclude not needed projects and file copying actions  
ECHO Add /debug+ /debug:full args to do a debug build

del *.exe
del *.dll
del *.dll.unsigned

MD temp
MD temp\temp
copy ..\CSScriptLibrary\sgKey.snk sgKey.snk

if exist build.log del build.log

@set common_4_params=/noconfig /nostdlib+ /r:"%net4_asms%\System.Design.dll" /r:"%net4_asms%\System.Drawing.dll" /r:"%net4_asms%\mscorlib.dll"  

@set common_source_files=..\GACHelper.cs ..\fileparser.cs ..\Precompiler.cs ..\csscript.cs ..\csparser.cs ..\AssemblyResolver.cs ..\AssemblyExecutor.cs  ..\Exceptions.cs ..\ExecuteOptions.cs ..\ScriptLauncherBuilder.cs ..\Settings.cs ..\Utils.cs 

ECHO Building...
ECHO Building cscs.exe: 
ECHO Building cscs.exe: >> build.log
"%net4_tools%\csc.exe" /nologo /nowarn:169,618 /o /define:fork_x86,net4 /out:cscs.exe /t:exe %common_source_files% ..\cscscript\CSExecutionClient.cs ..\cscscript\AssemblyInfo.cs /win32icon:..\Logo\css_logo.ico  %common_4_params% /r:"%net4_asms%\System.dll" /r:"%net4_asms%\System.Data.dll" /r:"%net4_asms%\System.XML.dll" /r:"%net4_asms%\System.Windows.Forms.dll" /r:"%net4_asms%\System.Core.dll" >> build.log
ECHO ------------ >> build.log

ECHO Building...
ECHO Building cscs32.exe: 
ECHO Building cscs32.exe: >> build.log
"%net4_tools%\csc.exe" /nologo /nowarn:169,618 /o /platform:x86 /define:net4 /out:cscs32.exe /t:exe %common_source_files% ..\cscscript\CSExecutionClient.cs ..\cscscript\AssemblyInfo.cs /win32icon:..\Logo\css_logo.ico  %common_4_params% /r:"%net4_asms%\System.dll" /r:"%net4_asms%\System.Data.dll" /r:"%net4_asms%\System.XML.dll" /r:"%net4_asms%\System.Windows.Forms.dll" /r:"%net4_asms%\System.Core.dll" >> build.log
ECHO ------------ >> build.log

ECHO Building csws.exe:
ECHO Building csws.exe: >> build.log
"%net4_tools%\csc.exe"/nologo /nowarn:169,618 /o /define:fork_x86,net4 /out:csws.exe /t:winexe %common_source_files% ..\cswscript\CSExecutorClient.cs ..\cswscript\AssemblyInfo.cs /win32icon:..\Logo\css_logo.ico  %common_4_params%  /r:"%net4_asms%\System.dll" /r:"%net4_asms%\System.Data.dll" /r:"%net4_asms%\System.XML.dll" /r:"%net4_asms%\System.Core.dll" /r:"%net4_asms%\System.Windows.Forms.dll" >> build.log
ECHO ------------ >> build.log

ECHO Building csws32.exe:
ECHO Building csws32.exe: >> build.log
"%net4_tools%\csc.exe" /nologo /nowarn:169,618 /o /platform:x86 /define:net4 /out:csws32.exe /t:winexe %common_source_files% ..\cswscript\CSExecutorClient.cs ..\cswscript\AssemblyInfo.cs %common_4_params% /win32icon:..\Logo\css_logo.ico  /r:"%net4_asms%\System.dll" /r:"%net4_asms%\System.Data.dll" /r:"%net4_asms%\System.XML.dll" /r:"%net4_asms%\System.Core.dll" /r:"%net4_asms%\System.Windows.Forms.dll" >> build.log
ECHO ------------ >> build.log

ECHO Building cscs.v3.5.exe: 
ECHO Building cscs.v3.5.exe: >> build.log
%windir%\Microsoft.NET\Framework\v3.5\csc /nologo /nowarn:169,618 /o /define:net35 /out:cscs.v3.5.exe /t:exe %common_source_files% ..\cscscript\CSExecutionClient.cs ..\cscscript\AssemblyInfo.cs /win32icon:..\Logo\css_logo.ico  /r:System.dll /r:System.Data.dll /r:System.XML.dll /r:System.Windows.Forms.dll /r:System.Core.dll >> build.log
ECHO ------------ >> build.log

ECHO Building csws.v3.5.exe:
ECHO Building csws.v3.5.exe: >> build.log
%windir%\Microsoft.NET\Framework\v3.5\csc /nologo /nowarn:169,618 /o /define:net35 /out:csws.v3.5.exe /t:winexe %common_source_files% ..\cswscript\CSExecutorClient.cs ..\cswscript\AssemblyInfo.cs /win32icon:..\Logo\css_logo.ico  /r:System.dll /r:System.Data.dll /r:System.XML.dll /r:System.Core.dll /r:System.Windows.Forms.dll >> build.log
ECHO ------------ >> build.log

ECHO Building css_config.exe: 
ECHO Building css_config.exe: >> ..\Build\build.log
"%net4_tools%\csc.exe" /nologo %common_4_params% /unsafe- /nowarn:1701,1702 /o /out:css_config.exe /win32manifest:..\ChooseDefaultProgram\app.manifest /win32icon:..\Logo\css_logo.ico /resource:..\css_config\SplashScreen.resources /target:winexe ..\css_config\AssemblyInfo.cs ..\css_config\css_config.cs ..\css_config\Program.cs ..\css_config\SplashForm.cs ..\css_config\VistaSecurity.cs /r:"%net4_asms%\System.dll" /r:"%net4_asms%\System.Drawing.dll" /r:"%net4_asms%\System.Core.dll" /r:"%net4_asms%\System.Data.dll" /r:"%net4_asms%\System.XML.dll" /r:"%net4_asms%\System.Windows.Forms.dll" >> build.log
ECHO ------------ >> build.log

ECHO Building CS-Script.exe: 
ECHO Building CS-Script.exe: >> ..\Build\build.log
"%net4_tools%\csc.exe" /nologo %common_4_params%  /unsafe- /nowarn:1701,1702 /o /out:CS-Script.exe /resource:..\CS-Script\CSScript.Properties.Resources.resources /target:winexe /win32icon:..\Logo\css_logo.ico ..\CS-Script\Properties\AssemblyInfo.cs ..\CS-Script\Program.cs /r:"%net4_asms%\System.dll" /r:"%net4_asms%\System.Drawing.dll" /r:"%net4_asms%\System.Core.dll" /r:"%net4_asms%\System.Data.dll" /r:"%net4_asms%\System.XML.dll" /r:"%net4_asms%\System.Windows.Forms.dll" >> build.log
ECHO ------------ >> build.log

ECHO Building ChooseDefaultProgram.exe: 
ECHO Building ChooseDefaultProgram.exe: >> ..\Build\build.log
"%net4_tools%\csc.exe" /nologo %common_4_params%  /unsafe- /nowarn:1701,1702 /o /out:ChooseDefaultProgram.exe /win32manifest:..\ChooseDefaultProgram\app.manifest /resource:..\ChooseDefaultProgram\CSScript.Resources.resources /target:winexe /win32icon:..\Logo\css_logo.ico  ..\ChooseDefaultProgram\Resources.Designer.cs ..\ChooseDefaultProgram\AssemblyInfo.cs ..\ChooseDefaultProgram\ChooseDefaultProgram.cs /r:"%net4_asms%\System.dll" /r:"%net4_asms%\System.Core.dll" /r:"%net4_asms%\System.Data.dll" /r:"%net4_asms%\System.XML.dll" /r:"%net4_asms%\System.Windows.Forms.dll" >> build.log
ECHO ------------ >> build.log

ECHO Building cscs.exe (v1.1):
ECHO Building cscs.exe (v1.1): >> build.log
%windir%\Microsoft.NET\Framework\v2.0.50727\csc /nologo /nowarn:169,618,1699 /define:net1 /o /out:cscs.v1.1.exe /t:exe %common_source_files% ..\cscscript\CSExecutionClient.cs ..\cscscript\AssemblyInfo.cs /win32icon:..\Logo\css_logo.ico  /r:System.dll /r:System.Data.dll /r:System.XML.dll /r:System.Windows.Forms.dll >> build.log
ECHO ------------ >> build.log

ECHO Building csws.exe (v1.1):
ECHO Building csws.exe (v1.1): >> build.log
%windir%\Microsoft.NET\Framework\v2.0.50727\csc /nologo /nowarn:169,618,1699 /define:net1 /o /out:csws.v1.1.exe /t:winexe %common_source_files% ..\cswscript\CSExecutorClient.cs ..\cswscript\AssemblyInfo.cs /win32icon:..\Logo\css_logo.ico  /r:System.dll /r:System.Data.dll /r:System.XML.dll /r:System.Windows.Forms.dll >> build.log
ECHO ------------ >> build.log

cd ..\CSScriptLibrary
ECHO Building CSScriptLibrary.v1.1.dll: 
ECHO Building CSScriptLibrary.v1.1.dll: >> ..\Build\build.log
%windir%\Microsoft.NET\Framework\v2.0.50727\csc /nologo /nowarn:169,618,1699 /define:net1 /o /doc:..\Build\temp\temp\CSScriptLibrary.v1.1.xml /out:..\Build\temp\temp\CSScriptLibrary.v1.1.dll /t:library %common_source_files% CSScriptLib.cs crc32.cs AsmHelper.cs AssemblyInfo.cs  /r:System.dll /r:System.Data.dll /r:System.XML.dll /r:System.Windows.Forms.dll >> ..\Build\build.log
rem move ..\Build\temp\temp\CSScriptLibrary.dll ..\Build\temp\temp\CSScriptLibrary.v1.1.dll
ECHO ------------ >> ..\Build\build.log

ECHO Building CSScriptLibrary.v3.5.dll: 
ECHO Building CSScriptLibrary.v3.5.dll: >> ..\Build\build.log
%windir%\Microsoft.NET\Framework\v3.5\csc /nologo /nowarn:169,1699,618 /define:net35 /o /doc:..\Build\temp\temp\CSScriptLibrary.v3.5.xml /out:..\Build\temp\temp\CSScriptLibrary.v3.5.dll /t:library %common_source_files% CSScriptLib.cs AsmHelper.cs ObjectCaster.cs AssemblyInfo.cs crc32.cs /r:System.dll /r:System.Data.dll /r:System.XML.dll /r:System.Windows.Forms.dll >> ..\Build\build.log
rem move ..\Build\temp\temp\CSScriptLibrary.dll ..\Build\temp\temp\CSScriptLibrary.v3.5.dll
ECHO ------------ >> ..\Build\build.log

ECHO Building CSScriptLibrary.v3.5.dll (renamed): 
ECHO Building CSScriptLibrary.v3.5.dll (renamed): >> ..\Build\build.log
%windir%\Microsoft.NET\Framework\v3.5\csc /nologo /nowarn:169,1699,618 /define:net35 /o /doc:CSScriptLibrary.xml /out:..\Build\temp\temp\CSScriptLibrary.dll /t:library %common_source_files% CSScriptLib.cs AsmHelper.cs ObjectCaster.cs AssemblyInfo.cs crc32.cs /r:System.dll /r:System.Data.dll /r:System.XML.dll /r:System.Windows.Forms.dll >> ..\Build\build.log
ECHO ------------ >> ..\Build\build.log

ECHO Building CSScriptLibrary.v4.0.dll: 
ECHO Building CSScriptLibrary.v4.0.dll: >> ..\Build\build.log
"%net4_tools%\msbuild.exe" ..\CSScriptLibrary\CSScriptLibrary.v4.0.csproj /t:Rebuild /p:configuration=Release /verbosity:quiet
ECHO ------------ >> ..\Build\build.log

ECHO Building CSScriptLibrary.dll (unsigned): 
ECHO Building CSScriptLibrary.dll (unsigned): >> ..\Build\build.log
"%net4_tools%\csc.exe" /nologo %common_4_params%  /nowarn:169,1699,618 /define:CSSLib_BuildUnsigned /o /doc:CSScriptLibrary.xml /out:..\Build\temp\temp\CSScriptLibrary.dll /t:library %common_source_files% CSScriptLib.cs crc32.cs AsmHelper.cs ObjectCaster.cs AssemblyInfo.cs CSScriptLib.Eval.cs /r:..\Mono.CSharp.dll /r:"%net4_asms%\System.dll" /r:"%net4_asms%\System.Data.dll" /r:"%net4_asms%\System.Core.dll"  /r:"%net4_asms%\System.XML.dll" /r:"%net4_asms%\System.Windows.Forms.dll" >> ..\Build\build.log
ECHO ------------ >> ..\Build\build.log
move ..\Build\temp\temp\CSScriptLibrary.dll ..\Build\temp\temp\CSScriptLibrary.dll.unsigned

ECHO Building CSScriptLibrary.dll: 
ECHO Building CSScriptLibrary.dll: >> ..\Build\build.log
"%net4_tools%\csc.exe" /nologo %common_4_params%  /nowarn:169,1699,618 /o /doc:CSScriptLibrary.xml /out:..\Build\temp\temp\CSScriptLibrary.dll /t:library %common_source_files% CSScriptLib.cs AsmHelper.cs ObjectCaster.cs AssemblyInfo.cs crc32.cs CSScriptLib.Eval.cs /r:..\Mono.CSharp.dll /r:"%net4_asms%\System.dll" /r:"%net4_asms%\System.Data.dll" /r:"%net4_asms%\System.XML.dll" /r:"%net4_asms%\System.Core.dll" /r:"%net4_asms%\System.Windows.Forms.dll" >> ..\Build\build.log
ECHO ------------ >> ..\Build\build.log

ECHO Building ConfigConsole.exe: 
ECHO Building ConfigConsole.exe: >> ..\Build\build.log
cscs.exe /l /ew "..\..\Lib\ConfigConsole\ConfigConsole.cs"
ECHO ------------ >> ..\Build\build.log

cd ..\Build

move temp\temp\CSScriptLibrary.dll.unsigned CSScriptLibrary.dll.unsigned
move temp\temp\CSScriptLibrary.dll CSScriptLibrary.dll
move ..\CSScriptLibrary\CSScriptLibrary.xml CSScriptLibrary.xml
move temp\temp\CSScriptLibrary.v1.1.xml CSScriptLibrary.v1.1.xml
move temp\temp\CSScriptLibrary.v3.5.xml CSScriptLibrary.v3.5.xml
move temp\temp\CSScriptLibrary.v1.1.dll CSScriptLibrary.v1.1.dll
move temp\temp\CSScriptLibrary.v3.5.dll CSScriptLibrary.v3.5.dll
copy cscs.v1.1.exe "..\..\Lib\Bin\NET 1.1\cscs.exe"
copy csws.v1.1.exe "..\..\Lib\Bin\NET 1.1\csws.exe"
copy CSScriptLibrary.v1.1.xml "..\..\Lib\Bin\NET 1.1\CSScriptLibrary.v1.1.xml"
copy CSScriptLibrary.v1.1.dll "..\..\Lib\Bin\NET 1.1\CSScriptLibrary.v1.1.dll"
copy cscs.v3.5.exe "..\..\Lib\Bin\NET 3.5\cscs.exe"
copy csws.v3.5.exe "..\..\Lib\Bin\NET 3.5\csws.exe"
copy cscs.exe "..\..\Lib\Bin\Linux\cscs"
copy CSScriptLibrary.v3.5.xml "..\..\Lib\Bin\NET 3.5\CSScriptLibrary.v3.5.xml"
copy CSScriptLibrary.v3.5.dll "..\..\Lib\Bin\NET 3.5\CSScriptLibrary.v3.5.dll"
copy CSScriptLibrary.v3.5.dll "..\..\Samples\Hosting\CodeDOM\Older versions of CLR\CSScriptLibrary.v3.5.dll"
copy CSScriptLibrary.v3.5.dll "..\..\Samples\Hosting\CodeDOM\VS2008 project\Lib\CSScriptLibrary.v3.5.dll"
copy CSScriptLibrary.v3.5.xml "..\..\Samples\Hosting\CodeDOM\VS2008 project\Lib\CSScriptLibrary.v3.5.xml"
copy ..\CSScriptLibrary\bin\Release.4.0\CSScriptLibrary.xml "..\..\Lib\Bin\NET 4.0\CSScriptLibrary.xml"
copy ..\CSScriptLibrary\bin\Release.4.0\CSScriptLibrary.dll "..\..\Lib\Bin\NET 4.0\CSScriptLibrary.dll"
copy ..\CSScriptLibrary\bin\Release.4.0\CSScriptLibrary.dll "..\..\Samples\Hosting\CodeDOM\VS2010 project\Lib\CSScriptLibrary.dll"
copy ..\CSScriptLibrary\bin\Release.4.0\CSScriptLibrary.xml "..\..\Samples\Hosting\CodeDOM\VS2010 project\Lib\CSScriptLibrary.xml"

copy cscs.exe "..\..\Lib\Bin\NET 4.5\cscs.exe"
copy csws.exe "..\..\Lib\Bin\NET 4.5\csws.exe"
copy cscs32.exe "..\..\Lib\Bin\NET 4.5\cscs32.exe"
copy csws32.exe "..\..\Lib\Bin\NET 4.5\csws32.exe"
copy CSScriptLibrary.xml "..\..\Lib\Bin\NET 4.5\CSScriptLibrary.xml"
copy CSScriptLibrary.dll "..\..\Lib\Bin\NET 4.5\CSScriptLibrary.dll"
copy CSScriptLibrary.xml "..\..\Samples\Hosting\CodeDOM\VS2012 project\Lib\CSScriptLibrary.xml"
copy CSScriptLibrary.dll "..\..\Samples\Hosting\CodeDOM\VS2012 project\Lib\CSScriptLibrary.dll"
copy CSScriptLibrary.dll.unsigned "..\..\Lib\Bin\NET 4.5\CSScriptLibrary.dll.unsigned"
copy cscs.exe ..\..\cscs.exe
copy csws.exe ..\..\csws.exe
copy css_config.exe ..\..\css_config.exe
copy ..\Mono.CSharp.dll Mono.CSharp.dll
copy ..\Mono.CSharp.dll ..\..\Lib\Mono.CSharp.dll
copy CSScriptLibrary.dll ..\..\Lib\CSScriptLibrary.dll
copy CSScriptLibrary.xml ..\..\Lib\CSScriptLibrary.xml
copy CS-Script.exe ..\..\Lib\ShellExtensions\CS-Script.exe

ECHO Building CSSCodeProvider.v1.1.dll: >> ..\Build\build.log
rem cscs.exe /nl /noconfig /cd ..\CSSCodeProvider\CSSCodeProvider.cs >> ..\Build\build.log
%windir%\Microsoft.NET\Framework\v2.0.50727\csc /nologo /nowarn:618,162 /define:net1 /o /out:..\Build\temp\temp\CSSCodeProvider.v1.1.dll /t:library ..\CSSCodeProvider\CSSCodeProvider.cs ..\CSSCodeProvider\ccscompiler.cs ..\CSSCodeProvider\AssemblyInfo.cs ..\CSSCodeProvider\cppcompiler.cs ..\CSSCodeProvider\xamlcompiler.cs /r:System.dll /r:Microsoft.JScript.dll /r:System.Windows.Forms.dll >> build.log
move ..\Build\temp\temp\CSSCodeProvider.v1.1.dll CSSCodeProvider.v1.1.dll
ECHO ------------ >> ..\Build\build.log


ECHO Building CSSCodeProvider.v3.5.dll: >> ..\Build\build.log
%windir%\Microsoft.NET\Framework\v3.5\csc /nologo /nowarn:618 /o /out:..\Build\temp\temp\CSSCodeProvider.v3.5.dll /t:library ..\CSSCodeProvider.v3.5\CSSCodeProvider.cs ..\CSSCodeProvider.v3.5\ccscompiler.cs ..\CSSCodeProvider.v3.5\AssemblyInfo.cs ..\CSSCodeProvider.v3.5\cppcompiler.cs ..\CSSCodeProvider.v3.5\xamlcompiler.cs /r:System.dll /r:Microsoft.JScript.dll /r:System.Windows.Forms.dll >> build.log
move ..\Build\temp\temp\CSSCodeProvider.v3.5.dll CSSCodeProvider.v3.5.dll
ECHO ------------ >> ..\Build\build.log

ECHO Building CSScript.Tasks.dll: >> ..\Build\build.log
"%net4_tools%\csc.exe" /nologo /nowarn:618,162 /debug+ /debug:full /o /out:..\Build\temp\temp\CSScript.Tasks.dll /t:library ..\NAnt.CSScript\CSScript.Tasks.cs ..\NAnt.CSScript\AssemblyInfo.cs /r:System.dll /r:CSScriptLibrary.dll /r:System.Core.dll /r:System.Xml.dll /r:E:\Galos\BuildTools\nant\bin\NAnt.Core.dll >> build.log
move ..\Build\temp\temp\CSScript.Tasks.dll CSScript.Tasks.dll
copy CSScript.Tasks.dll ..\..\Lib\CSScript.Tasks.dll
copy CSScript.Tasks.dll ..\..\Samples\NAnt\CSScript.Tasks.dll
copy CSScriptLibrary.dll ..\..\Samples\NAnt\CSScriptLibrary.dll
ECHO ------------ >> ..\Build\build.log

ECHO Building CSSCodeProvider.dll: >> ..\Build\build.log
"%net4_tools%\csc.exe" /nologo /nowarn:618 /o  /out:..\Build\temp\temp\CSSCodeProvider.dll /t:library ..\CSSCodeProvider.v4.0\CSSCodeProvider.cs ..\CSSCodeProvider.v4.0\ccscompiler.cs ..\CSSCodeProvider.v4.0\AssemblyInfo.cs ..\CSSCodeProvider.v4.0\cppcompiler.cs ..\CSSCodeProvider.v4.0\xamlcompiler.cs /r:System.dll /r:Microsoft.JScript.dll /r:System.Windows.Forms.dll >> build.log
copy ..\Build\temp\temp\CSSCodeProvider.dll ..\..\Lib\CSSCodeProvider.dll
move ..\Build\temp\temp\CSSCodeProvider.dll CSSCodeProvider.dll
ECHO ------------ >> ..\Build\build.log

ECHO Building css.exe: >> ..\Build\build.log
%windir%\Microsoft.NET\Framework\v2.0.50727\csc /nologo /define:net1 /o /out:css.exe /win32icon:..\Logo\css_logo.ico /t:exe ..\css.cs ..\CS-S.AsmInfo.cs /r:System.dll >> build.log
ECHO ------------ >> ..\Build\build.log

ECHO Building runasm32.exe: >> ..\Build\build.log
"%net4_tools%\csc.exe" /nologo  /o /platform:x86 /out:runasm32.exe /t:exe ..\runasm32.cs /r:System.dll >> build.log
ECHO ------------ >> ..\Build\build.log

ECHO Building CSSPostSharp.dll: >> ..\Build\build.log
%windir%\Microsoft.NET\Framework\v3.5\csc /nologo /o /out:CSSPostSharp.dll /t:library ..\CSSPostSharp.cs /r:System.dll /r:System.Core.dll  >> build.log
ECHO ------------ >> ..\Build\build.log

copy CSSPostSharp.dll ..\..\Lib\CSSPostSharp.dll
copy css.exe ..\..\css.exe
copy runasm32.exe ..\..\Lib\runasm32.exe

rem pause to allow all apps (ant-viruses, compilers) exit and release the temp files
ping 1.1.1.1 -n 1 -w 2000 > nul

ECHO Cleaning up...
del  sgKey.snk
del /S /Q temp\*
RD temp\temp
RD temp

notepad build.log
del build.log



