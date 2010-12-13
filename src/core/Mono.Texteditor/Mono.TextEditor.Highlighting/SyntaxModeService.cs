// SyntaxModeService.cs
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
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml;
using System.Xml.Schema;

namespace Mono.TextEditor.Highlighting
{
	public static class SyntaxModeService
	{
		static Dictionary<string, SyntaxMode> syntaxModes = new Dictionary<string, SyntaxMode> ();
		static Dictionary<string, Style>      styles      = new Dictionary<string, Style> ();
		static Dictionary<string, IXmlProvider> syntaxModeLookup = new Dictionary<string, IXmlProvider> ();
		static Dictionary<string, IXmlProvider> styleLookup      = new Dictionary<string, IXmlProvider> ();
		
		public static string[] Styles {
			get {
				List<string> result = new List<string> ();
				foreach (string style in styles.Keys) {
					if (!result.Contains (style))
						result.Add (style);
				}
				foreach (string style in styleLookup.Keys) {
					if (!result.Contains (style))
						result.Add (style);
				}
				return result.ToArray ();
			}
		}
		
		public static void InstallSyntaxMode (string mimeType, SyntaxMode mode)
		{
			if (syntaxModeLookup.ContainsKey (mimeType))
				syntaxModeLookup.Remove (mimeType);
			syntaxModes[mimeType] = mode;
		}
		
		public static Style GetColorStyle (Gtk.Style widgetStyle, string name) 
		{
			if (styles.ContainsKey (name))
				return styles [name];
			if (styleLookup.ContainsKey (name)) {
				LoadStyle (name);
				return GetColorStyle (widgetStyle, name);
			}
			return new DefaultStyle (widgetStyle);
		}
		
		public static IXmlProvider GetProvider (SyntaxMode mode)
		{
			foreach (string mimeType in mode.MimeType.Split (';')) {
				if (syntaxModeLookup.ContainsKey (mimeType)) 
					return syntaxModeLookup[mimeType];
			}
			return null;
		}
		
		public static IXmlProvider GetProvider (Style style)
		{
			if (styleLookup.ContainsKey (style.Name)) 
				return styleLookup[style.Name];
			return null;
		}
		
		static void LoadStyle (string name)
		{
			if (!styleLookup.ContainsKey (name))
				throw new System.ArgumentException ("Style " + name + " not found", "name");
			XmlReader reader = styleLookup [name].Open ();
			try {
				styles [name] = Style.LoadFrom (reader);
			} catch (Exception e) {
				throw new IOException ("Error while loading style :" + name, e);
			} finally {
				reader.Close ();
			}
		}
		
		static void LoadSyntaxMode (string mimeType)
		{
			if (!syntaxModeLookup.ContainsKey (mimeType))
				throw new System.ArgumentException ("Syntax mode for mime:" + mimeType + " not found", "mimeType");
			XmlReader reader = syntaxModeLookup [mimeType].Open ();
			try {
				SyntaxMode mode = SyntaxMode.Read (reader);
				foreach (string mime in mode.MimeType.Split (';')) {
					syntaxModes [mime] = mode;
				}
			} catch (Exception e) {
				throw new IOException ("Error while syntax mode for mime:" + mimeType, e);
			} finally {
				reader.Close ();
			}
		}
		
		public static SyntaxMode GetSyntaxMode (string mimeType)
		{
			if (syntaxModes.ContainsKey (mimeType))
				return syntaxModes[mimeType];
			if (syntaxModeLookup.ContainsKey (mimeType)) {
				LoadSyntaxMode (mimeType);
				syntaxModeLookup.Remove (mimeType);
				return GetSyntaxMode (mimeType);
			}
			return null;
		}
		
		public static bool ValidateAllSyntaxModes ()
		{
			foreach (string mime in new List<string> (syntaxModeLookup.Keys)) {
				GetSyntaxMode (mime);
			}
			syntaxModeLookup.Clear ();
			foreach (string style in new List<string> (styleLookup.Keys)) {
				GetColorStyle (null, style);
			}
			styleLookup.Clear ();
			bool result = true;
			foreach (KeyValuePair<string, Style> style in styles) {
				HashSet<SyntaxMode> checkedModes = new HashSet<SyntaxMode> ();
				foreach (KeyValuePair<string, SyntaxMode> mode in syntaxModes) {
					if (checkedModes.Contains (mode.Value))
						continue;
					if (!mode.Value.Validate (style.Value)) {
						System.Console.WriteLine(mode.Key + " failed to validate against:" + style.Key);
						result = false;
					}
					checkedModes.Add (mode.Value);
				}
			}
			return result;
		}
		
		public static void Remove (Style style)
		{
			if (styles.ContainsKey (style.Name))
				styles.Remove (style.Name);
			if (styleLookup.ContainsKey (style.Name))
				styleLookup.Remove (style.Name);
		}
		
		public static void Remove (SyntaxMode mode)
		{
			foreach (string mimeType in mode.MimeType.Split (';')) {
				if (syntaxModes.ContainsKey (mimeType)) 
					syntaxModes.Remove (mimeType);
				if (syntaxModeLookup.ContainsKey (mimeType)) 
					syntaxModeLookup.Remove (mimeType);
			}
		}
		
		public static void ScanSpans (Document doc, SyntaxMode mode, Rule rule, Stack<Span> spanStack, int start, int end)
		{
			SyntaxMode.SpanParser parser = mode.CreateSpanParser (doc, mode, null, spanStack);
			parser.ParseSpans (start, end - start);
		}
		
		static bool IsEqual (Span[] spans1, Span[] spans2)
		{
			if (spans1 == null || spans1.Length == 0) 
				return spans2 == null || spans2.Length == 0;
			if (spans2 == null || spans1.Length != spans2.Length)
				return false;
			for (int i = 0; i < spans1.Length; i++) {
				if (spans1[i] != spans2[i]) {
					return false;
				}
			}
			return true;
		}
		
		static Queue<UpdateWorker> updateQueue = new Queue<UpdateWorker> ();
		
		class UpdateWorker
		{
			Document doc;
			SyntaxMode mode;
			int startOffset;
			int endOffset;
			
			public ManualResetEvent ManualResetEvent {
				get;
				private set;
			}
			
			public Document Doc {
				get { return this.doc; }
			}
			
			public bool IsFinished {
				get;
				set;
			}
			public UpdateWorker (Document doc, SyntaxMode mode, int startOffset, int endOffset)
			{
				this.doc = doc;
				this.mode = mode;
				this.startOffset = startOffset;
				this.endOffset = endOffset;
				IsFinished = false;
				ManualResetEvent = new ManualResetEvent (false);
			}
			
			protected void ScanSpansThreaded (Document doc, Rule rule, Stack<Span> spanStack, int start, int end)
			{
				SyntaxMode.SpanParser parser = mode.CreateSpanParser (doc, mode, null, spanStack);
				parser.ParseSpans (start, end - start);
			}
			
			bool EndsWithContinuation (Span span, LineSegment line)
			{
				return !span.StopAtEol || span.StopAtEol && !string.IsNullOrEmpty (span.Continuation) &&
					line != null && doc.GetTextAt (line).Trim ().EndsWith (span.Continuation);
			}
			
			public void InnerRun ()
			{
				bool doUpdate = false;
				LineSegment lineSegment = doc.GetLineByOffset (startOffset);
				if (lineSegment == null)
					return;
				RedBlackTree<LineSegmentTree.TreeNode>.RedBlackTreeIterator iter = lineSegment.Iter;
				if (iter == null || iter.Current == null)
					return;
				Stack<Span> spanStack = iter.Current.StartSpan != null ? new Stack<Span> (iter.Current.StartSpan) : new Stack<Span> ();
				try {
					LineSegment oldLine = iter.Current.Offset > 0 ? doc.GetLineByOffset (iter.Current.Offset - 1) : null;
					do {
						LineSegment line = iter.Current;
						if (line == null || line.Offset < 0)
							break;
						
						List<Span> spanList = new List<Span> (spanStack.ToArray ());
						spanList.Reverse ();
						for (int i = 0; i < spanList.Count; i++) {
							if (!EndsWithContinuation (spanList[i], oldLine)) {
								spanList.RemoveAt (i);
								i--;
							}
						}
						Span[] newSpans = spanList.ToArray ();
						if (line.Offset > endOffset) {
							bool equal = IsEqual (line.StartSpan, newSpans);
							doUpdate |= !equal;
							if (equal)
								break;
						}
						line.StartSpan = newSpans.Length > 0 ? newSpans : null;
						oldLine = line;
						Rule rule = mode;
						if (spanStack.Count > 0 && !String.IsNullOrEmpty (spanStack.Peek ().Rule))
							rule = mode.GetRule (spanStack.Peek ().Rule) ?? mode;
						
						ScanSpansThreaded (doc, rule, spanStack, line.Offset, line.EndOffset);
						while (spanStack.Count > 0 && !EndsWithContinuation (spanStack.Peek (), line))
							spanStack.Pop ();
					
					} while (iter.MoveNext ());
				} catch (Exception e) {
					Console.WriteLine ("Syntax highlighting exception:" + e);
				}
				if (doUpdate) {
					Gtk.Application.Invoke (delegate {
						doc.RequestUpdate (new UpdateAll ());
						doc.CommitDocumentUpdate ();
					});
				}
				IsFinished = true;
				ManualResetEvent.Set ();
			}
		}
		
//		static bool updateIsRunning = false;
//		static void Update (object o)
//		{
//			updateIsRunning = false;
//		}
		
	//	static readonly object syncObject  = new object();
		static Thread         updateThread = null;
		static AutoResetEvent queueSignal  = new AutoResetEvent (false);
		
		static void StartUpdateThread ()
		{
			updateThread = new Thread (ProcessQueue);
			updateThread.Name = "Syntax highlighting";
			updateThread.IsBackground = true;
			updateThread.Start();
		}
		
		static void ProcessQueue ()
		{
			while (true) {
				while (updateQueue.Count > 0) {
					UpdateWorker worker = null;
					lock (updateQueue) {
						worker = updateQueue.Dequeue ();
					}
					worker.InnerRun ();
				}
				queueSignal.WaitOne ();
			}
		}
		
		public static void StartUpdate (Document doc, SyntaxMode mode, int startOffset, int endOffset)
		{
			lock (updateQueue) {
				updateQueue.Enqueue (new UpdateWorker (doc, mode, startOffset, endOffset));
			}
			queueSignal.Set ();
		}
		
		public static void WaitUpdate (Document doc)
		{
			foreach (UpdateWorker worker in updateQueue.ToArray ()) {
				try {
					if (worker != null && worker.Doc == doc)
						worker.ManualResetEvent.WaitOne ();
				} catch (Exception e) {
					Console.WriteLine ("Worker:" + worker);
					Console.WriteLine ("------");
					Console.WriteLine (e);
					Console.WriteLine ("------");
					Console.WriteLine ("worker.Doc:" + worker.Doc);
					Console.WriteLine ("worker.IsFinished:" + worker.IsFinished);
					Console.WriteLine ("worker.ManualResetEvent:" + worker.ManualResetEvent);
				}
			}
		}
		
		static string Scan (XmlReader reader, string attribute)
		{
			while (reader.Read () && !reader.IsStartElement ()) 
				;
			return reader.GetAttribute (attribute);
		}
		/*
		public static bool IsValidStyle (string fileName)
		{
			if (!fileName.EndsWith ("Style.xml"))
				return false;
			try {
				using (XmlTextReader reader =  new XmlTextReader (fileName)) {
					string styleName = Scan (reader, Style.NameAttribute);
					return !String.IsNullOrEmpty (styleName);
				}
			} catch (Exception) {
				return false;
			}
		}*/
		
		public static List<ValidationEventArgs> ValidateStyleFile (string fileName)
		{
			List<ValidationEventArgs> result = new List<ValidationEventArgs> ();
			XmlReaderSettings settings = new XmlReaderSettings ();
			settings.ValidationType = ValidationType.Schema;
			settings.ValidationEventHandler += delegate(object sender, ValidationEventArgs e) {
				result.Add (e);
			};
			settings.Schemas.Add (null, new XmlTextReader (typeof(SyntaxModeService).Assembly.GetManifestResourceStream ("Styles.xsd")));
			
			using (XmlReader reader = XmlReader.Create (fileName, settings)) {
				while (reader.Read ())
					;
			}
			return result;
		}
		
		
		
		public static bool IsValidSyntaxMode (string fileName)
		{
			if (!fileName.EndsWith ("SyntaxMode.xml"))
				return false;
			try {
				using (XmlTextReader reader =  new XmlTextReader (fileName)) {
					string mimeTypes = Scan (reader, SyntaxMode.MimeTypesAttribute);
					return !String.IsNullOrEmpty (mimeTypes);
				}
			} catch (Exception) {
				return false;
			}
		}
		
		public static void LoadStylesAndModes (string path)
		{
			foreach (string file in Directory.GetFiles (path)) {
				if (!file.EndsWith (".xml")) 
					continue;
				if (file.EndsWith ("SyntaxMode.xml")) {
					using (XmlTextReader reader =  new XmlTextReader (file)) {
						string mimeTypes = Scan (reader, SyntaxMode.MimeTypesAttribute);
						foreach (string mimeType in mimeTypes.Split (';')) {
							syntaxModeLookup [mimeType] = new UrlXmlProvider (file);
						}
					}
				} else if (file.EndsWith ("Style.xml")) {
					using (XmlTextReader reader =  new XmlTextReader (file)) {
						string styleName = Scan (reader, Style.NameAttribute);
						styleLookup [styleName] = new UrlXmlProvider (file);
					}
				}
			}
		}
		public static void LoadStylesAndModes (Assembly assembly)
		{
			foreach (string resource in assembly.GetManifestResourceNames ()) {
				if (!resource.EndsWith (".xml")) 
					continue;
				if (resource.EndsWith ("SyntaxMode.xml")) {
					using (Stream stream = assembly.GetManifestResourceStream (resource)) 
					using (XmlTextReader reader =  new XmlTextReader (stream)) {
						string mimeTypes = Scan (reader, SyntaxMode.MimeTypesAttribute);
						ResourceXmlProvider provider = new ResourceXmlProvider (assembly, resource);
						foreach (string mimeType in mimeTypes.Split (';')) {
							syntaxModeLookup [mimeType] = provider;
						}
					}
				} else if (resource.EndsWith ("Style.xml")) {
					using (Stream stream = assembly.GetManifestResourceStream (resource)) 
					using (XmlTextReader reader = new XmlTextReader (stream)) {
						string styleName = Scan (reader, Style.NameAttribute);
						styleLookup [styleName] = new ResourceXmlProvider (assembly, resource);
					}
				}
			}
		}

		public static void AddSyntaxMode (IXmlProvider provider)
		{
			using (XmlReader reader = provider.Open ()) {
				string mimeTypes = Scan (reader, SyntaxMode.MimeTypesAttribute);
				foreach (string mimeType in mimeTypes.Split (';')) {
					syntaxModeLookup [mimeType] = provider;
				}
			}
		}
		
		public static void RemoveSyntaxMode (IXmlProvider provider)
		{
			using (XmlReader reader = provider.Open ()) {
				string mimeTypes = Scan (reader, SyntaxMode.MimeTypesAttribute);
				foreach (string mimeType in mimeTypes.Split (';')) {
					syntaxModeLookup.Remove (mimeType);
				}
			}
		}
		
		public static void AddStyle (IXmlProvider provider)
		{
			using (XmlReader reader = provider.Open ()) {
				string styleName = Scan (reader, Style.NameAttribute);
				styleLookup [styleName] = provider;
			}
		}
		public static void RemoveStyle (IXmlProvider provider)
		{
			using (XmlReader reader = provider.Open ()) {
				string styleName = Scan (reader, Style.NameAttribute);
				styleLookup.Remove (styleName);
			}
		}
		
		static SyntaxModeService ()
		{
			StartUpdateThread ();
			LoadStylesAndModes (typeof (SyntaxModeService).Assembly);
			SyntaxModeService.GetSyntaxMode ("text/x-csharp").AddSemanticRule ("Comment", new HighlightUrlSemanticRule ("comment"));
			SyntaxModeService.GetSyntaxMode ("text/x-csharp").AddSemanticRule ("XmlDocumentation", new HighlightUrlSemanticRule ("comment"));
			SyntaxModeService.GetSyntaxMode ("text/x-csharp").AddSemanticRule ("String", new HighlightUrlSemanticRule ("string"));
			
			InstallSyntaxMode ("text/x-jay", new JaySyntaxMode ());
		}
	}
}
