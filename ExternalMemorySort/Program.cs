using System;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace ExternalMemorySort
{
	class MainClass
	{
		enum Algo { List, External, KMerge }

		public static void Main (string[] args)
		{
			var j = 0;
			var k = 5;
			var b = 65536;
			var algo = Algo.External;
			var count = (int) 4e8;
			while (j < args.Length)
			{
				if (args[j].Equals("-count"))
					count = Convert.ToInt32(args[++j]);
				else if (args[j].Equals("-k"))
					k = Convert.ToInt32(args[++j]);
				else if (args[j].Equals("-b"))
					b = Convert.ToInt32(args[++j]);
				else if (args[j].Equals("-internal"))
					algo = Algo.List;
				else if (args[j].Equals("-kmerge"))
					algo = Algo.KMerge;
				j++;
			}
			Test(count, k, algo, b);
		}

		private static void Test(int count, int k, Algo algo, int b = 65536)
		{
			Console.WriteLine("Test " + algo + " on " + count + " elements, k = " + k + " b = " + b);
			// always produce the same pseudo-random numbers
			m_z = 6531;
			m_w = 1365801;
			System.IO.StreamWriter file = new System.IO.StreamWriter("result.txt", true);
			var before = DateTime.UtcNow;
			
			IList<uint> list;

			switch (algo)
			{
				case Algo.List:
					list = new List<uint>(count);
					break;
				case Algo.External:
					list = new ExternalMemoryList<uint>("directory", count/k);
					break;
				default:
					list = new KMerge<uint>("directory", k, b);
					break;
			}
			for (int i = 0; i < count; i++)
			{
				list.Add(get_random());
			}
			var then = DateTime.UtcNow;
			if (list is List<uint>)
				(list as List<uint>).Sort();
			else
				(list as ExternalMemoryList<uint>).Sort();

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


		static uint m_z, m_w;
		static uint get_random()
		{
			m_z = 36969 * (m_z & 65535) + (m_z >> 16);
			m_w = 18000 * (m_w & 65535) + (m_w >> 16);
			return (m_z << 16) + m_w;  /* 32-bit result */
		}

	}
}
