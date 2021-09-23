using System;
using System.IO;

namespace DemoInfo
{
	public struct ServerInfo
	{
		public Int32 Protocol;
		public Int32 ServerCount;
		public bool IsDedicated;
		public bool IsOfficialValveServer;
		public bool IsHltv;
		public bool IsReplay;
		public bool IsRedirectingToProxyRelay;
		public Int32 COs;
		public UInt32 MapCrc;
		public UInt32 ClientCrc;
		public UInt32 StringTableCrc;
		public Int32 MaxClients;
		public Int32 MaxClasses;
		public Int32 PlayerSlot;
		public float TickInterval;
		public string GameDir;
		public string MapName;
		public string MapGroupName;
		public string SkyName;
		public string HostName;
		public UInt32 PublicIp;
		public UInt64 UgcMapId;


		public void Parse(IBitStream bitstream, DemoParser parser)
		{
			while (!bitstream.ChunkFinished)
			{
				var desc = bitstream.ReadProtobufVarInt();
				var wireType = desc & 7;
				var fieldnum = desc >> 3;

				if (wireType == 5)
				{
					if (fieldnum == 14)
					{
						parser.TickInterval = bitstream.ReadFloat();
					}
					else
					{
						var val = bitstream.ReadInt(32);
						switch (fieldnum)
						{
							case 8:
								MapCrc = val;
								break;
							case 9:
								ClientCrc = val;
								break;
							case 10:
								StringTableCrc = val;
								break;
						}
					}
				}
				else if (wireType == 2)
				{
					var val = bitstream.ReadProtobufString();

					switch (fieldnum)
					{
						case 15:
							GameDir = val;
							break;
						case 16:
							MapName = val;
							break;
						case 17:
							MapGroupName = val;
							break;
						case 18:
							SkyName = val;
							break;
						case 19:
							HostName = val;
							break;
					}
				}
				else if (wireType == 0)
				{
					var val = bitstream.ReadProtobufVarInt();
					var boolval = (val == 0) ? false : true;

					switch (fieldnum)
					{
						case 1:
							Protocol = val;
							break;
						case 2:
							ServerCount = val;
							break;
						case 3:
							IsDedicated = boolval;
							break;
						case 4:
							IsOfficialValveServer = boolval;
							break;
						case 5:
							IsHltv = boolval;
							break;
						case 6:
							IsReplay = boolval;
							break;
						case 7:
							COs = val;
							break;
						case 11:
							MaxClients = val;
							break;
						case 12:
							MaxClasses = val;
							break;
						case 13:
							PlayerSlot = val;
							break;
						case 20:
							PublicIp = (uint)val;
							break;
						case 21:
							IsRedirectingToProxyRelay = boolval;
							break;
						case 22:
							UgcMapId = (uint)val;
							break;
					}
				}
			}
		}
	}
}