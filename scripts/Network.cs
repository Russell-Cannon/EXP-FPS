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
		SendMessage("handshake", true);
	}

	public void SendMessage(ulong target, string message, bool reliable)
	{
		sendPacket(target, new Dictionary() {{"message", message}}, reliable);
	}
	public void SendMessage(string message, bool reliable)
	{
		sendPacket(new Dictionary() {{"message", message}}, reliable);
	}

	private void sendPacket(ulong target, Dictionary dictionary, bool reliable)
	{
		// Set the sendType and channel
		int sendType = reliable ? (int)Steam.NetworkingSendReliable : (int)Steam.NetworkingSendNoDelay;
		int channel = 0;

		// Create a data array to send the data through
		byte[] data = GD.VarToBytes(dictionary).Compress(FileAccess.CompressionMode.Zstd);

		Steam.SendMessageToUser(target, data, sendType, channel);
	}
	private void sendPacket(Dictionary dictionary, bool reliable) { 
		if (MatchMaker.Instance.Lobby != null)
		{
			// Loop through all peers
			foreach (ulong member in MatchMaker.Instance.Lobby.Members)
			{
				// Send package to each peer
				sendPacket(member, dictionary, reliable);
			}
		}
	}

	private void read(Dictionary dictionary, ulong author)
	{
		if (dictionary.ContainsKey("message"))
		{
			// Do not post handshakes
			if (dictionary["message"].ToString() == "handshake") return;
			Console.Instance.Post(Steam.GetFriendPersonaName(author) + ": " + dictionary["message"]);
		}
	}
}
