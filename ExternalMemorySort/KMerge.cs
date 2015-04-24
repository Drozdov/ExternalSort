using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ExternalMemorySort
{
	public class KMerge<T> : ExternalMemoryList<T> where T : IComparable<T>
	{
		private int k, buffer;
		private Queue<int> queue;
		private bool merging = false;
		private BufferedStream currentStream;
		int index = 0;
		public KMerge(string pathToDirectory, int k, int buffer) : base(pathToDirectory, buffer, null, true)
		{
			this.k = k;
			this.buffer = buffer;
			preamble = SORTED;
		}

		public override void Sort()
		{
			SaveCurrent();
			if (!SortOnSave)
			{
				for (int i = 0; i < bucketsCount; i++)
					Sort(i);
			}
			queue = new Queue<int>();
			for (int i = 0; i < bucketsCount; i++)
			{
				queue.Enqueue(i);
			}
			var curIndx = bucketsCount;
			merging = true;
			while (queue.Count > 0)
			{
				var size = Math.Min(k, queue.Count);
				IEnumerator<T>[] enumerators = new IEnumerator<T>[size];
				for (int i = 0; i < size; i++)
				{
					enumerators[i] = new SmallPartEnumerator(PathToDirectory + SORTED + queue.Dequeue(), BytesGetter, buffer);
				}
				Clear();
				if (queue.Count == 0)
				{
					merging = false;
					preamble = EXTERNAL;
					SortOnSave = false;
					Merge(enumerators);
					SortOnSave = true;
				}
				else
				{
					currentStream = new BufferedStream(new FileStream(PathToDirectory + SORTED + curIndx,
										FileMode.Create,
										FileAccess.Write, FileShare.None));
					index = 0;
					Merge(enumerators);
					queue.Enqueue(curIndx++);
					Buffer.BlockCopy(current, 0, bytes, 0, index * bytesRequired);
					currentStream.Write(bytes, 0, index * bytesRequired);
					currentStream.Close();
				}
			}
			merging = false;
		}

		public override void Add(T item)
		{
			if (!merging)
			{
				base.Add(item);
				return;
			}
			current[index++] = item;
			if (index == current.Length)
			{
				Buffer.BlockCopy(current, 0, bytes, 0, current.Length * bytesRequired);
				currentStream.Write(bytes, 0, current.Length * bytesRequired);
				index = 0;
			}
		}
	}
}
