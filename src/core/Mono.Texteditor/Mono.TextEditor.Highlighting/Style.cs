// Style.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;
using System.Xml;
using Gdk;

namespace Mono.TextEditor.Highlighting
{
	public class Style
	{
		Dictionary<string, ChunkStyle> styleLookupTable = new Dictionary<string, ChunkStyle> (); 
		Dictionary<string, string> customPalette = new Dictionary<string, string> (); 
		
		public Color GetColorFromDefinition (string colorName)
		{
			return this.GetChunkStyle (colorName).Color;
		}
		
		#region Named colors
		
		public const string DefaultString = "text";
		public virtual ChunkStyle Default {
			get {
				return GetChunkStyle (DefaultString);
			}
		}
		
		public const string CaretString = "caret";
		public virtual ChunkStyle Caret {
			get {
				return GetChunkStyle (CaretString);
			}
		}

		public const string LineNumberString = "linenumber";
		public virtual ChunkStyle LineNumber {
			get {
				return GetChunkStyle (LineNumberString);
			}
		}

		public const string LineNumberFgHighlightedString = "linenumber.highlight";
		public virtual Color LineNumberFgHighlighted {
			get {
				return GetColorFromDefinition (LineNumberFgHighlightedString);
			}
		}

		public const string IconBarBgString = "iconbar";
		public virtual Color IconBarBg {
			get {
				return GetColorFromDefinition (IconBarBgString);
			}
		}

		public const string IconBarSeperatorString = "iconbar.separator";
		public virtual Color IconBarSeperator {
			get {
				return GetColorFromDefinition (IconBarSeperatorString);
			}
		}

		public const string FoldLineString = "fold";
		public virtual ChunkStyle FoldLine {
			get {
				return GetChunkStyle (FoldLineString);
			}
		}

		public const string FoldLineHighlightedString = "fold.highlight";
		public virtual Color FoldLineHighlighted {
			get {
				return GetColorFromDefinition (FoldLineHighlightedString);
			}
		}
		
		public const string LineChangedBgString = "marker.line.changed";
		public virtual Color LineChangedBg {
			get {
				return GetColorFromDefinition (LineChangedBgString);
			}
		}
		
		public const string LineDirtyBgString = "marker.line.dirty";
		public virtual Color LineDirtyBg {
			get {
				return GetColorFromDefinition (LineDirtyBgString);
			}
		}

		public const string SelectionString = "text.selection";
		public virtual ChunkStyle Selection {
			get {
				return GetChunkStyle (SelectionString);
			}
		}

		public const string LineMarkerString = "marker.line";
		public virtual Color LineMarker {
			get {
				return GetColorFromDefinition (LineMarkerString);
			}
		}

		public const string RulerString = "marker.ruler";
		public virtual Color Ruler {
			get {
				return GetColorFromDefinition (RulerString);
			}
		}

		public const string WhitespaceMarkerString = "marker.whitespace";
		public virtual Color WhitespaceMarker {
			get {
				return GetColorFromDefinition (WhitespaceMarkerString);
			}
		}

		public const string InvalidLineMarkerString = "marker.invalidline";
		public virtual Color InvalidLineMarker {
			get {
				return GetColorFromDefinition (InvalidLineMarkerString);
			}
		}
		
		public const string FoldToggleMarkerString = "fold.togglemarker";
		public virtual Color FoldToggleMarker {
			get {
				return GetColorFromDefinition (FoldToggleMarkerString);
			}
		}
		
		public const string BracketHighlightRectangleString = "marker.bracket";
		public virtual ChunkStyle BracketHighlightRectangle {
			get {
				return GetChunkStyle (BracketHighlightRectangleString);
			}
		}
		
		public const string BookmarkColor2String = "marker.bookmark.color2";
		public virtual Color BookmarkColor2 {
			get {
				return GetColorFromDefinition (BookmarkColor2String);
			}
		}
		
		public const string BookmarkColor1String = "marker.bookmark.color1";
		public virtual Color BookmarkColor1 {
			get {
				return GetColorFromDefinition (BookmarkColor1String);
			}
		}

		
		public const string SearchTextBgString = "text.background.searchresult";
		public Color SearchTextBg {
			get {
				return GetColorFromDefinition (SearchTextBgString);
			}
		}

		public const string BreakpointString = "marker.breakpoint";
		public Color BreakpointFg {
			get {
				return GetChunkStyle (BreakpointString).Color;
			}
		}
		public Color BreakpointBg {
			get {
				return GetChunkStyle (BreakpointString).BackgroundColor;
			}
		}

		public const string BreakpointMarkerColor2String = "marker.breakpoint.color2";
		public Color BreakpointMarkerColor2 {
			get {
				return GetColorFromDefinition (BreakpointMarkerColor2String);
			}
		}

		public const string BreakpointMarkerColor1String = "marker.breakpoint.color1";
		public Color BreakpointMarkerColor1 {
			get {
				return GetColorFromDefinition (BreakpointMarkerColor1String);
			}
		}

		public const string CurrentDebugLineString = "marker.debug.currentline";
		public Color CurrentDebugLineFg {
			get {
				return GetChunkStyle (CurrentDebugLineString).Color;
			}
		}
		
		public Color CurrentDebugLineBg {
			get {
				return GetChunkStyle (CurrentDebugLineString).BackgroundColor;
			}
		}

		public const string CurrentDebugLineMarkerColor2String = "marker.debug.currentline.color2";
		public Color CurrentDebugLineMarkerColor2 {
			get {
				return GetColorFromDefinition (CurrentDebugLineMarkerColor2String);
			}
		}

		public const string CurrentDebugLineMarkerColor1String = "marker.debug.currentline.color1";
		public Color CurrentDebugLineMarkerColor1 {
			get {
				return GetColorFromDefinition (CurrentDebugLineMarkerColor1String);
			}
		}

		public const string CurrentDebugLineMarkerBorderString = "marker.debug.currentline.border";
		public Color CurrentDebugLineMarkerBorder {
			get {
				return GetColorFromDefinition (CurrentDebugLineMarkerBorderString);
			}
		}
		
		public const string DebugStackLineString = "marker.debug.stackline";
		public Color DebugStackLineFg {
			get {
				return GetChunkStyle (DebugStackLineString).Color;
			}
		}
		
		public Color DebugStackLineBg {
			get {
				return GetChunkStyle (DebugStackLineString).BackgroundColor;
			}
		}

		public const string DebugStackLineMarkerColor2String = "marker.debug.stackline.color2";
		public Color DebugStackLineMarkerColor2 {
			get {
				return GetColorFromDefinition (DebugStackLineMarkerColor2String);
			}
		}

		public const string DebugStackLineMarkerColor1String = "marker.debug.stackline.color1";
		public Color DebugStackLineMarkerColor1 {
			get {
				return GetColorFromDefinition (DebugStackLineMarkerColor1String);
			}
		}

		public const string DebugStackLineMarkerBorderString = "marker.debug.stackline.border";
		public Color DebugStackLineMarkerBorder {
			get {
				return GetColorFromDefinition (DebugStackLineMarkerBorderString);
			}
		}

		public const string InvalidBreakpointBgString = "marker.breakpoint.invalid.background";
		public Color InvalidBreakpointBg {
			get {
				return GetColorFromDefinition (InvalidBreakpointBgString);
			}
		}

		public const string InvalidBreakpointMarkerColor1String = "marker.breakpoint.invalid.color1";
		public Color InvalidBreakpointMarkerColor1 {
			get {
				return GetColorFromDefinition (InvalidBreakpointMarkerColor1String);
			}
		}

		public const string DisabledBreakpointBgString = "marker.breakpoint.disabled.background";
		public Color DisabledBreakpointBg {
			get {
				return GetColorFromDefinition (DisabledBreakpointBgString);
			}
		}

		public const string InvalidBreakpointMarkerBorderString = "marker.breakpoint.invalid.border";
		public Color InvalidBreakpointMarkerBorder {
			get {
				return GetColorFromDefinition (InvalidBreakpointMarkerBorderString);
			}
		}
		
		public const string ErrorUnderlineString = "marker.underline.error";
		public Color ErrorUnderline {
			get {
				return GetColorFromDefinition (ErrorUnderlineString);
			}
		}
		
		public const string WarningUnderlineString = "marker.underline.warning";
		public Color WarningUnderline {
			get {
				return GetColorFromDefinition (WarningUnderlineString);
			}
		}
		
		public const string PrimaryTemplateColorString = "marker.template.primary_template";
		public virtual ChunkStyle PrimaryTemplate {
			get {
				return GetChunkStyle (PrimaryTemplateColorString);
			}
		}
		
		public const string PrimaryTemplateHighlightedColorString = "marker.template.primary_highlighted_template";
		public virtual ChunkStyle PrimaryTemplateHighlighted {
			get {
				return GetChunkStyle (PrimaryTemplateHighlightedColorString);
			}
		}
		
		public const string SecondaryTemplateColorString = "marker.template.secondary_template";
		public virtual ChunkStyle SecondaryTemplate {
			get {
				return GetChunkStyle (SecondaryTemplateColorString);
			}
		}
		
		public const string SecondaryTemplateHighlightedColorString = "marker.template.secondary_highlighted_template";
		public virtual ChunkStyle SecondaryTemplateHighlighted {
			get {
				return GetChunkStyle (SecondaryTemplateHighlightedColorString);
			}
		}
		#endregion
		
		public string Name {
			get;
			private set;
		}
		
		public string Description {
			get;
			private set;
		}
		
		public static Cairo.Color ToCairoColor (Gdk.Color color)
		{
			return new Cairo.Color ((double)color.Red / ushort.MaxValue,
			                        (double)color.Green / ushort.MaxValue,
			                        (double)color.Blue / ushort.MaxValue);
		}
		
		protected Style ()
		{
			SetStyle (DefaultString, new ChunkStyle (new Gdk.Color (0, 0, 0), new Gdk.Color (255, 255, 255)));
			GetChunkStyle (DefaultString).ChunkProperties |= ChunkProperties.TransparentBackground;
			
			SetStyle (CaretString, new ReferencedChunkStyle (this, DefaultString));

			SetStyle (LineNumberString, new ChunkStyle (new Gdk.Color (172, 168, 153), new Gdk.Color (255, 255, 255)));
			SetStyle (LineNumberFgHighlightedString, new ChunkStyle (new Gdk.Color (122, 118, 103)));
			
			SetStyle (IconBarBgString, new ChunkStyle (new Gdk.Color (255, 255, 255)));
			SetStyle (IconBarSeperatorString, new ChunkStyle (new Gdk.Color (172, 168, 153)));
			
			SetStyle (FoldLineString, new ReferencedChunkStyle (this, LineNumberString));
			SetStyle (FoldLineHighlightedString, new ReferencedChunkStyle (this, IconBarSeperatorString));
			SetStyle (FoldToggleMarkerString, new ReferencedChunkStyle (this, DefaultString));
			
			SetStyle (LineDirtyBgString, new ChunkStyle (new Gdk.Color (255, 238, 98)));
			SetStyle (LineChangedBgString, new ChunkStyle (new Gdk.Color (108, 226, 108)));
			
			SetStyle (SelectionString, new ChunkStyle (new Gdk.Color (255, 255, 255), new Gdk.Color (96, 87, 210)));
			
			SetStyle (LineMarkerString, new ChunkStyle (new Gdk.Color (200, 255, 255)));
			SetStyle (RulerString, new ChunkStyle (new Gdk.Color (187, 187, 187)));
			SetStyle (WhitespaceMarkerString, new ReferencedChunkStyle (this, RulerString));
			
			SetStyle (InvalidLineMarkerString, new ChunkStyle (new Gdk.Color (210, 0, 0)));
			
			SetStyle (BreakpointString, new ChunkStyle (new Gdk.Color (255, 255, 255), new Gdk.Color (125, 0, 0)));
			
			SetStyle (BreakpointMarkerColor1String, new ChunkStyle (new Gdk.Color (255, 255, 255)));
			SetStyle (BreakpointMarkerColor2String, new ChunkStyle (new Gdk.Color (125, 0, 0)));

			SetStyle (DisabledBreakpointBgString, new ChunkStyle (new Gdk.Color (237, 220, 220)));
			
			SetStyle (CurrentDebugLineString, new ChunkStyle (new Gdk.Color (0, 0, 0), new Gdk.Color (255, 255, 0)));
			SetStyle (CurrentDebugLineMarkerColor1String, new ChunkStyle (new Gdk.Color (255, 255, 0)));
			SetStyle (CurrentDebugLineMarkerColor2String, new ChunkStyle (new Gdk.Color (255, 255, 204)));
			SetStyle (CurrentDebugLineMarkerBorderString, new ChunkStyle (new Gdk.Color (102, 102, 0)));
			
			SetStyle (DebugStackLineString, new ChunkStyle (new Gdk.Color (0, 0, 0), new Gdk.Color (128, 255, 128)));
			SetStyle (DebugStackLineMarkerColor1String, new ChunkStyle (new Gdk.Color (128, 255, 128)));
			SetStyle (DebugStackLineMarkerColor2String, new ChunkStyle (new Gdk.Color (204, 255, 204)));
			SetStyle (DebugStackLineMarkerBorderString, new ChunkStyle (new Gdk.Color (51, 102, 51))); 
			
			SetStyle (InvalidBreakpointBgString, new ChunkStyle (new Gdk.Color (237, 220, 220)));
			SetStyle (InvalidBreakpointMarkerColor1String, new ChunkStyle (new Gdk.Color (237, 220, 220)));
			SetStyle (InvalidBreakpointMarkerBorderString, new ChunkStyle (new Gdk.Color (125, 0, 0)));
			SetStyle (SearchTextBgString, new ChunkStyle (new Gdk.Color (250, 250, 0)));
			
			SetStyle (BracketHighlightRectangleString, new ChunkStyle (new Gdk.Color (128, 128, 128), new Gdk.Color (196, 196, 196)));
			
			SetStyle (BookmarkColor1String, new ChunkStyle (new Gdk.Color (255, 255, 255)));
			SetStyle (BookmarkColor2String, new ChunkStyle (new Gdk.Color (105, 156, 235)));
			
			SetStyle (ErrorUnderlineString, new ChunkStyle (new Gdk.Color (255, 0, 0)));
			SetStyle (WarningUnderlineString, new ChunkStyle (new Gdk.Color (30, 30, 255)));
			
			SetStyle ("diff.line-added", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0,  0x8B,  0x8B), ChunkProperties.None));
			SetStyle ("diff.line-removed", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0x6A, 0x5A, 0xCD), ChunkProperties.None));
			SetStyle ("diff.line-changed", new ReferencedChunkStyle (this, "text.preprocessor"));
			SetStyle ("diff.header", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 128,  0), ChunkProperties.Bold));
			SetStyle ("diff.header-seperator", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0,  0,  255)));
			SetStyle ("diff.header-oldfile", new ReferencedChunkStyle (this, "diff.header"));
			SetStyle ("diff.header-newfile", new ReferencedChunkStyle (this, "diff.header"));
			SetStyle ("diff.location", new ReferencedChunkStyle (this, "keyword.misc"));
			
			SetStyle (PrimaryTemplateColorString, new ChunkStyle (new Gdk.Color (0xB4, 0xE4, 0xB4), new Gdk.Color (0xB4, 0xE4, 0xB4)));
			SetStyle (PrimaryTemplateHighlightedColorString, new ChunkStyle (new Gdk.Color (0, 0, 0), new Gdk.Color (0xB4, 0xE4, 0xB4)));
			
			SetStyle (SecondaryTemplateColorString, new ChunkStyle (new Gdk.Color (0xFF, 0xFF, 0xFF), new Gdk.Color (0xFF, 0xFF, 0xFF)));
			SetStyle (SecondaryTemplateHighlightedColorString, new ChunkStyle (new Gdk.Color (0x7F, 0x7F, 0x7F), new Gdk.Color (0xFF, 0xFF, 0xFF)));
		}
		
		protected void PopulateDefaults ()
		{
			SetStyle ("text", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 0)));
			SetStyle ("text.punctuation", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 0)));
			SetStyle ("text.link", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 255)));
			SetStyle ("text.preprocessor", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 128 , 0)));
			SetStyle ("text.preprocessor.keyword", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 128, 0), ChunkProperties.Bold));
			SetStyle ("text.markup", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0x8A , 0x8C)));
			SetStyle ("text.markup.tag", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0x6A, 0x5A, 0xCD)));
			
			SetStyle ("comment", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 255)));
			SetStyle ("comment.line", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 255)));
			SetStyle ("comment.block", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 255)));
			SetStyle ("comment.doc", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 255)));
			SetStyle ("comment.tag", new Mono.TextEditor.ChunkStyle (new Gdk.Color (128, 128, 128), ChunkProperties.Italic));
			SetStyle ("comment.tag.line", new Mono.TextEditor.ChunkStyle (new Gdk.Color (128, 128, 128), ChunkProperties.Italic));
			SetStyle ("comment.tag.block", new Mono.TextEditor.ChunkStyle (new Gdk.Color (128, 128, 128), ChunkProperties.Italic));
			SetStyle ("comment.tag.doc", new Mono.TextEditor.ChunkStyle (new Gdk.Color (128, 128, 128), ChunkProperties.Italic));
			SetStyle ("comment.keyword", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0,255), ChunkProperties.Italic));
			SetStyle ("comment.keyword.todo", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 255), ChunkProperties.Bold));
			
			SetStyle ("constant", new Mono.TextEditor.ChunkStyle (new Gdk.Color (255, 0, 255)));
			SetStyle ("constant.digit", new Mono.TextEditor.ChunkStyle (new Gdk.Color (255, 0, 255)));
			SetStyle ("constant.language", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), ChunkProperties.Bold));
			SetStyle ("constant.language.void", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), ChunkProperties.Bold));
			
			SetStyle ("string", new Mono.TextEditor.ChunkStyle (new Gdk.Color (255, 0, 255)));
			SetStyle ("string.single", new Mono.TextEditor.ChunkStyle (new Gdk.Color (255, 0, 255)));
			SetStyle ("string.double", new Mono.TextEditor.ChunkStyle (new Gdk.Color (255, 0, 255)));
			SetStyle ("string.other", new Mono.TextEditor.ChunkStyle (new Gdk.Color (255, 0, 255)));
			
			SetStyle ("keyword.semantic.type", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0x8A , 0x8C)));
			
			SetStyle ("keyword", new Mono.TextEditor.ChunkStyle (new Gdk.Color (0, 0, 0), ChunkProperties.Bold));
			
			SetStyle ("keyword.access", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), ChunkProperties.Bold));
			SetStyle ("keyword.operator", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), ChunkProperties.Bold));
			SetStyle ("keyword.operator.declaration", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), ChunkProperties.Bold));
			SetStyle ("keyword.selection", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), ChunkProperties.Bold));
			SetStyle ("keyword.iteration", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), ChunkProperties.Bold));
			SetStyle ("keyword.jump", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), ChunkProperties.Bold));
			SetStyle ("keyword.context", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), ChunkProperties.Bold));
			SetStyle ("keyword.exceptions", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), ChunkProperties.Bold));
			SetStyle ("keyword.modifier", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), ChunkProperties.Bold));
			SetStyle ("keyword.type", new Mono.TextEditor.ChunkStyle (new Gdk.Color ( 46, 139,  87), ChunkProperties.Bold));
			SetStyle ("keyword.namespace", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), ChunkProperties.Bold));
			SetStyle ("keyword.property", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), ChunkProperties.Bold));
			SetStyle ("keyword.declaration", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), ChunkProperties.Bold));
			SetStyle ("keyword.parameter", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), ChunkProperties.Bold));
			SetStyle ("keyword.misc", new Mono.TextEditor.ChunkStyle (new Gdk.Color (165,  42,  42), ChunkProperties.Bold));
			
		}
		
		void SetStyle (string name, ChunkStyle style)
		{
			styleLookupTable[name] = style;
		}
		
		public ChunkStyle GetDefaultChunkStyle ()
		{
			ChunkStyle style;
			if (!styleLookupTable.TryGetValue (DefaultString, out style)) {
				style = new ChunkStyle (GetColorFromDefinition (DefaultString));
				styleLookupTable[DefaultString] = style;
			}
			return style;
		}
		
		public ChunkStyle GetChunkStyle (string name)
		{
			ChunkStyle style;
			if (styleLookupTable.TryGetValue (name, out style))
				return style;
			
			int dotIndex = name.LastIndexOf ('.');
			string fallbackName = name;
			while (dotIndex > 1) {
				fallbackName = fallbackName.Substring (0, dotIndex);
				if (styleLookupTable.TryGetValue (fallbackName, out style)) {
					styleLookupTable[name] = style;
				//	Console.WriteLine ("Chunk style {0} fell back to {1}", name, fallbackName);
					return style;
				}
				dotIndex = fallbackName.LastIndexOf ('.');
			}
			
		//	Console.WriteLine ("Chunk style {0} fell back to default", name);
			return GetDefaultChunkStyle ();
		}
		
		public void SetChunkStyle (string name, string weight, string foreColor, string backColor)
		{
			Gdk.Color color   = !String.IsNullOrEmpty (foreColor) ? this.GetColorFromString (foreColor) : Gdk.Color.Zero;
			Gdk.Color bgColor = !String.IsNullOrEmpty (backColor) ? this.GetColorFromString (backColor) : Gdk.Color.Zero;
			ChunkProperties properties = ChunkProperties.None;
			if (weight != null) {
				if (weight.ToUpper ().IndexOf ("BOLD") >= 0)
					properties |= ChunkProperties.Bold;
				if (weight.ToUpper ().IndexOf ("ITALIC") >= 0)
					properties |= ChunkProperties.Italic;
				if (weight.ToUpper ().IndexOf ("UNDERLINE") >= 0)
					properties |= ChunkProperties.Underline;
			}
			SetStyle (name, new Mono.TextEditor.ChunkStyle (color, bgColor, properties));
		}
		
		public Gdk.Color GetColorFromString (string colorString)
		{
			if (customPalette.ContainsKey (colorString))
				return this.GetColorFromString (customPalette[colorString]);
			if (styleLookupTable.ContainsKey (colorString)) 
				return styleLookupTable[colorString].Color;
			Gdk.Color result = new Color ();
			if (!Gdk.Color.Parse (colorString, ref result)) 
				throw new Exception ("Can't parse color: " + colorString);
			return result;
		}
		
		public const string NameAttribute = "name";
		
		static void ReadStyleTree (XmlReader reader, Style result, string curName, string curWeight, string curColor, string curBgColor)
		{
			string name    = reader.GetAttribute ("name"); 
			string weight  = reader.GetAttribute ("weight") ?? curWeight;
			string color   = reader.GetAttribute ("color") ?? curColor;
			string bgColor = reader.GetAttribute ("bgColor") ?? curBgColor;
			string fullName;
			if (String.IsNullOrEmpty (curName)) {
				fullName = name;
			} else {
				fullName = curName + "." + name;
			}
			if (!String.IsNullOrEmpty (color)) {
				result.SetChunkStyle (fullName, weight, color, bgColor);
			}
			XmlReadHelper.ReadList (reader, "Style", delegate () {
				switch (reader.LocalName) {
				case "Style":
					ReadStyleTree (reader, result, fullName, weight, color, bgColor);
					return true;
				}
				return false;
			});
		}
		
		public static Style LoadFrom (XmlReader reader)
		{
			Style result = new Style ();
			XmlReadHelper.ReadList (reader, "EditorStyle", delegate () {
				switch (reader.LocalName) {
				case "EditorStyle":
					result.Name        = reader.GetAttribute (NameAttribute);
					result.Description = reader.GetAttribute ("_description");
					return true;
				case "Color":
					result.customPalette[reader.GetAttribute ("name")] = reader.GetAttribute ("value");
					return true;
				case "Style":
					ReadStyleTree (reader, result, null, null, null, null);
					return true;
				}
				return false;
			});
			result.GetChunkStyle (DefaultString).ChunkProperties |= ChunkProperties.TransparentBackground;
			return result;
		}
		
		public virtual void UpdateFromGtkStyle (Gtk.Style style)
		{
		}
	}
}
