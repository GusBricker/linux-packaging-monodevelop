// 
// InstrumentationService.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;

namespace MonoDevelop.Core.Instrumentation
{
	public static class InstrumentationService
	{
		static Dictionary <string, Counter> counters;
		static List<CounterCategory> categories;
		static bool enabled = true;
		static DateTime startTime;
		
		static InstrumentationService ()
		{
			counters = new Dictionary <string, Counter> ();
			categories = new List<CounterCategory> ();
			startTime = DateTime.Now;
		}
		
		public static bool Enabled {
			get { return enabled; }
			set { enabled = value; }
		}
		
		public static DateTime StartTime {
			get { return startTime; }
		}
		
		public static Counter CreateCounter (string name)
		{
			return CreateCounter (name, null);
		}
		
		public static Counter CreateCounter (string name, string category)
		{
			if (category == null)
				category = "Global";
				
			lock (counters) {
				CounterCategory cat = GetCategory (category);
				if (cat == null) {
					cat = new CounterCategory (category);
					categories.Add (cat);
				}
				
				Counter c = new Counter (name, cat);
				cat.AddCounter (c);
				counters [name] = c;
				return c;
			}
		}
		
		public static MemoryProbe CreateMemoryProbe (string name)
		{
			return CreateMemoryProbe (name, null);
		}
		
		public static MemoryProbe CreateMemoryProbe (string name, string category)
		{
			if (!enabled)
				return null;
			
			Counter c;
			lock (counters) {
				if (!counters.TryGetValue (name, out c))
					c = CreateCounter (name, category);
			}
			return new MemoryProbe (c);
		}
		
		public static Counter CreateTimerCounter (string name)
		{
			return CreateTimerCounter (name, null);
		}
		
		public static Counter CreateTimerCounter (string name, string category)
		{
			Counter c = CreateCounter (name, category);
			c.DisplayMode = CounterDisplayMode.Line;
			return c;
		}
		
		public static Counter GetCounter (string name)
		{
			lock (counters) {
				Counter c;
				counters.TryGetValue (name, out c);
				return c;
			}
		}
		
		public static CounterCategory GetCategory (string name)
		{
			lock (counters) {
				foreach (CounterCategory cat in categories)
					if (cat.Name == name)
						return cat;
				return null;
			}
		}
		
		public static IEnumerable<CounterCategory> GetCategories ()
		{
			lock (counters) {
				return new List<CounterCategory> (categories);
			}
		}
		
		public static void Dump ()
		{
			foreach (CounterCategory cat in categories) {
				Console.WriteLine (cat.Name);
				Console.WriteLine (new string ('-', cat.Name.Length));
				Console.WriteLine ();
				foreach (Counter c in cat.Counters)
					Console.WriteLine ("{0,-6} {1,-6} : {2}", c.Count, c.TotalCount, c.Name);
				Console.WriteLine ();
			}
		}
	}
}
