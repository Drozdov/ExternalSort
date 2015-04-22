using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ExternalMemorySort;

namespace ExternalMemoryUnitTests
{
    [TestClass]
    public class UnitTest
    {
		[TestMethod]
		public void TestMethod0()
		{
			var externalMemoryList = new ExternalMemoryList<int>("dir", 100, new IntBytesGetter());
			Assert.AreEqual(externalMemoryList.Count, 0);
			for (int i = 0; i < 200; i++)
			{
				Assert.AreEqual(externalMemoryList.Count, i);
				externalMemoryList.Add(i);
				Assert.AreEqual(externalMemoryList[i], i);
			}
			try
			{
				int v = externalMemoryList[201];
			}
			catch (IndexOutOfRangeException)
			{
				return;
			}
			Assert.Fail("No exception was thrown.");
		}

		[TestMethod]
		public void TestMethod1()
		{
			var externalMemoryList = new ExternalMemoryList<int>("dir2", 100, new IntBytesGetter());
			var cur = 37;
			const int step = 77;
			const int count = 1000;
			for (int i = 0; i < count; i++)
			{
				externalMemoryList.Add(cur);
				cur = (cur + step) % count;
			}
			externalMemoryList.Sort();
			for (int i = 0; i < count; i++)
				Assert.AreEqual(externalMemoryList[i], i);
		}

		[TestMethod]
		public void TestMethod2()
		{
			var externalMemoryList = new ExternalMemoryList<int>("dir3", 350, new IntBytesGetter());
			for (int i = 0; i < 10000; i++)
			{
				var rand = new Random();
				externalMemoryList.Add(rand.Next(10000) - 5000);
			}
			externalMemoryList.Sort();
			int current = -5001;
			foreach (var element in externalMemoryList)
			{
				Assert.IsTrue(element >= current);
				current = element;
			}
		}

		[TestMethod]
		public void TestMethod1NoGetter()
		{
			var externalMemoryList = new ExternalMemoryList<int>("dir4", 100);
			var cur = 37;
			const int step = 77;
			const int count = 1000;
			for (int i = 0; i < count; i++)
			{
				externalMemoryList.Add(cur);
				cur = (cur + step) % count;
			}
			externalMemoryList.Sort();
			for (int i = 0; i < count; i++)
				Assert.AreEqual(externalMemoryList[i], i);
		}

		[TestMethod]
		public void TestMethod2NoGetter()
		{
			var externalMemoryList = new ExternalMemoryList<uint>("dir5", 350);
			for (int i = 0; i < 10000; i++)
			{
				var rand = new Random();
				externalMemoryList.Add((uint)rand.Next(10000));
			}
			externalMemoryList.Sort();
			uint current = 0;
			foreach (var element in externalMemoryList)
			{
				Assert.IsTrue(element >= current);
				current = element;
			}
		}

        public class IntBytesGetter : IBytesGetter<int>
        {
            public int BytesRequired
            {
                get { return 4; }
            }

            public byte[] GetBytes(int value)
            {
                return BitConverter.GetBytes(value);
            }

            public int GetValue(byte[] bytes, int startIndex)
            {
                return BitConverter.ToInt32(bytes, startIndex);
            }
        }
    }
}
