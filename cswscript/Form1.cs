#region Licence...
//-----------------------------------------------------------------------------
// Date:	10/11/04    Time: 3:00p
// Module:	Form1.cs
// Classes:	CSExecutionClient
//			AppInfo
//
// This module contains the definition of the CSExecutionClient class. Which implements 
// compiling C# code and executing 'Main' method of compiled assembly
//
// Written by Oleg Shilo (oshilo@gmail.com)
// Copyright (c) 2004. All rights reserved.
//
// Redistribution and use of this code in source and binary forms,  without 
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright notice, 
//    this list of conditions and the following disclaimer. 
// 2. Neither the name of an author nor the names of the contributors may be used 
//    to endorse or promote products derived from this software without specific 
//    prior written permission.
// 3. This code may be used in compiled form in any way you desire. This
//	  file may be redistributed unmodified by any means PROVIDING it is 
//    not sold for profit without the authors written consent, and 
//    providing that this notice and the authors name is included. 
//
// Redistribution and use of this code in source and binary forms, with modification, 
// are permitted provided that all above conditions are met and software is not used 
// or sold for profit.
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
#endregion

using System;
using System.Drawing;
using System.Collections;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;

namespace csscript
{
	delegate void PrintDelegate(string msg);

	/// <summary>
	/// Wrapper class that runs CSExecutor witin console application context.
	/// </summary>
	class CSExecutorClient
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args) 
		{
			CSExecutor.Execute(args, new PrintDelegate(Print));
		}
		
		static void Print(string msg)
		{
			MessageBox.Show(msg, "cs-script");
		}
	}
	/// <summary>
	/// Repository for application specific data
	/// </summary>
	class AppInfo
	{
		public static string appName = "cswscript";
		public static string appLogo = "C# Script execution engine;\nCopyright (C) 2004 Oleg Shilo.\n";
		public static string appParamsHelp = "";
	}
}
