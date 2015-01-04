using System;
using google.protobuf;
using System.IO;

namespace Brotogen
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			var input = ProtoBuf.Serializer.Deserialize<FileDescriptorSet> (File.OpenRead (args[0]));
			var g = new CSharpGenerator (input);
		}
	}
}
