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
			foreach (var count in new int[] {  (int)2e8, (int)4e8 })
			{
				// always produce the same pseudo-random numbers
				m_z = 6531;
				m_w = 1365801;
				System.IO.StreamWriter file = new System.IO.StreamWriter("result.txt", true);
				var before = DateTime.UtcNow;
				var list = new List<uint>(count);
				//var list = new ExternalMemoryList<uint>("directory" + j++, count / 5);
				for (int i = 0; i < count; i++)
				{
					list.Add(get_random());
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


		static uint m_z, m_w;
		static uint get_random()
		{
			m_z = 36969 * (m_z & 65535) + (m_z >> 16);
			m_w = 18000 * (m_w & 65535) + (m_w >> 16);
			return (m_z << 16) + m_w;  /* 32-bit result */
		}

		static void Test2()
		{
			int j = 0;
			//foreach (var k in new int[] {5, 10, 20, 50, 100, 250, 500, 1000, 2000})
			for (int k = 14; k <= 14; k++)
			{
				// always produce the same pseudo-random numbers
				m_z = 6531;
				m_w = 1365801;

				int count = (int) 5e7;
				System.IO.StreamWriter file = new System.IO.StreamWriter("result.txt", true);
				var before = DateTime.UtcNow;
				
				var list = new ExternalMemoryList<uint>("directory" + j++, count / k);
				for (int i = 0; i < count; i++)
				{
					list.Add(get_random());
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
