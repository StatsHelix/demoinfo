using DemoInfo.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Emit;
using ProtoBuf;

namespace DemoInfo.DP
{
	public static class DemoPacketParser
    {
		private static readonly IEnumerable<IMessageParser> Parsers = (
			from type in Assembly.GetExecutingAssembly().GetTypes()
			where type.GetInterfaces().Contains(typeof(IMessageParser))
			let parser = (IMessageParser)type.GetConstructor(new Type[0]).Invoke(new object[0])
			orderby -parser.Priority
			select parser).ToArray();

		private static readonly Func<Stream, int, IExtensible> ReadProtobufPacket;

		static DemoPacketParser()
		{
			var dynmeth = new DynamicMethod("DemoPacketParser__GetPacketType",
				typeof(IExtensible), new Type[] { typeof(Stream), typeof(int) });
			var ilgen = dynmeth.GetILGenerator();

			var deserializeMethod = typeof(Serializer).GetMethod("DeserializeWithLengthPrefix",
				                        new Type[] { typeof(Stream), typeof(PrefixStyle) });

			var elementList = new List<Type>();
			foreach (var ele in
				Enum.GetValues(typeof(SVC_Messages)).Cast<object>().Select(x => new { Code = (int)x, Name = x.ToString(), Prefix = "CSVCMsg_" })
				.Concat(Enum.GetValues(typeof(NET_Messages)).Cast<object>().Select(x => new { Code = (int)x, Name = x.ToString(), Prefix = "CNETMsg_" }))
				.Select(x => new { x.Code, Type = Assembly.GetExecutingAssembly().GetType("DemoInfo.Messages." + x.Prefix + x.Name.Substring(4)) })
				.OrderBy(x => x.Code)) {
				while (elementList.Count < ele.Code)
					elementList.Add(null);
				elementList.Add(ele.Type);
			}

			var defaultCase = ilgen.DefineLabel();
			var jumpTable = elementList.Select(t => (t != null) ? ilgen.DefineLabel() : defaultCase).ToArray();
			ilgen.Emit(OpCodes.Ldarg_1);
			ilgen.Emit(OpCodes.Switch, jumpTable);
			ilgen.Emit(OpCodes.Br, defaultCase);

			//var endOfMethod = ilgen.DefineLabel();
			for (int i = 0; i < elementList.Count; i++) {
				if (elementList[i] != null) {
					ilgen.MarkLabel(jumpTable[i]);
					ilgen.Emit(OpCodes.Ldarg_0);
					ilgen.Emit(OpCodes.Ldc_I4, (int)PrefixStyle.Base128);

					ilgen.Emit(OpCodes.Tailcall);
					ilgen.Emit(OpCodes.Call, deserializeMethod.MakeGenericMethod(elementList[i]));
					//ilgen.Emit(OpCodes.Castclass, typeof(IExtensible));
					//ilgen.Emit(OpCodes.Br, endOfMethod);
					ilgen.Emit(OpCodes.Ret);
				}
			}
			ilgen.MarkLabel(defaultCase);
			ilgen.Emit(OpCodes.Ldnull);
			//ilgen.Emit(OpCodes.Br_S, endOfMethod);
			//ilgen.MarkLabel(endOfMethod);
			ilgen.Emit(OpCodes.Ret);

			ReadProtobufPacket = (Func<Stream, int, IExtensible>)dynmeth
				.CreateDelegate(typeof(Func<Stream, int, IExtensible>));
		}

		public static void ParsePacket(Stream stream, DemoParser demo)
        {
			var reader = new BinaryReader(stream);

			while (stream.Position < stream.Length)
            {
                int cmd = reader.ReadVarInt32();

				var result = ReadProtobufPacket(reader.BaseStream, cmd);

                if (result == null) {
                    reader.ReadBytes(reader.ReadVarInt32());
                    continue;
                }

                foreach (var parser in Parsers)
					if (parser.TryApplyMessage(result, demo) && (parser.Priority > 0))
						break;
            }
        }
    }
}
