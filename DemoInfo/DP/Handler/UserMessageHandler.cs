using System;
using System.Collections.Generic;
using DemoInfo.Messages;
using System.IO;
using System.Reflection;
using ProtoBuf;

namespace DemoInfo.DP.Handler
{
	/// <summary>
	/// Parser handler for UserMessage
	/// </summary>
	public class UserMessageHandler : IMessageParser
	{
		/// <summary>
		/// Try to parse a UserMessage packet 
		/// </summary>
		/// <param name="message">The message to analyze</param>
		/// <param name="parser">Demo parser</param>
		/// <returns></returns>
		public bool TryApplyMessage(IExtensible message, DemoParser parser)
		{
			CSVCMsg_UserMessage userMessage = message as CSVCMsg_UserMessage;

			if (userMessage == null || !Enum.IsDefined(typeof(ECstrike15UserMessages), userMessage.msg_type))
				return false;

			ECstrike15UserMessages msg = (ECstrike15UserMessages)userMessage.msg_type;
			Type toParse = Assembly.GetExecutingAssembly().GetType("DemoInfo.Messages.CCSUsrMsg_" + msg.ToString().Substring(6));

			using (var memstream = new MemoryStream(userMessage.msg_data))
			{
				IExtensible data = memstream.ReadProtobufMessage(toParse);
				if (data != null)
				{
					switch (data.GetType().Name)
					{
						case "CCSUsrMsg_SayText":
						{
								SayTextEventArgs e = new SayTextEventArgs();
								CCSUsrMsg_SayText sayMsg = (CCSUsrMsg_SayText)data;
								e.Text = sayMsg.text;
								e.TextAllChat = sayMsg.textallchat;
								e.Chat = sayMsg.chat;
								parser.RaiseSayText(e);
								break;
						}
						case "CCSUsrMsg_SayText2":
						{
								SayText2EventArgs e = new SayText2EventArgs();
								CCSUsrMsg_SayText2 sayMsg = (CCSUsrMsg_SayText2)data;
								e.TextAllChat = sayMsg.textallchat;
								e.Chat = sayMsg.chat;

								// get the player who wrote the message
								foreach (KeyValuePair<int, Player> keyValuePair in parser.Players)
								{
									if (keyValuePair.Value.Name == sayMsg.@params[0])
									{
										e.Sender = parser.Players[keyValuePair.Key];
										break;
									}
								}

								// @params is a 4 length array but only 2 are used [0] = nickname [1] = message text
								e.Text = sayMsg.@params[0] + " : " + sayMsg.@params[1];
								parser.RaiseSayText2(e);
								break;
							}
						default:
							return false;
					}
				}
			}

			return true;
		}

		public int Priority { get { return 0; } }
	}
}
