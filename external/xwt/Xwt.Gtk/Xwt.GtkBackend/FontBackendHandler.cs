// 
// FontBackendHandler.cs
//  
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Xwt.Backends;
using Pango;
using Xwt.Drawing;
using System.Globalization;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace Xwt.GtkBackend
{
	public class GtkFontBackendHandler: FontBackendHandler
	{
		public override object GetSystemDefaultFont ()
		{
			var la = new Gtk.Label ("");
			return la.Style.FontDescription;
		}

		public override IEnumerable<string> GetInstalledFonts ()
		{
			return Gdk.PangoHelper.ContextGet ().FontMap.Families.Select (f => f.Name);
		}

		public override object Create (string fontName, double size, FontStyle style, FontWeight weight, FontStretch stretch)
		{
			return FontDescription.FromString (fontName + ", " + style + " " + weight + " " + stretch + " " + size.ToString (CultureInfo.InvariantCulture));
		}

		#region IFontBackendHandler implementation
		
		public override object Copy (object handle)
		{
			FontDescription d = (FontDescription) handle;
			return d.Copy ();
		}
		
		public override object SetSize (object handle, double size)
		{
			FontDescription d = (FontDescription) handle;
			d = d.Copy ();
			d.Size = (int) (size * Pango.Scale.PangoScale);
			return d;
		}

		public override object SetFamily (object handle, string family)
		{
			FontDescription fd = (FontDescription) handle;
			fd = fd.Copy ();
			fd.Family = family;
			return fd;
		}

		public override object SetStyle (object handle, FontStyle style)
		{
			FontDescription fd = (FontDescription) handle;
			fd = fd.Copy ();
			fd.Style = (Pango.Style)(int)style;
			return fd;
		}

		public override object SetWeight (object handle, FontWeight weight)
		{
			FontDescription fd = (FontDescription) handle;
			fd = fd.Copy ();
			fd.Weight = (Pango.Weight)(int)weight;
			return fd;
		}

		public override object SetStretch (object handle, FontStretch stretch)
		{
			FontDescription fd = (FontDescription) handle;
			fd = fd.Copy ();
			fd.Stretch = (Pango.Stretch)(int)stretch;
			return fd;
		}
		
		public override double GetSize (object handle)
		{
			FontDescription fd = (FontDescription) handle;
			return (double)fd.Size / (double) Pango.Scale.PangoScale;
		}

		public override string GetFamily (object handle)
		{
			FontDescription fd = (FontDescription) handle;
			return fd.Family;
		}

		public override FontStyle GetStyle (object handle)
		{
			FontDescription fd = (FontDescription) handle;
			return (FontStyle)(int)fd.Style;
		}

		public override FontWeight GetWeight (object handle)
		{
			FontDescription fd = (FontDescription) handle;
			return (FontWeight)(int)fd.Weight;
		}

		public override FontStretch GetStretch (object handle)
		{
			FontDescription fd = (FontDescription) handle;
			return (FontStretch)(int)fd.Stretch;
		}
		#endregion


	}
}

