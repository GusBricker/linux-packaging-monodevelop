//
// CodeCompletionBugTests.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using NUnit.Framework;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.CSharp.Parser;
using MonoDevelop.CSharp.Resolver;
using MonoDevelop.CSharp.Completion;

namespace MonoDevelop.CSharpBinding.Tests
{
	[TestFixture()]
	public class CodeCompletionBugTests : UnitTests.TestBase
	{
		static int pcount = 0;
		
		public static CompletionDataList CreateProvider (string text)
		{
			return CreateProvider (text, false);
		}
		
		public static CompletionDataList CreateCtrlSpaceProvider (string text)
		{
			return CreateProvider (text, true);
		}
		
		static CompletionDataList CreateProvider (string text, bool isCtrlSpace)
		{
			string parsedText;
			string editorText;
			int cursorPosition = text.IndexOf ('$');
			int endPos = text.IndexOf ('$', cursorPosition + 1);
			if (endPos == -1)
				parsedText = editorText = text.Substring (0, cursorPosition) + text.Substring (cursorPosition + 1);
			else {
				parsedText = text.Substring (0, cursorPosition) + new string (' ', endPos - cursorPosition) + text.Substring (endPos + 1);
				editorText = text.Substring (0, cursorPosition) + text.Substring (cursorPosition + 1, endPos - cursorPosition - 1) + text.Substring (endPos + 1);
				cursorPosition = endPos - 1; 
			}
			
			TestWorkbenchWindow tww = new TestWorkbenchWindow ();
			TestViewContent sev = new TestViewContent ();
			DotNetProject project = new DotNetAssemblyProject ("C#");
			project.FileName = "/tmp/a" + pcount + ".csproj";
			
			string file = "/tmp/test-file-" + (pcount++) + ".cs";
			project.AddFile (file);
			
			ProjectDomService.Load (project);
			ProjectDom dom = ProjectDomService.GetProjectDom (project);
			dom.ForceUpdate (true);
			ProjectDomService.Parse (project, file, null, delegate { return parsedText; });
			ProjectDomService.Parse (project, file, null, delegate { return parsedText; });
			
			sev.Project = project;
			sev.ContentName = file;
			sev.Text = editorText;
			sev.CursorPosition = cursorPosition;
			tww.ViewContent = sev;
			Document doc = new Document (tww);
			doc.ParsedDocument = new NRefactoryParser ().Parse (null, sev.ContentName, parsedText);
			foreach (var e in doc.ParsedDocument.Errors)
				Console.WriteLine (e);
			CSharpTextEditorCompletion textEditorCompletion = new CSharpTextEditorCompletion (doc);
			
			int triggerWordLength = 1;
			CodeCompletionContext ctx = new CodeCompletionContext ();
			ctx.TriggerOffset = sev.CursorPosition;
			int line, column;
			sev.GetLineColumnFromPosition (sev.CursorPosition, out line, out column);
			ctx.TriggerLine = line;
			ctx.TriggerLineOffset = column;
			
			if (isCtrlSpace)
				return textEditorCompletion.CodeCompletionCommand (ctx) as CompletionDataList;
			else
				return textEditorCompletion.HandleCodeCompletion (ctx, editorText[cursorPosition - 1] , ref triggerWordLength) as CompletionDataList;
		}
		
		public static void CheckObjectMembers (CompletionDataList provider)
		{
			Assert.IsNotNull (provider.Find ("Equals"), "Method 'System.Object.Equals' not found.");
			Assert.IsNotNull (provider.Find ("GetHashCode"), "Method 'System.Object.GetHashCode' not found.");
			Assert.IsNotNull (provider.Find ("GetType"), "Method 'System.Object.GetType' not found.");
			Assert.IsNotNull (provider.Find ("ToString"), "Method 'System.Object.ToString' not found.");
		}
		
		public static void CheckProtectedObjectMembers (CompletionDataList provider)
		{
			CheckObjectMembers (provider);
			Assert.IsNotNull (provider.Find ("MemberwiseClone"), "Method 'System.Object.MemberwiseClone' not found.");
		}
		
		public static void CheckStaticObjectMembers (CompletionDataList provider)
		{
			Assert.IsNotNull (provider.Find ("Equals"), "Method 'System.Object.Equals' not found.");
			Assert.IsNotNull (provider.Find ("ReferenceEquals"), "Method 'System.Object.ReferenceEquals' not found.");
		}
		
		[Test()]
		public void TestSimpleCodeCompletion ()
		{
			CompletionDataList provider = CreateProvider (
@"class Test { public void TM1 () {} public void TM2 () {} public int TF1; }
class CCTest {
void TestMethod ()
{
	Test t;
	$t.$
}
}
");
			Assert.IsNotNull (provider);
			Assert.AreEqual (7, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("TM1"));
			Assert.IsNotNull (provider.Find ("TM2"));
			Assert.IsNotNull (provider.Find ("TF1"));
		}
		[Test()]
		public void TestSimpleInterfaceCodeCompletion ()
		{
			CompletionDataList provider = CreateProvider (
@"interface ITest { void TM1 (); void TM2 (); int TF1 { get; } }
class CCTest {
void TestMethod ()
{
	ITest t;
	$t.$
}
}
");
			Assert.IsNotNull (provider);
			Assert.AreEqual (7, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("TM1"));
			Assert.IsNotNull (provider.Find ("TM2"));
			Assert.IsNotNull (provider.Find ("TF1"));
		}

		/// <summary>
		/// Bug 399695 - Code completion not working with an enum in a different namespace
		/// </summary>
		[Test()]
		public void TestBug399695 ()
		{
			CompletionDataList provider = CreateProvider (
@"namespace Other { enum TheEnum { One, Two } }
namespace ThisOne { 
        public class Test {
                public Other.TheEnum TheEnum {
                        set { }
                }

                public void TestMethod () {
                        $TheEnum = $
                }
        }
}");
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("Other.TheEnum"), "Other.TheEnum not found.");
		}

		/// <summary>
		/// Bug 318834 - autocompletion kicks in when inputting decimals
		/// </summary>
		[Test()]
		public void TestBug318834 ()
		{
			CompletionDataList provider = CreateProvider (
@"class T
{
        static void Main ()
        {
                $decimal foo = 0.$
        }
}

");
			Assert.IsNull (provider);
		}

		/// <summary>
		/// Bug 321306 - Code completion doesn't recognize child namespaces
		/// </summary>
		[Test()]
		public void TestBug321306 ()
		{
			CompletionDataList provider = CreateProvider (
@"namespace a
{
	namespace b
	{
		public class c
		{
			public static int hi;
		}
	}
	
	public class d
	{
		public d ()
		{
			$b.$
		}
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (1, provider.Count);
			Assert.IsNotNull (provider.Find ("c"), "class 'c' not found.");
		}

		/// <summary>
		/// Bug 322089 - Code completion for indexer
		/// </summary>
		[Test()]
		public void TestBug322089 ()
		{
			CompletionDataList provider = CreateProvider (
@"class AClass
{
	public int AField;
	public int BField;
}

class Test
{
	public void TestMethod ()
	{
		AClass[] list = new AClass[0];
		$list[0].$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (6, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("AField"), "field 'AField' not found.");
			Assert.IsNotNull (provider.Find ("BField"), "field 'BField' not found.");
		}
		
		/// <summary>
		/// Bug 323283 - Code completion for indexers offered by generic types (generics)
		/// </summary>
		[Test()]
		public void TestBug323283 ()
		{
			CompletionDataList provider = CreateProvider (
@"class AClass
{
	public int AField;
	public int BField;
}

class MyClass<T>
{
	public T this[int i] {
		get {
			return default (T);
		}
	}
}

class Test
{
	public void TestMethod ()
	{
		MyClass<AClass> list = new MyClass<AClass> ();
		$list[0].$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (6, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("AField"), "field 'AField' not found.");
			Assert.IsNotNull (provider.Find ("BField"), "field 'BField' not found.");
		}

		/// <summary>
		/// Bug 323317 - Code completion not working just after a constructor
		/// </summary>
		[Test()]
		public void TestBug323317 ()
		{
			CompletionDataList provider = CreateProvider (
@"class AClass
{
	public int AField;
	public int BField;
}

class Test
{
	public void TestMethod ()
	{
		$new AClass().$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (6, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("AField"), "field 'AField' not found.");
			Assert.IsNotNull (provider.Find ("BField"), "field 'BField' not found.");
		}
		
		/// <summary>
		/// Bug 325509 - Inaccessible methods displayed in autocomplete
		/// </summary>
		[Test()]
		public void TestBug325509 ()
		{
			CompletionDataList provider = CreateProvider (
@"class AClass
{
	public int A;
	public int B;
	
	protected int C;
	int D;
}

class Test
{
	public void TestMethod ()
	{
		AClass a;
		$a.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (6, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("A"), "field 'A' not found.");
			Assert.IsNotNull (provider.Find ("B"), "field 'B' not found.");
		}

		/// <summary>
		/// Bug 338392 - MD tries to use types when declaring namespace
		/// </summary>
		[Test()]
		public void TestBug338392 ()
		{
			CompletionDataList provider = CreateProvider (
@"namespace A
{
        class C
        {
        }
}

$namespace A.$
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (0, provider.Count);
		}

		/// <summary>
		/// Bug 427284 - Code Completion: class list shows the full name of classes
		/// </summary>
		[Test()]
		public void TestBug427284 ()
		{
			CompletionDataList provider = CreateProvider (
@"namespace TestNamespace
{
        class Test
        {
        }
}
class TestClass
{
	void Method ()
	{
		$TestNamespace.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (1, provider.Count);
			Assert.IsNotNull (provider.Find ("Test"), "class 'Test' not found.");
		}

		/// <summary>
		/// Bug 427294 - Code Completion: completion on values returned by methods doesn't work
		/// </summary>
		[Test()]
		public void TestBug427294 ()
		{
			CompletionDataList provider = CreateProvider (
@"class TestClass
{
	public TestClass GetTestClass ()
	{
	}
}

class Test
{
	public void TestMethod ()
	{
		TestClass a;
		$a.GetTestClass ().$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (5, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("GetTestClass"), "method 'GetTestClass' not found.");
		}
		
		/// <summary>
		/// Bug 405000 - Namespace alias qualifier operator (::) does not trigger code completion
		/// </summary>
		[Test()]
		public void TestBug405000 ()
		{
			CompletionDataList provider = CreateProvider (
@"namespace A {
	class Test
	{
	}
}

namespace B {
	using foo = A;
	class C
	{
		public static void Main ()
		{
			$foo::$
		}
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (1, provider.Count);
			Assert.IsNotNull (provider.Find ("Test"), "class 'Test' not found.");
		}
		
		/// <summary>
		/// Bug 427649 - Code Completion: protected methods shown in code completion
		/// </summary>
		[Test()]
		public void TestBug427649 ()
		{
			CompletionDataList provider = CreateProvider (
@"class BaseClass
{
	protected void ProtecedMember ()
	{
	}
}

class C : BaseClass
{
	public static void Main ()
	{
		BaseClass bc;
		$bc.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (4, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
		}
		
		/// <summary>
		/// Bug 427734 - Code Completion issues with enums
		/// </summary>
		[Test()]
		public void TestBug427734A ()
		{
			CompletionDataList provider = CreateProvider (
@"public class Test
{
	public enum SomeEnum { a,b }
	
	public void Run ()
	{
		$Test.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (3, provider.Count);
			CodeCompletionBugTests.CheckStaticObjectMembers (provider); // 2 from System.Object
			Assert.IsNotNull (provider.Find ("SomeEnum"), "enum 'SomeEnum' not found.");
		}
		
		/// <summary>
		/// Bug 427734 - Code Completion issues with enums
		/// </summary>
		[Test()]
		public void TestBug427734B ()
		{
			CompletionDataList provider = CreateProvider (
@"public class Test
{
	public enum SomeEnum { a,b }
	
	public void Run ()
	{
		$SomeEnum.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (2, provider.Count);
			Assert.IsNotNull (provider.Find ("a"), "enum member 'a' not found.");
			Assert.IsNotNull (provider.Find ("b"), "enum member 'b' not found.");
		}
		
		/// <summary>
		/// Bug 431764 - Completion doesn't work in properties
		/// </summary>
		[Test()]
		public void TestBug431764 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"public class Test
{
	int number;
	public int Number {
		set { $this.number = $ }
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("value"), "Should contain 'value'");
		}
		
		/// <summary>
		/// Bug 431797 - Code completion showing invalid options
		/// </summary>
		[Test()]
		public void TestBug431797A ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"public class Test
{
	private List<string> strings;
	$public $
}");
		
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("strings"), "should not contain 'strings'");
		}
		
		/// <summary>
		/// Bug 431797 - Code completion showing invalid options
		/// </summary>
		[Test()]
		public void TestBug431797B ()
		{
			CompletionDataList provider = CreateProvider (
@"public class Test
{
	public delegate string [] AutoCompleteHandler (string text, int pos);
	public void Method ()
	{
		Test t = new Test ();
		$t.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("AutoCompleteHandler"), "should not contain 'AutoCompleteHandler' delegate");
		}
		
		/// <summary>
		/// Bug 432681 - Incorrect completion in nested classes
		/// </summary>
		[Test()]
		public void TestBug432681 ()
		{
			CompletionDataList provider = CreateProvider (
@"

class C {
        public class D
        {
        }

        public void Method ()
        {
                $C.D c = new $
        }
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual ("C.D", provider.DefaultCompletionString, "Completion string is incorrect");
		}
		
		[Test()]
		public void TestGenericObjectCreation ()
		{
			CompletionDataList provider = CreateProvider (
@"
class List<T>
{
}
class Test{
	public void Method ()
	{
		$List<int> i = new $
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsTrue (provider.Find ("List<int>") != null, "List<int> not found");
		}
		
		/// <summary>
		/// Bug 431803 - Autocomplete not giving any options
		/// </summary>
		[Test()]
		public void TestBug431803 ()
		{
			CompletionDataList provider = CreateProvider (
@"public class Test
{
	public string[] GetStrings ()
	{
		$return new $
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (1, provider.Count);
			Assert.IsNotNull (provider.Find ("string"), "type string not found.");
		}

		/// <summary>
		/// Bug 434770 - No autocomplete on array types
		/// </summary>
		[Test()]
		public void TestBug434770 ()
		{
			CompletionDataList provider = CreateProvider (
@"
namespace System {
	public class Array 
	{
		public int Length {
			get {}
			set {}
		}
		public int MyField;
	}
}
public class Test
{
	public void AMethod ()
	{
		byte[] buffer = new byte[1024];
		$buffer.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Length"), "property 'Length' not found.");
			Assert.IsNotNull (provider.Find ("MyField"), "field 'MyField' not found.");
		}
		
		/// <summary>
		/// Bug 439601 - Intellisense Broken For Partial Classes
		/// </summary>
		[Test()]
		public void TestBug439601 ()
		{
			CompletionDataList provider = CreateProvider (
@"
namespace MyNamespace
{
	partial class FormMain
	{
		private void Foo()
		{
			Bar();
		}
		
		private void Blah()
		{
			Foo();
		}
	}
}

namespace MyNamespace
{
	public partial class FormMain
	{
		public FormMain()
		{
		}
		
		private void Bar()
		{
			$this.$
		}
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Foo"), "method 'Foo' not found.");
			Assert.IsNotNull (provider.Find ("Blah"), "method 'Blah' not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "method 'Bar' not found.");
		}
		
		/// <summary>
		/// Bug 432434 - Code completion doesn't work with subclasses
		/// </summary>
		[Test()]
		public void TestBug432434 ()
		{
			CompletionDataList provider = CreateProvider (

@"public class Test
{
	public class Inner
	{
		public void Inner1 ()
		{
		}
		
		public void Inner2 ()
		{
		}
	}
	
	public void Run ()
	{
		Inner inner = new Inner ();
		$inner.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Inner1"), "Method inner1 not found.");
			Assert.IsNotNull (provider.Find ("Inner2"), "Method inner2 not found.");
		}

		/// <summary>
		/// Bug 432434A - Code completion doesn't work with subclasses
		/// </summary>
		[Test()]
		public void TestBug432434A ()
		{
			CompletionDataList provider = CreateProvider (

@"    public class E
        {
                public class Inner
                {
                        public void Method ()
                        {
                                Inner inner = new Inner();
                                $inner.$
                        }
                }
        }
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Method"), "Method 'Method' not found.");
		}
		
		/// <summary>
		/// Bug 432434B - Code completion doesn't work with subclasses
		/// </summary>
		[Test()]
		public void TestBug432434B ()
		{
			CompletionDataList provider = CreateProvider (

@"  public class E
        {
                public class Inner
                {
                        public class ReallyInner $: $
                        {

                        }
                }
        }
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("E"), "Class 'E' not found.");
			Assert.IsNotNull (provider.Find ("Inner"), "Class 'Inner' not found.");
			Assert.IsNull (provider.Find ("ReallyInner"), "Class 'ReallyInner' found, but shouldn't.");
		}
		

		/// <summary>
		/// Bug 436705 - code completion for constructors does not handle class name collisions properly
		/// </summary>
		[Test()]
		public void TestBug436705 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public class Point
{
}

class C {

        public void Method ()
        {
                $System.Drawing.Point p = new $
        }
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual ("System.Drawing.Point", provider.DefaultCompletionString, "Completion string is incorrect");
		}
		
		/// <summary>
		/// Bug 439963 - Lacking members in code completion
		/// </summary>
		[Test()]
		public void TestBug439963 ()
		{
			CompletionDataList provider = CreateProvider (
@"public class StaticTest
{
	public void Test1()
	{}
	public void Test2()
	{}
	
	public static StaticTest GetObject ()
	{
	}
}

public class Test
{
	public void TestMethod ()
	{
		$StaticTest.GetObject ().$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Test1"), "Method 'Test1' not found.");
			Assert.IsNotNull (provider.Find ("Test2"), "Method 'Test2' not found.");
			Assert.IsNull (provider.Find ("GetObject"), "Method 'GetObject' found, but shouldn't.");
		}

		/// <summary>
		/// Bug 441671 - Finalisers show up in code completion
		/// </summary>
		[Test()]
		public void TestBug441671 ()
		{
			CompletionDataList provider = CreateProvider (
@"class TestClass
{
	public TestClass (int i)
	{
	}
	public void TestMethod ()
	{
	}
	public ~TestClass ()
	{
	}
}

class AClass
{
	void AMethod ()
	{
		TestClass c;
		$c.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (5, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNull (provider.Find (".dtor"), "destructor found - but shouldn't.");
			Assert.IsNotNull (provider.Find ("TestMethod"), "method 'TestMethod' not found.");
		}
		
		/// <summary>
		/// Bug 444110 - Code completion doesn't activate
		/// </summary>
		[Test()]
		public void TestBug444110 ()
		{
			CompletionDataList provider = CreateProvider (
@"using System;
using System.Collections.Generic;

namespace System.Collections.Generic {
	
	public class TemplateClass<T>
	{
		public T TestField;
	}
}

namespace CCTests
{
	
	public class Test
	{
		public TemplateClass<int> TemplateClass { get; set; }
	}
	
	class MainClass
	{
		public static void Main(string[] args)
		{
			Test t = new Test();
			$t.TemplateClass.$
		}
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (5, provider.Count);
			CodeCompletionBugTests.CheckObjectMembers (provider); // 4 from System.Object
			Assert.IsNotNull (provider.Find ("TestField"), "field 'TestField' not found.");
		}
		
		/// <summary>
		/// Bug 460234 - Invalid options shown when typing 'override'
		/// </summary>
		[Test()]
		public void TestBug460234 ()
		{
			CompletionDataList provider = CreateProvider (
@"
namespace System {
	public class Object
	{
		public virtual int GetHashCode ()
		{
		}
		protected virtual void Finalize ()
		{
		}
	}
}
public class TestMe : System.Object
{
	$override $
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (1, provider.Count);
			Assert.IsNull (provider.Find ("Finalize"), "method 'Finalize' found, but shouldn't.");
			Assert.IsNotNull (provider.Find ("GetHashCode"), "method 'GetHashCode' not found.");
		}

		/// <summary>
		/// Bug 457003 - code completion shows variables out of scope
		/// </summary>
		[Test()]
		public void TestBug457003 ()
		{
			CompletionDataList provider = CreateProvider (
@"
class A
{
	public void Test ()
	{
		if (true) {
			A st = null;
		}
		
		if (true) {
			int i = 0;
			$st.$
		}
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsTrue (provider.Count == 0, "variable 'st' found, but shouldn't.");
		}
		
		/// <summary>
		/// Bug 457237 - code completion doesn't show static methods when setting global variable
		/// </summary>
		[Test()]
		public void TestBug457237 ()
		{
			CompletionDataList provider = CreateProvider (
@"
class Test
{
	public static double Val = 0.5;
}

class Test2
{
	$double dd = Test.$
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Val"), "field 'Val' not found.");
		}

		/// <summary>
		/// Bug 459682 - Static methods/properties don't show up in subclasses
		/// </summary>
		[Test()]
		public void TestBug459682 ()
		{
			CompletionDataList provider = CreateProvider (
@"public class BaseC
{
	public static int TESTER;
}
public class Child : BaseC
{
	public Child()
	{
		$Child.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("TESTER"), "field 'TESTER' not found.");
		}
		
		/// <summary>
		/// Bug 466692 - Missing completion for return/break keywords after yield
		/// </summary>
		[Test()]
		public void TestBug466692 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public class TestMe 
{
	public int Test ()
	{
		$yield $
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (2, provider.Count);
			Assert.IsNotNull (provider.Find ("break"), "keyword 'break' not found");
			Assert.IsNotNull (provider.Find ("return"), "keyword 'return' not found");
		}
		
		/// <summary>
		/// Bug 467507 - No completion of base members inside explicit events
		/// </summary>
		[Test()]
		public void TestBug467507 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
using System;

class Test
{
	public void TestMe ()
	{
	}
	
	public event EventHandler TestEvent {
		add {
			$
		}
		remove {
			
		}
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("TestMe"), "method 'TestMe' not found");
			Assert.IsNotNull (provider.Find ("value"), "keyword 'value' not found");
		}
		
		/// <summary>
		/// Bug 444643 - Extension methods don't show up on array types
		/// </summary>
		[Test()]
		public void TestBug444643 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
using System;
using System.Collections.Generic;

	static class ExtensionTest
	{
		public static bool TestExt<T> (this IList<T> list, T val)
		{
			return true;
		}
	}
	
	class MainClass
	{
		public static void Main(string[] args)
		{
			$args.$
		}
	}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("TestExt"), "method 'TestExt' not found");
		}
		
		/// <summary>
		/// Bug 471935 - Code completion window not showing in MD1CustomDataItem.cs
		/// </summary>
		[Test()]
		public void TestBug471935 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
public class AClass
{
	public AClass Test ()
	{
		if (true) {
			AClass data;
			$data.$
			return data;
		} else if (false) {
			AClass data;
			return data;
		}
		return null;
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found");
		}
		
		/// <summary>
		/// Bug 471937 - Code completion of 'new' showing invorrect entries 
		/// </summary>
		[Test()]
		public void TestBug471937 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
class B
{
}

class A
{
	public void Test()
	{
		int i = 5;
		i += 5;
		$A a = new $
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNull (provider.Find ("B"), "class 'B' found, but shouldn'tj.");
		}
		
		/// <summary>
		/// Bug 473686 - Constants are not included in code completion
		/// </summary>
		[Test()]
		public void TestBug473686 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
class ATest
{
	const int TESTCONST = 0;

	static void Test()
	{
		$$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("TESTCONST"), "constant 'TESTCONST' not found.");
		}
		
		/// <summary>
		/// Bug 473849 - Classes with no visible constructor shouldn't appear in "new" completion
		/// </summary>
		[Test()]
		public void TestBug473849 ()
		{
			CompletionDataList provider = CreateProvider (
@"
class TestB
{
	protected TestB()
	{
	}
}

class TestC : TestB
{
	internal TestC ()
	{
	}
}

class TestD : TestB
{
	public TestD ()
	{
	}
}

class TestE : TestD
{
	protected TestE ()
	{
	}
}

class Test : TestB
{
	void TestMethod ()
	{
		$TestB test = new $
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNull (provider.Find ("TestE"), "class 'TestE' found, but shouldn't.");
			Assert.IsNotNull (provider.Find ("TestD"), "class 'TestD' not found");
			Assert.IsNotNull (provider.Find ("TestC"), "class 'TestC' not found");
			Assert.IsNotNull (provider.Find ("TestB"), "class 'TestB' not found");
			Assert.IsNotNull (provider.Find ("Test"), "class 'Test' not found");
		}
		
		/// <summary>
		/// Bug 474199 - Code completion not working for a nested class
		/// </summary>
		[Test()]
		public void TestBug474199A ()
		{
			CompletionDataList provider = CreateProvider (
@"
public class InnerTest
{
	public class Inner
	{
		public void Test()
		{
		}
	}
}

public class ExtInner : InnerTest
{
}

class Test
{
	public void TestMethod ()
	{
		var inner = new ExtInner.Inner ();
		$inner.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Test"), "method 'Test' not found");
		}
		
		/// <summary>
		/// Bug 474199 - Code completion not working for a nested class
		/// </summary>
		[Test()]
		public void TestBug474199B ()
		{
			IParameterDataProvider provider = ParameterCompletionTests.CreateProvider (
@"
public class InnerTest
{
	public class Inner
	{
		public Inner(string test)
		{
		}
	}
}

public class ExtInner : InnerTest
{
}

class Test
{
	public void TestMethod ()
	{
		$new ExtInner.Inner ($
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.AreEqual (1, provider.OverloadCount, "There should be one overload");
			Assert.AreEqual (1, provider.GetParameterCount(0), "Parameter 'test' should exist");
		}
		
		/// <summary>
		/// Bug 350862 - Autocomplete bug with enums
		/// </summary>
		[Test()]
		public void TestBug350862 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public enum MyEnum {
	A,
	B,
	C
}

public class Test
{
	MyEnum item;
	public void Method (MyEnum val)
	{
		$item = $
	}
}

");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("val"), "parameter 'val' not found");
		}
		
		/// <summary>
		/// Bug 470954 - using System.Windows.Forms is not honored
		/// </summary>
		[Test()]
		public void TestBug470954 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public class Control
{
	public MouseButtons MouseButtons { get; set; }
}

public enum MouseButtons {
	Left, Right
}

public class SomeControl : Control
{
	public void Run ()
	{
		$MouseButtons m = MouseButtons.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("Left"), "enum 'Left' not found");
			Assert.IsNotNull (provider.Find ("Right"), "enum 'Right' not found");
		}
		
		/// <summary>
		/// Bug 470954 - using System.Windows.Forms is not honored
		/// </summary>
		[Test()]
		public void TestBug470954_bis ()
		{
			CompletionDataList provider = CreateProvider (
@"
public class Control
{
	public string MouseButtons { get; set; }
}

public enum MouseButtons {
	Left, Right
}

public class SomeControl : Control
{
	public void Run ()
	{
		$int m = MouseButtons.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNull (provider.Find ("Left"), "enum 'Left' found");
			Assert.IsNull (provider.Find ("Right"), "enum 'Right' found");
		}
		
		/// <summary>
		/// Bug 487236 - Object initializer completion uses wrong type
		/// </summary>
		[Test()]
		public void TestBug487236 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
public class A
{
	public string Name { get; set; }
}

class MyTest
{
	public void Test ()
	{
		$var x = new A () { $
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("Name"), "property 'Name' not found.");
		}
		
		/// <summary>
		/// Bug 487236 - Object initializer completion uses wrong type
		/// </summary>
		[Test()]
		public void TestBug487236B ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
public class A
{
	public string Name { get; set; }
}

class MyTest
{
	public void Test ()
	{
		$A x = new NotExists () { $
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNull (provider.Find ("Name"), "property 'Name' found, but shouldn't'.");
		}
		
		/// <summary>
		/// Bug 487228 - No intellisense for implicit arrays
		/// </summary>
		[Test()]
		public void TestBug487228 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public class Test
{
	public void Method ()
	{
		var v = new [] { new Test () };
		$v[0].$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("Method"), "method 'Method' not found");
		}
		
		/// <summary>
		/// Bug 487218 - var does not work with arrays
		/// </summary>
		[Test()]
		public void TestBug487218 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public class Test
{
	public void Method ()
	{
		var v = new Test[] { new Test () };
		$v[0].$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("Method"), "method 'Method' not found");
		}
		
		/// <summary>
		/// Bug 487206 - Intellisense not working
		/// </summary>
		[Test()]
		public void TestBug487206 ()
		{
			CompletionDataList provider = CreateProvider (
@"
class CastByExample
{
	static T Cast<T> (object obj, T type)
	{
		return (T) obj;
	}
	
	static void Main ()
	{
		var typed = Cast (o, new { Foo = 5 });
		$typed.$
	}
}");
			Assert.IsNotNull (provider, "provider not found.");
			
			Assert.IsNotNull (provider.Find ("Foo"), "property 'Foo' not found");
		}

		/// <summary>
		/// Bug 487203 - Extension methods not working
		/// </summary>
		[Test()]
		public void TestBug487203 ()
		{
			CompletionDataList provider = CreateProvider (
@"
using System;
using System.Collections.Generic;

static class Linq
{
	public static IEnumerable<T> Select<S, T> (this IEnumerable<S> collection, Func<S, T> func)
	{
	}
}

class Program 
{
	public void Foo ()
	{
		Program[] prgs;
		foreach (var prg in (from Program p in prgs select p)) {
			$prg.$
		}
	}
}		
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Foo"), "method 'Foo' not found");
		}
		
		/// <summary>
		/// Bug 491020 - Wrong typeof intellisense
		/// </summary>
		[Test()]
		public void TestBug491020 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public class EventClass<T>
{
	public class Inner {}
	public delegate void HookDelegate (T del);
	public void Method ()
	{}
}

public class Test
{
	public static void Main ()
	{
		$EventClass<int>.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Inner"), "class 'Inner' not found.");
			Assert.IsNotNull (provider.Find ("HookDelegate"), "delegate 'HookDelegate' not found.");
			Assert.IsNull (provider.Find ("Method"), "method 'Method' found, but shouldn't.");
		}
		
		/// <summary>
		/// Bug 491020 - Wrong typeof intellisense
		/// It's a different case when the class is inside a namespace.
		/// </summary>
		[Test()]
		public void TestBug491020B ()
		{
			CompletionDataList provider = CreateProvider (
@"

namespace A {
	public class EventClass<T>
	{
		public class Inner {}
		public delegate void HookDelegate (T del);
		public void Method ()
		{}
	}
}

public class Test
{
	public static void Main ()
	{
		$A.EventClass<int>.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Inner"), "class 'Inner' not found.");
			Assert.IsNotNull (provider.Find ("HookDelegate"), "delegate 'HookDelegate' not found.");
			Assert.IsNull (provider.Find ("Method"), "method 'Method' found, but shouldn't.");
		}
		
		/// <summary>
		/// Bug 491019 - No intellisense for recursive generics
		/// </summary>
		[Test()]
		public void TestBug491019 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public abstract class NonGenericBase
{
	public abstract int this[int i] { get; }
}

public abstract class GenericBase<T> : NonGenericBase where T : GenericBase<T>
{
	T Instance { get { return default (T); } }

	public void Foo ()
	{
		$Instance.Instance.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Instance"), "class 'Inner' not found.");
			Assert.IsNull (provider.Find ("this"), "'this' found, but shouldn't.");
		}
		
		
				
		/// <summary>
		/// Bug 429034 - Class alias completion not working properly
		/// </summary>
		[Test()]
		public void TestBug429034 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
using Path = System.IO.Path;

class Test
{
	void Test ()
	{
		$$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Path"), "class 'Path' not found.");
		}	
		
		/// <summary>
		/// Bug 429034 - Class alias completion not working properly
		/// </summary>
		[Test()]
		public void TestBug429034B ()
		{
			CompletionDataList provider = CreateProvider (
@"
using Path = System.IO.Path;

class Test
{
	void Test ()
	{
		$Path.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("DirectorySeparatorChar"), "method 'PathTest' not found.");
		}
		
		[Test()]
		public void TestInvalidCompletion ()
		{
			CompletionDataList provider = CreateProvider (
@"
class TestClass
{
	public void TestMethod ()
	{
	}
}

class Test
{
	public void Foo ()
	{
		TestClass tc;
		$tc.garbage.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("TestMethod"), "method 'TestMethod' found, but shouldn't.");
		}
		
		/// <summary>
		/// Bug 510919 - Code completion does not show interface method when not using a local var 
		/// </summary>
		[Test()]
		public void TestBug510919 ()
		{
			CompletionDataList provider = CreateProvider (
@"
public class Foo : IFoo 
{
	public void Bar () { }
}

public interface IFoo 
{
	void Bar ();
}

public class Program
{
	static IFoo GiveMeFoo () 
	{
		return new Foo ();
	}

	static void Main ()
	{
		$GiveMeFoo ().$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "method 'Bar' not found.");
		}
		
			
		/// <summary>
		/// Bug 526667 - wrong code completion in object initialisation (new O() {...};)
		/// </summary>
		[Test()]
		public void TestBug526667 ()
		{
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
using System;
using System.Collections.Generic;

public class O
{
	public string X {
		get;
		set;
	}
	public string Y {
		get;
		set;
	}
	public List<string> Z {
		get;
		set;
	}

	public static O A ()
	{
		return new O {
			X = ""x"",
			Z = new List<string> (new string[] {
				""abc"",
				""def""
			})
			$, $
		};
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Y"), "property 'Y' not found.");
		}
		
		
			
		/// <summary>
		/// Bug 538208 - Go to declaration not working over a generic method...
		/// </summary>
		[Test()]
		public void TestBug538208 ()
		{
			// We've to test 2 expressions for this bug. Since there are 2 ways of accessing
			// members.
			// First: the identifier expression
			CompletionDataList provider = CreateCtrlSpaceProvider (
@"
class MyClass
{
	public string Test { get; set; }
	
	T foo<T>(T arg)
	{
		return arg;
	}

	public void Main(string[] args)
	{
		var myObject = foo<MyClass>(new MyClass());
		$myObject.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Test"), "property 'Test' not found.");
			
			// now the member reference expression 
			provider = CreateCtrlSpaceProvider (
@"
class MyClass2
{
	public string Test { get; set; }
	
	T foo<T>(T arg)
	{
		return arg;
	}

	public void Main(string[] args)
	{
		var myObject = this.foo<MyClass2>(new MyClass2());
		$myObject.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Test"), "property 'Test' not found.");
		}
		
		/// <summary>
		/// Bug 542976 resolution problem
		/// </summary>
		[Test()]
		public void TestBug542976 ()
		{
				CompletionDataList provider = CreateProvider (
@"
class KeyValuePair<S, T>
{
	public S Key { get; set;}
	public T Value { get; set;}
}

class TestMe<T> : System.Collections.Generic.IEnumerable<T>
{
	public System.Collections.Generic.IEnumerator<T> GetEnumerator ()
	{
		throw new System.NotImplementedException();
	}

	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
	{
		throw new System.NotImplementedException();
	}
}

namespace TestMe 
{
	class Bar
	{
		public int Field;
	}
	
	class Test
	{
		void Foo (TestMe<KeyValuePair<Bar, int>> things)
		{
			foreach (var thing in things) {
				$thing.Key.$
			}
		}
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Field"), "field 'Field' not found.");
		}
		
		
		/// <summary>
		/// Bug 545189 - C# resolver bug
		/// </summary>
		[Test()]
		public void TestBug545189A ()
		{
				CompletionDataList provider = CreateProvider (
@"
class A<T>
{
	class B
	{
		public T field;
	}
}

public class Foo
{
	public void Bar ()
	{
		A<Foo>.B baz = new A<Foo>.B ();
		$baz.field.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Bar"), "method 'Bar' not found.");
		}
		
		/// <summary>
		/// Bug 549864 - Intellisense does not work properly with expressions
		/// </summary>
		[Test()]
		public void TestBug549864 ()
		{
				CompletionDataList provider = CreateProvider (
@"
delegate T MyFunc<S, T> (S t);

class TestClass
{
    public string Value
    {
        get;
        set;
    }
	
    public static object GetProperty<TType> (MyFunc<TType, object> expression)
	{
		return null;
    }
    private static object ValueProperty = TestClass.GetProperty<TestClass> ($x => x.$);

}

");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("Value"), "property 'Value' not found.");
		}
		
		
		/// <summary>
		/// Bug 550185 - Intellisence for extension methods
		/// </summary>
		[Test()]
		public void TestBug550185 ()
		{
				CompletionDataList provider = CreateProvider (
@"
public interface IMyinterface<T> {
	T Foo ();
}

public static class ExtMethods {
	public static int MyCountMethod(this IMyinterface<string> i)
	{
		return 0;
	}
}

class TestClass
{
	void Test ()
	{
		IMyinterface<int> test;
		$test.$
	}
}

");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNull (provider.Find ("MyCountMet2hod"), "method 'MyCountMethod' found, but shouldn't.");
		}
		
			
		/// <summary>
		/// Bug 553101 – Enum completion does not use type aliases
		/// </summary>
		[Test()]
		public void TestBug553101 ()
		{
				CompletionDataList provider = CreateProvider (
@"
namespace Some.Type 
{
	public enum Name { Foo, Bar }
}

namespace Test
{
	using STN = Some.Type.Name;
	
	public class Main
	{
		public void TestMe ()
		{
			$STN foo = $
		}
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
		}
			
		/// <summary>
		/// Bug 555523 - C# code completion gets confused by extension methods with same names as properties
		/// </summary>
		[Test()]
		public void TestBug555523A ()
		{
				CompletionDataList provider = CreateProvider (
@"
class A
{
	public int AA { get; set; }
}

class B
{
	public int BB { get; set; }
}

static class ExtMethod
{
	public static A Extension (this MyClass myClass)
	{
		return null;
	}
}

class MyClass
{
	public B Extension {
		get;
		set;
	}
}

class MainClass
{
	public static void Main (string[] args)
	{
		MyClass myClass;
		$myClass.Extension ().$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("AA"), "property 'AA' not found.");
		}
		
		/// <summary>
		/// Bug 555523 - C# code completion gets confused by extension methods with same names as properties
		/// </summary>
		[Test()]
		public void TestBug555523B ()
		{
				CompletionDataList provider = CreateProvider (
@"
class A
{
	public int AA { get; set; }
}

class B
{
	public int BB { get; set; }
}

static class ExtMethod
{
	public static A Extension (this MyClass myClass)
	{
		return null;
	}
}

class MyClass
{
	public B Extension {
		get;
		set;
	}
}

class MainClass
{
	public static void Main (string[] args)
	{
		MyClass myClass;
		$myClass.Extension.$
	}
}
");
			Assert.IsNotNull (provider, "provider not found.");
			Assert.IsNotNull (provider.Find ("BB"), "property 'BB' not found.");
		}
		
	}
}
