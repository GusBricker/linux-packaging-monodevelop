/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using NGit.Util;
using Sharpen;

namespace NGit.Util
{
	[NUnit.Framework.TestFixture]
	public class RawParseUtils_LineMapTest
	{
		[NUnit.Framework.Test]
		public virtual void TestEmpty()
		{
			IntList map = RawParseUtils.LineMap(new byte[] {  }, 0, 0);
			NUnit.Framework.Assert.IsNotNull(map);
			NUnit.Framework.Assert.AreEqual(2, map.Size());
			NUnit.Framework.Assert.AreEqual(int.MinValue, map.Get(0));
			NUnit.Framework.Assert.AreEqual(0, map.Get(1));
		}

		[NUnit.Framework.Test]
		public virtual void TestOneBlankLine()
		{
			IntList map = RawParseUtils.LineMap(new byte[] { (byte)('\n') }, 0, 1);
			NUnit.Framework.Assert.AreEqual(3, map.Size());
			NUnit.Framework.Assert.AreEqual(int.MinValue, map.Get(0));
			NUnit.Framework.Assert.AreEqual(0, map.Get(1));
			NUnit.Framework.Assert.AreEqual(1, map.Get(2));
		}

		/// <exception cref="Sharpen.UnsupportedEncodingException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestTwoLineFooBar()
		{
			byte[] buf = Sharpen.Runtime.GetBytesForString("foo\nbar\n", "ISO-8859-1");
			IntList map = RawParseUtils.LineMap(buf, 0, buf.Length);
			NUnit.Framework.Assert.AreEqual(4, map.Size());
			NUnit.Framework.Assert.AreEqual(int.MinValue, map.Get(0));
			NUnit.Framework.Assert.AreEqual(0, map.Get(1));
			NUnit.Framework.Assert.AreEqual(4, map.Get(2));
			NUnit.Framework.Assert.AreEqual(buf.Length, map.Get(3));
		}

		/// <exception cref="Sharpen.UnsupportedEncodingException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestTwoLineNoLF()
		{
			byte[] buf = Sharpen.Runtime.GetBytesForString("foo\nbar", "ISO-8859-1");
			IntList map = RawParseUtils.LineMap(buf, 0, buf.Length);
			NUnit.Framework.Assert.AreEqual(4, map.Size());
			NUnit.Framework.Assert.AreEqual(int.MinValue, map.Get(0));
			NUnit.Framework.Assert.AreEqual(0, map.Get(1));
			NUnit.Framework.Assert.AreEqual(4, map.Get(2));
			NUnit.Framework.Assert.AreEqual(buf.Length, map.Get(3));
		}

		/// <exception cref="Sharpen.UnsupportedEncodingException"></exception>
		[NUnit.Framework.Test]
		public virtual void TestFourLineBlanks()
		{
			byte[] buf = Sharpen.Runtime.GetBytesForString("foo\n\n\nbar\n", "ISO-8859-1");
			IntList map = RawParseUtils.LineMap(buf, 0, buf.Length);
			NUnit.Framework.Assert.AreEqual(6, map.Size());
			NUnit.Framework.Assert.AreEqual(int.MinValue, map.Get(0));
			NUnit.Framework.Assert.AreEqual(0, map.Get(1));
			NUnit.Framework.Assert.AreEqual(4, map.Get(2));
			NUnit.Framework.Assert.AreEqual(5, map.Get(3));
			NUnit.Framework.Assert.AreEqual(6, map.Get(4));
			NUnit.Framework.Assert.AreEqual(buf.Length, map.Get(5));
		}
	}
}
