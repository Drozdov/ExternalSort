//#define spec
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System.Linq;

namespace ExternalMemorySort
{
	public class ExternalMemoryList<T> : IList<T> where T : IComparable<T>
	{
		public readonly string PathToDirectory;
		public readonly int MaxElementsInMemoryCount;
		public readonly IBytesGetter<T> BytesGetter;
		public ExternalMemoryList(string pathToDirectory, int maxElementsInMemoryCount, IBytesGetter<T> bytesGetter)
		#if spec
		modifies this
		#endif
		{
			pathToDirectory += "/";
			Directory.CreateDirectory(pathToDirectory);
			this.PathToDirectory = pathToDirectory;
			this.MaxElementsInMemoryCount = maxElementsInMemoryCount;
			this.BytesGetter = bytesGetter;
			current = new List<T>(maxElementsInMemoryCount);
		}

		#region Private
		private int count = 0;

		const string EXTERNAL = "external";
		const string SORTED = "sorted";

		private int currentBucket = -1;
		private List<T> current;
		private string preamble = EXTERNAL;

		private void SaveCurrent()
		{
			int newCount = current.Count;
			var bytes = new byte[newCount * BytesGetter.BytesRequired];
			int j = 0;
			foreach (var element in current)
			{
				Array.Copy(BytesGetter.GetBytes(element), 0, bytes, j, BytesGetter.BytesRequired);
				j += BytesGetter.BytesRequired;
			}
			using (var stream = new BufferedStream(new FileStream(PathToDirectory + preamble + CurrentBucket, 
				                    FileMode.Create, 
				                    FileAccess.Write, FileShare.None)))
			{
				stream.Write(bytes, 0, newCount * BytesGetter.BytesRequired);
			}
		}

		private int bucketsCount;

		private int LastBucket { get { return ElementsInLastBucket == MaxElementsInMemoryCount ? bucketsCount : bucketsCount - 1; } }

		private int ElementsInLastBucket
		{
			get
			{
				int res = count % MaxElementsInMemoryCount;
				if (res != 0)
					return res;
				if (MaxElementsInMemoryCount * bucketsCount == count)
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
				if (currentBucket == bucketsCount - 1 && currentBucket >= 0)
					SaveCurrent();
				currentBucket = value;
				//current = new List<T>();
				current.Clear();
				if (currentBucket >= bucketsCount)
				{
					bucketsCount = currentBucket + 1;
					return;
				}
				using (var enumerable = new SmallPartEnumerator(PathToDirectory + preamble + currentBucket, BytesGetter))
				{
					int size = value == LastBucket ? ElementsInLastBucket : MaxElementsInMemoryCount;
					
					foreach (var element in enumerable)
						current.Add((T)element);
						
				}
			}
		}
		#endregion

		#region IList implementation
		public T this [int index]
		{
			#if spec
			reads this
			#endif
			get
			{
				if (index < 0 || index >= count)
					throw new IndexOutOfRangeException();
				CurrentBucket = index / MaxElementsInMemoryCount;
				return current[index % MaxElementsInMemoryCount];
			}
			set
			{
				throw new InvalidOperationException("Not supported");
			}
		}

		public int IndexOf(T item)
		{
			throw new InvalidOperationException("Not supported");
		}

		public void Insert(int index, T item)
		{
			throw new InvalidOperationException("Not supported");
		}

		public void RemoveAt(int index)
		{
			throw new InvalidOperationException("Not supported");
		}
		#endregion

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
			current.Clear();
			currentBucket = -1;
			bucketsCount = 0;
		}

		public bool Contains(T item)
		{
			foreach (var element in this)
			{
				if (element.Equals(item))
					return true;
			}
			return false;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			switch (bucketsCount)
			{
				case 0:
					break;
				case 1:
					current.CopyTo(array, arrayIndex);
					break;
				default:
					throw new ArgumentException("Too many elements to copy");
			}
		}

		public bool Remove(T item)
		{
			throw new InvalidOperationException("Not supported");
		}

		public int Count { get { return count; } }

		public bool IsReadOnly { get { return false; } }
		#endregion

		#region IEnumerable implementation
		public IEnumerator<T> GetEnumerator()
		{
			return new Enumerator(this);
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		class Enumerator : IEnumerator<T>
		{
			private ExternalMemoryList<T> collection;
			int index = 0;

			internal Enumerator(ExternalMemoryList<T> colletion)
			{
				this.collection = colletion;
			}

			public bool MoveNext()
			{
				return ++index < collection.Count;
			}

			public void Reset()
			{
				index = 0;
			}

			void IDisposable.Dispose() { }

			public T Current
			{
				get { return collection[index]; }
			}

			object IEnumerator.Current
			{
				get { return Current; }
			}
		}
		#endregion

		#region Sorting procedures

		public void Sort(int bucketId)
		{
			CurrentBucket = bucketId;
			current.Sort();
			preamble = SORTED;
			SaveCurrent();
			preamble = EXTERNAL;
		}

		public void Sort()
		{
			for (int i = 0; i < bucketsCount; i++)
				Sort(i);
			var enumerators = new IEnumerator<T>[bucketsCount];
			for (int i = 0; i < bucketsCount; i++)
			{
				enumerators[i] = new SmallPartEnumerator(PathToDirectory + SORTED + i, BytesGetter);
			}
#if false
			T[] curMinimums = new T[bucketsCount];
			bool[] finished = new bool[bucketsCount];

			for (int i = 0; i < curMinimums.Length; i++)
			{
				enumerators[i].MoveNext();
				curMinimums[i] = enumerators[i].Current;
			}
			Clear();
			
			int argMin;
			do
			{
				argMin = -1;
				for (int i = 0; i < curMinimums.Length; i++)
				{
					if (!finished[i] && (argMin == -1 || curMinimums[i].CompareTo(curMinimums[argMin]) < 0))
					{
						argMin = i;
					}
				}
				if (argMin >= 0)
				{
					Add(curMinimums[argMin]);
					if (enumerators[argMin].MoveNext())
						curMinimums[argMin] = enumerators[argMin].Current;
					else
						finished[argMin] = true;
				}
			} while (argMin >= 0);
#else
			List<int> curOrder = new List<int>(bucketsCount);
			Clear();
			for (int i = 0; i < enumerators.Length; i++)
			{
				enumerators[i].MoveNext();
				curOrder.Add(i);
			}
			curOrder.Sort(new Comparer((id) => enumerators[id].Current));
			while (curOrder.Count > 0)
			{
				var id = curOrder[0];
				var enumerator = enumerators[id];
				var val = enumerator.Current;
				Add(val);
				if (enumerator.MoveNext())
				{
					val = enumerator.Current;
					int j = 0;
					while (j < curOrder.Count - 1 && val.CompareTo(enumerators[curOrder[j + 1]].Current) > 0)
					{
						curOrder[j] = curOrder[j + 1];
						curOrder[++j] = id;
					}
				}
				else
				{
					curOrder.Remove(id);
				}
			}
#endif
		}

		class Comparer : IComparer<int>
		{
			Func<int, T> foo;
			internal Comparer(Func<int, T> foo)
			{
				this.foo = foo;
			}

			public int Compare(int x, int y)
			{
				return foo(x).CompareTo(foo(y));
			}
		}

		class SmallPartEnumerator : IEnumerator<T>, IEnumerable<T>
		{
			Stream stream;
			IBytesGetter<T> bytesGetter;
			byte[] buffer;
			int elementsInBuffer = 100; // elements read at once
			int left = 0;
			int pos = 0;
			internal SmallPartEnumerator(string path, IBytesGetter<T> bytesGetter)
			{
				this.stream = new BufferedStream(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None));
				this.bytesGetter = bytesGetter;
				this.buffer = new byte[bytesGetter.BytesRequired * elementsInBuffer];
			}

			public T Current { get; private set; }

			public void Dispose()
			{
				stream.Dispose();
			}

			object IEnumerator.Current
			{
				get { return Current; }
			}

			public bool MoveNext()
			{
				if (left == 0)
				{
					left = stream.Read(buffer, 0, buffer.Length) / bytesGetter.BytesRequired;
					pos = 0;
				}
				if (left == 0)
					return false;
				Current = bytesGetter.GetValue(buffer, pos);
				pos += bytesGetter.BytesRequired;
				left--;
				return true;
			}

			public void Reset()
			{
				stream.Position = 0;
			}

			IEnumerator<T> IEnumerable<T>.GetEnumerator()
			{
				return this;
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this;
			}
		}
		#endregion

	}

	public interface IBytesGetter<T>
	{
		int BytesRequired { get; }

		byte[] GetBytes(T value);

		T GetValue(byte[] bytes, int startIndex);
	}
}

