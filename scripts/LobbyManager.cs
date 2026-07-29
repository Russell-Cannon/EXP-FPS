using Godot;
using System;
using GodotSteam;
using System.Collections.Generic;

public partial class LobbyManager : Node
{
	public List<ulong> Members {get; private set;} = new List<ulong>();
	public ulong ID {get; private set;}
	GameWarden warden;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		warden = new();
		AddChild(warden);
		Steam.LobbyChatUpdate += LobbyChatUpdate;
		//Get Steam IDs for each member in the lobby
		for (int i = 0; i < Steam.GetNumLobbyMembers(ID); i++)
		{
			ulong memberID = Steam.GetLobbyMemberByIndex(ID, i);
			Members.Add(memberID);
			warden.AddPlayer(memberID);
		}
	}

    public override void _ExitTree()
	{
		Steam.LobbyChatUpdate -= LobbyChatUpdate;
		//close session with users // Obsolete
		// foreach (ulong member in Members) 
		// 	Steam.CloseP2PSessionWithUser(member);
	}

	public LobbyManager(ulong lobbyID)
	{
		ID = lobbyID;
	}

	void LobbyChatUpdate(ulong lobbyId, long changedId, long makingChangeId, long chatState)
	{
		if (chatState == (int)Steam.ChatMemberStateChange.Entered)
			HandlePlayerJoined((ulong)changedId);
		else // Player kicked, banned, left, or quit
			HandlePlayerLeft((ulong)changedId);
	}
	public void HandlePlayerJoined(ulong UID)
	{
		Console.Instance.Post(Steam.GetFriendPersonaName(UID) + " joined");
		warden.AddPlayer(UID);
		Members.Add(UID);
	}
	public void HandlePlayerLeft(ulong UID)
	{
		Console.Instance.Post(Steam.GetFriendPersonaName(UID) + " left");
		warden.RemovePlayer(UID);
		Members.Remove(UID);
	}
}
