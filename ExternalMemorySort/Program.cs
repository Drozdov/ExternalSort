using System;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace ExternalMemorySort
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Test1();
		}

		static void Test1()
		{
			int j = 0;
			foreach (var count in new int[] { (int)5e7, (int)1e8, (int)2e8, (int)4e8 })
			{
				System.IO.StreamWriter file = new System.IO.StreamWriter("result.txt", true);
				var before = DateTime.UtcNow;
				var rand = new Random();
				//var list = new List<int>(count);
				var list = new ExternalMemoryList<int>("directory" + j++, count / 15);
				for (int i = 0; i < count; i++)
				{
					list.Add(rand.Next(10000));
				}
				var then = DateTime.UtcNow;
				list.Sort();
				var now = DateTime.UtcNow;
				Console.WriteLine("===" + list.ToString() + " on " + count + " elements:");
				Console.WriteLine("All:" + (now - before));
				Console.WriteLine("Sort: " + (now - then));
				Console.WriteLine("===================================");
				file.WriteLine("===" + list.ToString() + " on " + count + " elements:");
				file.WriteLine("All:" + (now - before));
				file.WriteLine("Sort: " + (now - then));
				file.WriteLine("===================================");
				file.Close();
			}
		}

		static void Test2()
		{
			int j = 0;
			for (int k = 1; k < 15; k++)
			{
				int count = (int) 5e7;
				System.IO.StreamWriter file = new System.IO.StreamWriter("result.txt", true);
				var before = DateTime.UtcNow;
				var rand = new Random();
				var list = new ExternalMemoryList<int>("directory" + j++, count / k);
				for (int i = 0; i < count; i++)
				{
					list.Add(rand.Next(10000));
				}
				var then = DateTime.UtcNow;
				list.Sort();
				var now = DateTime.UtcNow;
				Console.WriteLine("===" + list.ToString() + " on k = " + k);
				Console.WriteLine("All:" + (now - before));
				Console.WriteLine("Sort: " + (now - then));
				Console.WriteLine("===================================");
				file.WriteLine("===" + list.ToString() + " on k = " + k);
				file.WriteLine("All:" + (now - before));
				file.WriteLine("Sort: " + (now - then));
				file.WriteLine("===================================");
				file.Close();
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
