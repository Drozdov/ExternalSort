using System;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System.Collections.Generic;

namespace ExternalMemorySort
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			var rand = new Random();
			int count = 50000000;
			//var list = new List<int>(count);
			var list =  new ExternalMemoryList<int>("directory", 50000000, new IntBytesGetter());
			for (int i = 0; i < count; i++)
			{
				list.Add(rand.Next(10000));
			}
			var then = DateTime.UtcNow;
			list.Sort();
			var now = DateTime.UtcNow;
			Console.WriteLine(now - then);
			Console.ReadKey();
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
