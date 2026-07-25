using Godot;
using System;
using GodotSteam;
using Godot.Collections;

public partial class Network : Node
{
	public static Network Instance { get; private set; }
	public override void _Ready()
	{
		Instance = this;
	}
    public override void _Process(double delta)
	{
		//Receive array of packets
		var packets = Steam.ReceiveMessagesOnChannel(0, 32);
		for (int i = 0; i < packets.Count; i++)
		{
			//Get packet from array
			Dictionary packet = (Dictionary)packets[i];

			//Decompress packet
			byte[] data = packet["payload"].AsByteArray();
			Dictionary dictionary = (Dictionary) GD.BytesToVar(data.Decompress(data.LongLength, FileAccess.CompressionMode.Zstd));
			read(dictionary, (ulong)packet["identity"]);
		}
		
	}

	public void Handshake()
	{
		SendMessage("handshake");
	}

	public void SendMessage(string message)
	{
		SendPacketToAll(new Dictionary() {{"message", message}}, true);
	}

	public void SendPacket(ulong target, Dictionary dictionary, bool reliable)
	{
		// Set the sendType and channel
		int sendType = reliable ? (int)Steam.NetworkingSendReliable : (int)Steam.NetworkingSendNoDelay;
		int channel = 0;

		// Create a data array to send the data through
		byte[] data = GD.VarToBytes(dictionary).Compress(FileAccess.CompressionMode.Zstd);

		Steam.SendMessageToUser(target, data, sendType, channel);
	}
	public void SendPacketToAll(Dictionary dictionary, bool reliable) { 
		if (MatchMaker.Instance.Lobby != null)
		{
			// Loop through all peers
			foreach (ulong member in MatchMaker.Instance.Lobby.Members)
			{
				// Send package to each peer
				if (member != Profile.Instance.ID)
					SendPacket(member, dictionary, reliable);
			}
		}
	}

	private void read(Dictionary dictionary, ulong author)
	{
		if (dictionary.ContainsKey("message"))
		{
			// Do not post handshakes
			if (dictionary["message"].ToString() != "handshake")
				Console.Instance.Post(Steam.GetFriendPersonaName(author) + ": " + dictionary["message"]);
		}
		if (dictionary.ContainsKey("type") && dictionary["type"].ToString() == "update")
		{
			GameWarden.Instance?.Parse(dictionary, author);
		}
	}
}
