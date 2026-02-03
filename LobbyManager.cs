using Godot;
using System;
using GodotSteam;
using System.Collections.Generic;

public partial class LobbyManager : Node
{
	public List<ulong> Members {get; private set;} = new List<ulong>();
	public ulong ID {get; private set;}
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//Get Steam IDs for each member in the lobby
		for (int i = 0; i < Steam.GetNumLobbyMembers(ID); i++)
		{
			Members.Add(Steam.GetLobbyMemberByIndex(ID, i));
		}
	}

    public override void _ExitTree()
	{
		//close session with users // Obsolete
		// foreach (ulong member in Members) 
		// 	Steam.CloseP2PSessionWithUser(member);
	}


	public LobbyManager(ulong lobbyID)
	{
		ID = lobbyID;
	}
}
