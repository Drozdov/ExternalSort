using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ExternalMemorySort
{
	public class ExternalMemoryList<T> : IList<T> where T : IComparable<T>
	{
		public readonly string PathToDirectory;
		public readonly int MaxElementsInMemoryCount;
		public readonly IBytesGetter<T> BytesGetter;
		public bool SortOnSave;
		public ExternalMemoryList(string pathToDirectory, int maxElementsInMemoryCount, IBytesGetter<T> bytesGetter = null,
			bool sortOnSave = false)
		{
			pathToDirectory += "/";
			Directory.CreateDirectory(pathToDirectory);
			this.PathToDirectory = pathToDirectory;
			this.MaxElementsInMemoryCount = maxElementsInMemoryCount;
			this.BytesGetter = bytesGetter;
			current = new T[maxElementsInMemoryCount];
			bytesRequired = BytesGetter != null ? BytesGetter.BytesRequired : Marshal.SizeOf(typeof(T));
			bytes = new byte[maxElementsInMemoryCount * bytesRequired];
			SortOnSave = sortOnSave;
		}

		#region Private & Protected
		protected int count = 0;

		protected const string EXTERNAL = "external";
		protected const string SORTED = "sorted";

		protected int currentBucket = -1;
		protected T[] current;
		protected string preamble = EXTERNAL;
		protected int currentCount;
		protected int bucketsCount;
		protected byte[] bytes;
		protected int bytesRequired;

		protected void SaveCurrent()
		{
			if (SortOnSave)
				Array.Sort(current, 0, currentCount);
			int newCount = currentCount;
			if (BytesGetter != null)
			{
				for (int i = 0, j = 0; i < currentCount; ++i, j += BytesGetter.BytesRequired)
				{
					Array.Copy(BytesGetter.GetBytes(current[i]), 0, bytes, j, BytesGetter.BytesRequired);
				}
			}
			else
			{
				Buffer.BlockCopy(current, 0, bytes, 0, newCount * bytesRequired);
			}
			using (var stream = new BufferedStream(new FileStream(PathToDirectory + preamble + CurrentBucket, 
				                    FileMode.Create, 
				                    FileAccess.Write, FileShare.None)))
			{
				stream.Write(bytes, 0, newCount * bytesRequired);
			}
		}

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
				{
					SaveCurrent();
				}
				currentBucket = value;
				currentCount = 0;
				if (currentBucket >= bucketsCount)
				{
					bucketsCount = currentBucket + 1;
					return;
				}
				var path = PathToDirectory + preamble + currentBucket;
				if (BytesGetter != null)
				{
					using (var enumerable = new SmallPartEnumerator(path, BytesGetter))
					{
						int size = value == LastBucket ? ElementsInLastBucket : MaxElementsInMemoryCount;

						foreach (var element in enumerable)
						{
							AddItem(element);
						}
					}
				}
				else
				{
					using (var stream = new BufferedStream(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None)))
					{
						int copied = stream.Read(bytes, 0, bytesRequired * MaxElementsInMemoryCount);
						Buffer.BlockCopy(bytes, 0, current, 0, copied);
						currentCount = copied / bytesRequired;
					}
					
				}
			}
		}

		private void AddItem(T item)
		{
			if (currentCount >= MaxElementsInMemoryCount)
				throw new IndexOutOfRangeException("Adding to collection of not proper size");
			current[currentCount++] = item;
		}
		#endregion

		#region IList implementation
		public T this [int index]
		{
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
		public virtual void Add(T item)
		{
			CurrentBucket = LastBucket;
			AddItem(item);
			count++;
		}

		public void Clear()
		{
			count = 0;
			currentCount = 0;
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
			Array.Sort(current);
			preamble = SORTED;
			SaveCurrent();
			preamble = EXTERNAL;
		}

		public virtual void Sort()
		{
			SaveCurrent();
			if (!SortOnSave)
			{
				for (int i = 0; i < bucketsCount; i++)
					Sort(i);
			}
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
			Clear();
			Merge(enumerators);
#endif
		}

		protected void Merge(IEnumerator<T>[] enumerators)
		{
			List<int> curOrder = new List<int>(bucketsCount);
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
			foreach (var enumerator in enumerators)
			{
				enumerator.Dispose();
			}
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

		protected class SmallPartEnumerator : IEnumerator<T>, IEnumerable<T>
		{
			Stream stream;
			IBytesGetter<T> bytesGetter;
			byte[] buffer;
			private T[] queue;
			int elementsInBuffer; // elements read at once
			int left = 0;
			int pos = 0;
			private int bytesRequired;

			internal SmallPartEnumerator(string path, IBytesGetter<T> bytesGetter, int buffer = 256)
			{
				this.elementsInBuffer = buffer;
				this.stream = new BufferedStream(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None));
				this.bytesGetter = bytesGetter;
				bytesRequired = bytesGetter != null ? bytesGetter.BytesRequired : Marshal.SizeOf(typeof(T));
				this.buffer = new byte[bytesRequired * elementsInBuffer];
				if (bytesGetter == null)
					queue = new T[elementsInBuffer];
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
				return bytesGetter != null ? MoveNextWithGetter() : MoveNextWithoutGetter();
			}

			bool MoveNextWithGetter()
			{
				if (left == 0)
				{
					left = stream.Read(buffer, 0, buffer.Length) / bytesRequired;
					pos = 0;
				}
				if (left == 0)
					return false;
				Current = bytesGetter.GetValue(buffer, pos);
				pos += bytesRequired;
				left--;
				return true;
			}

			bool MoveNextWithoutGetter()
			{
				if (left == 0)
				{
					int read = stream.Read(buffer, 0, buffer.Length);
					pos = 0;
					Buffer.BlockCopy(buffer, 0, queue, 0, read);
					left = read / bytesRequired;
				}
				if (left == 0)
					return false;
				Current = queue[pos++];
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

