using NUnit.Framework;
using RefactoringEssentials.CSharp.CodeRefactorings;

namespace RefactoringEssentials.Tests.CSharp.CodeRefactorings
{
    [TestFixture]
    public class AutoLinqSumActionTests : CSharpCodeRefactoringTestBase
    {
        [Test, Ignore("Not implemented!")]
        public void TestSimpleIntegerLoop()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2, 3 };
		$foreach (var x in list)
			result += x;
	}
}";

            string result = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2, 3 };
		result += list.Sum ();
	}
}";

            Assert.AreEqual(result, RunContextAction(new AutoLinqSumAction(), source));
        }

        [Test, Ignore("Not implemented!")]
        public void TestMergedIntegerLoop()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		var list = new int[] { 1, 2, 3 };
		int result = 0;
		$foreach (var x in list)
			result += x;
	}
}";

            string result = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		var list = new int[] { 1, 2, 3 };
		int result = list.Sum ();
	}
}";

            Assert.AreEqual(result, RunContextAction(new AutoLinqSumAction(), source));
        }

        [Test, Ignore("Not implemented!")]
        public void TestNonZeroMergedIntegerLoop()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		var list = new int[] { 1, 2, 3 };
		int result = 1;
		$foreach (var x in list)
			result += x;
	}
}";

            string result = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		var list = new int[] { 1, 2, 3 };
		int result = 1 + list.Sum ();
	}
}";

            Assert.AreEqual(result, RunContextAction(new AutoLinqSumAction(), source));
        }

        [Test, Ignore("Not implemented!")]
        public void TestMergedAssignmentIntegerLoop()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		var list = new int[] { 1, 2, 3 };
		int result;
		result = 1;
		$foreach (var x in list)
			result += x;
	}
}";

            string result = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		var list = new int[] { 1, 2, 3 };
		int result;
		result = 1 + list.Sum ();
	}
}";

            Assert.AreEqual(result, RunContextAction(new AutoLinqSumAction(), source));
        }

        [Test, Ignore("Not implemented!")]
        public void TestMergedDecimal()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		var list = new int[] { 1, 2, 3 };
		decimal result = 0.0m;
		$foreach (var x in list)
			result += x;
	}
}";

            string result = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		var list = new int[] { 1, 2, 3 };
		decimal result = list.Sum ();
	}
}";

            Assert.AreEqual(result, RunContextAction(new AutoLinqSumAction(), source));
        }

        [Test, Ignore("Not implemented!")]
        public void TestIntegerLoopInBlock()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2, 3 };
		$foreach (var x in list) {
			result += x;
		}
	}
}";

            string result = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2, 3 };
		result += list.Sum ();
	}
}";

            Assert.AreEqual(result, RunContextAction(new AutoLinqSumAction(), source));
        }

        [Test, Ignore("Not implemented!")]
        public void TestExpression()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2, 3 };
		$foreach (var x in list) {
			result += x * 2;
		}
	}
}";

            string result = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2, 3 };
		result += list.Sum (x => x * 2);
	}
}";

            Assert.AreEqual(result, RunContextAction(new AutoLinqSumAction(), source));
        }

        [Test]
        public void TestDisabledForStrings()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		string result = string.Empty;
		var list = new string[] { ""a"", ""b"" };
		$foreach (var x in list) {
			result += x;
		}
	}
}";
            TestWrongContext<AutoLinqSumAction>(source);
        }

        [Test, Ignore("Not implemented!")]
        public void TestShort()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		short result = 0;
		var list = new short[] { 1, 2, 3 };
		$foreach (var x in list)
			result += x;
	}
}";

            string result = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		short result = 0;
		var list = new short[] { 1, 2, 3 };
		result += list.Sum ();
	}
}";

            Assert.AreEqual(result, RunContextAction(new AutoLinqSumAction(), source));
        }

        [Test, Ignore("Not implemented!")]
        public void TestLong()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		long result = 0;
		var list = new long[] { 1, 2, 3 };
		$foreach (var x in list)
			result += x;
	}
}";

            string result = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		long result = 0;
		var list = new long[] { 1, 2, 3 };
		result += list.Sum ();
	}
}";

            Assert.AreEqual(result, RunContextAction(new AutoLinqSumAction(), source));
        }

        [Test, Ignore("Not implemented!")]
        public void TestUnsignedLong()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		ulong result = 0;
		var list = new ulong[] { 1, 2, 3 };
		$foreach (var x in list)
			result += x;
	}
}";

            string result = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		ulong result = 0;
		var list = new ulong[] { 1, 2, 3 };
		result += list.Sum ();
	}
}";

            Assert.AreEqual(result, RunContextAction(new AutoLinqSumAction(), source));
        }

        [Test, Ignore("Not implemented!")]
        public void TestFloat()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		float result = 0;
		var list = new float[] { 1, 2, 3 };
		$foreach (var x in list)
			result += x;
	}
}";

            string result = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		float result = 0;
		var list = new float[] { 1, 2, 3 };
		result += list.Sum ();
	}
}";

            Assert.AreEqual(result, RunContextAction(new AutoLinqSumAction(), source));
        }

        [Test, Ignore("Not implemented!")]
        public void TestDouble()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		double result = 0;
		var list = new double[] { 1, 2, 3 };
		$foreach (var x in list)
			result += x;
	}
}";

            string result = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		double result = 0;
		var list = new double[] { 1, 2, 3 };
		result += list.Sum ();
	}
}";

            Assert.AreEqual(result, RunContextAction(new AutoLinqSumAction(), source));
        }

        [Test, Ignore("Not implemented!")]
        public void TestDecimal()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		decimal result = 0;
		var list = new decimal[] { 1, 2, 3 };
		$foreach (var x in list)
			result += x;
	}
}";

            string result = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		decimal result = 0;
		var list = new decimal[] { 1, 2, 3 };
		result += list.Sum ();
	}
}";

            Assert.AreEqual(result, RunContextAction(new AutoLinqSumAction(), source));
        }

        [Test, Ignore("Not implemented!")]
        public void TestMinus()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2, 3 };
		$foreach (var x in list) {
			result -= x;
		}
	}
}";

            string result = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2, 3 };
		result += list.Sum (x => -x);
	}
}";

            Assert.AreEqual(result, RunContextAction(new AutoLinqSumAction(), source));
        }

        [Test, Ignore("Not implemented!")]
        public void TestCombined()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2, 3 };
		$foreach (var x in list) {
			result += x;
			result += 2 * x;
		}
	}
}";

            string result = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2, 3 };
		result += list.Sum (x => x + 2 * x);
	}
}";

            Assert.AreEqual(result, RunContextAction(new AutoLinqSumAction(), source));
        }

        [Test, Ignore("Not implemented!")]
        public void TestCombinedPrecedence()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2, 3 };
		$foreach (var x in list) {
			result += x;
			result += x << 1;
		}
	}
}";

            string result = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2, 3 };
		result += list.Sum (x => x + (x << 1));
	}
}";

            Assert.AreEqual(result, RunContextAction(new AutoLinqSumAction(), source));
        }

        [Test, Ignore("Not implemented!")]
        public void TestEmptyStatements()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2, 3 };
		$foreach (var x in list) {
			result += x;
			;
		}
	}
}";

            string result = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2, 3 };
		result += list.Sum ();
	}
}";

            Assert.AreEqual(result, RunContextAction(new AutoLinqSumAction(), source));
        }

        [Test, Ignore("Not implemented!")]
        public void TestSimpleConditional()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2, 3 };
		$foreach (var x in list) {
			if (x > 0)
				result += x;
		}
	}
}";

            string result = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2, 3 };
		result += list.Where (x => x > 0).Sum ();
	}
}";

            Assert.AreEqual(result, RunContextAction(new AutoLinqSumAction(), source));
        }

        [Test, Ignore("Not implemented!")]
        public void TestInvertedConditional()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2, 3 };
		$foreach (var x in list) {
			if (x > 0)
				;
			else
				result += x;
		}
	}
}";

            string result = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2, 3 };
		result += list.Where (x => x <= 0).Sum ();
	}
}";

            Assert.AreEqual(result, RunContextAction(new AutoLinqSumAction(), source));
        }

        [Test, Ignore("Not implemented!")]
        public void TestIncrement()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2, 3 };
		$foreach (var x in list) {
			result++;
		}
	}
}";

            string result = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2, 3 };
		result += list.Count ();
	}
}";

            Assert.AreEqual(result, RunContextAction(new AutoLinqSumAction(), source));
        }

        [Test, Ignore("Not implemented!")]
        public void TestCompleteConditional()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2, 3 };
		$foreach (var x in list) {
			if (x > 0)
				result += x * 2;
			else
				result += x;
		}
	}
}";

            string result = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2, 3 };
		result += list.Sum (x => x > 0 ? x * 2 : x);
	}
}";

            Assert.AreEqual(result, RunContextAction(new AutoLinqSumAction(), source));
        }

        [Test]
        public void TestDisabledForSideEffects()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		string result = string.Empty;
		var list = new string[] { ""a"", ""b"" };
		$foreach (var x in list) {
			TestMethod();
			result += x;
		}
	}
}";
            TestWrongContext<AutoLinqSumAction>(source);
        }

        [Test]
        public void TestDisabledForInnerAssignments()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2 };
		int p = 0;
		$foreach (var x in list) {
			result += (p = x);
		}
	}
}";
            TestWrongContext<AutoLinqSumAction>(source);
        }

        [Test]
        public void TestDisabledForInnerIncrements()
        {
            string source = @"
using System.Linq;

class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2 };
		int p = 0;
		$foreach (var x in list) {
			result += (p++);
		}
	}
}";
            TestWrongContext<AutoLinqSumAction>(source);
        }

        [Test]
        public void TestDisabledForNoLinq()
        {
            string source = @"
class TestClass
{
	void TestMethod() {
		int result = 0;
		var list = new int[] { 1, 2 };
		$foreach (var x in list) {
			result += x;
		}
	}
}";
            TestWrongContext<AutoLinqSumAction>(source);
        }
    }
}

