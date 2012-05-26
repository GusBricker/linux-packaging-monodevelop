//
// AssemblyInfo.cs: Assembly Informations
//
// Author:
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2004-2005 Novell Inc. (http://www.novell.com)
//

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

// AssemblyTitle and AssemblyDescription are in /<ui>/Main.cs
[assembly: AssemblyCompany ("Novell Inc.")]
[assembly: AssemblyCopyright ("Copyright © 2004-2005 Novell Inc.")]
[assembly: AssemblyVersion ("0.1.1.0")]

namespace Mono.Tools {

	public class AssemblyInfo {
	
		private string _name;
		private string _title;
		private string _copyright;
		private string _description;
		private string _version;
		
		public AssemblyInfo ()
			: this (Assembly.GetExecutingAssembly ())
		{
		} 
	
		public AssemblyInfo (Assembly a)
		{
			if (a == null)
				throw new ArgumentNullException ("a");

			AssemblyName an = a.GetName ();
			_name = an.ToString ();

			object [] att = a.GetCustomAttributes (typeof (AssemblyTitleAttribute), false);
			_title = ((att.Length > 0) ? ((AssemblyTitleAttribute) att [0]).Title : String.Empty);

			att = a.GetCustomAttributes (typeof (AssemblyCopyrightAttribute), false);
			_copyright = ((att.Length > 0) ? ((AssemblyCopyrightAttribute) att [0]).Copyright : String.Empty);

			att = a.GetCustomAttributes (typeof (AssemblyDescriptionAttribute), false);
			_description = ((att.Length > 0) ? ((AssemblyDescriptionAttribute) att [0]).Description : String.Empty);
			
			_version = an.Version.ToString ();
		}
	
		public string Copyright {
			get { return _copyright; }
		}

		public string Description {
			get { return _description; }
		}

		public string Name {
			get { return _name; }
		}

		public string Title {
			get { return _title; }
		}

		public string Version {
			get { return _version; }
		}
		
		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder ();
			sb.AppendFormat ("{1} - version {2}{0}{3}{0}{4}{0}",
				Environment.NewLine,
				_title, _version,
				_description,
				_copyright);
			return sb.ToString ();
		}
	}
}
