using System;
using System.Runtime.Serialization;
using System.IO;

namespace ExternalMemorySort
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			var collection = new ExternalMemoryCollection<int>("directory", 5);
			collection.Formatter = new IntFormatter();
			collection.Add(1);
			collection.Add(1);
			collection.Add(1);
			collection.Add(1);
			collection.Add(1);
			collection.Add(1);
			collection.Add(1);
			collection.Add(1);
			collection.Add(1);
			collection.Add(1);
			collection.Add(1);
			collection.Add(1);
			collection.Add(1);
			collection.Add(1);
			collection.Add(1);
			collection.Add(1);
			collection.Add(1);
			collection.Add(1);
		}

		public class IntFormatter : IFormatter
		{
			public SerializationBinder Binder {
				get;
				set;
			}

			public StreamingContext Context {
				get;
				set;
			}

			public ISurrogateSelector SurrogateSelector {
				get;
				set;
			}

			public object Deserialize (Stream serializationStream)
			{
				byte[] buf = new byte[4];
				serializationStream.Read(buf, 0, 4);
				return BitConverter.ToInt32(buf, 0);
			}

			public void Serialize (Stream serializationStream, object graph)
			{
				int val = (int)graph;
				var b = BitConverter.GetBytes(val);
				serializationStream.Write(b, 0, 4);
			}
		}
	}
}
