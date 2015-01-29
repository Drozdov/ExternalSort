using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ExternalMemorySort
{
	public class ExternalMemoryCollection<T> : ICollection<T>
	{
		public readonly string PathToDirectory;
		public readonly int MaxElementsInMemoryCount;
		public ExternalMemoryCollection(string pathToDirectory, int maxElementsInMemoryCount)
		{
			if (!pathToDirectory.EndsWith("/"))
				pathToDirectory += "/";
			Directory.CreateDirectory(pathToDirectory);
			this.PathToDirectory = pathToDirectory;
			this.MaxElementsInMemoryCount = maxElementsInMemoryCount;
			this.Formatter = new BinaryFormatter();
		}

		private int count = 0;
		public IFormatter Formatter { get; set; }
		private int currentBucket = -1;
		private IList<T> current = new List<T>();

		private void SaveCurrent()
		{
			if (current.Count < MaxElementsInMemoryCount)
				return;
			int newCount = current.Count;
			var stream = new FileStream(PathToDirectory + "external" + CurrentBucket, 
				FileMode.Create, 
				FileAccess.Write, FileShare.None);
			foreach (var element in current)
			{
				Formatter.Serialize(stream, element);
			}
		}

		private int BucketsCount { get; set; }

		private int LastBucket { get { return ElementsInLastBucket == MaxElementsInMemoryCount ? BucketsCount : BucketsCount - 1; } }

		private int ElementsInLastBucket
		{
			get
			{
				int res = count % MaxElementsInMemoryCount;
				if (res != 0)
					return res;
				if (MaxElementsInMemoryCount * BucketsCount == count)
					return MaxElementsInMemoryCount;
				else
					return 0;
			}
		}

		private int CurrentBucket
		{
			get { return currentBucket; } 
			set
			{
				if (currentBucket == value)
					return;
				if (currentBucket == BucketsCount - 1 && currentBucket >= 0)
					SaveCurrent();
				currentBucket = value;
				current = new List<T>();
				if (currentBucket >= BucketsCount)
				{
					BucketsCount = currentBucket + 1;
					return;
				}
				Stream stream = new FileStream(PathToDirectory + "external" + currentBucket, 
					                FileMode.Open, 
					                FileAccess.Read, 
					                FileShare.Read);
				int size = value == LastBucket ? ElementsInLastBucket : MaxElementsInMemoryCount;
				try
				{
					for (int j = 0; j < size; j++)
					{
						var element = Formatter.Deserialize(stream);
						if (element is T)
							current.Add((T)element);
					}
				}
				catch
				{
				}
			}
		}

		#region ICollection implementation
		public void Add(T item)
		{
			CurrentBucket = LastBucket;
			current.Add(item);
			count++;
		}

		public void Clear()
		{
			count = 0;
			current = new List<T>();
			currentBucket = 0;
		}

		public bool Contains(T item)
		{
			throw new NotImplementedException();
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		public bool Remove(T item)
		{
			throw new NotImplementedException();
		}

		public int Count { get { return count; } }

		public bool IsReadOnly
		{
			get
			{
				throw new NotImplementedException();
			}
		}
		#endregion
		#region IEnumerable implementation
		public IEnumerator<T> GetEnumerator()
		{
			throw new NotImplementedException();
		}
		#endregion
		#region IEnumerable implementation
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}
		#endregion

		//[Serializable]
		public class MyObject {
			public int n1 = 0;
			public int n2 = 0;
			public String str = null;
		}

		public void TestSerialize()
		{
			MyObject obj = new MyObject();
			obj.n1 = 1;
			obj.n2 = 24;
			obj.str = "Some String";
			IFormatter formatter = new BinaryFormatter();
			Stream stream = new FileStream("MyFile.bin", 
				FileMode.Append, 
				FileAccess.Write, FileShare.None);
			formatter.Serialize(stream, obj);
			stream.Close();
		}

		public void TestDeSerialize()
		{
			IFormatter formatter = new BinaryFormatter();
			Stream stream = new FileStream("MyFile.bin", 
				FileMode.Open, 
				FileAccess.Read, 
				FileShare.Read);
			for (int i = 0; i < 3; i++)
			{
				MyObject obj = (MyObject)formatter.Deserialize(stream);
				//stream.Close();

				// Here's the proof
				Console.WriteLine("n1: {0}", obj.n1);
				Console.WriteLine("n2: {0}", obj.n2);
				Console.WriteLine("str: {0}", obj.str);
			}
		}
	}
}

