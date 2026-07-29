using Godot;
using System;
using GodotSteam;
using System.Reflection.Metadata;

public partial class MatchMaker : Node
{
    public static MatchMaker Instance {get; private set;}
    public const int MAX_PLAYERS = 8;
    public LobbyManager Lobby = null;
    
    public override void _Ready()
    {
        Instance = this;

        //On lobby created: set data
        Steam.LobbyCreated += (long connectCode, ulong lobbyID) => {
            //Quit if connection fails
            if (connectCode != 1) return;
            Console.Instance.Post("Created Match");

            // Set this lobby as joinable
            Steam.SetLobbyJoinable(lobbyID, true);

            // Set some lobby data
            Steam.SetLobbyData(lobbyID, "name", Profile.Instance.ID + ":" + lobbyID);
            Steam.SetLobbyData(lobbyID, "version", Constants.Instance.VERSION);
        };

        //On lobby joined: grab its ID
        Steam.LobbyJoined += (ulong lobby, long permissions, bool locked, long response) => {
            // Create a new lobby manager for this ID
            AddChild(Lobby = new LobbyManager(lobby));
            Console.Instance.Post("Joined Match");
            Network.Instance.Handshake();
        };

        Steam.LobbyMatchList += _matchListFound;        
    }
    public void Host()
    {
        if (Lobby != null)
        {
            Console.Instance.Post("You are currently in a match.");
            return;
        }

        Console.Instance.Post("Creating match...");
        Steam.CreateLobby(Steam.LobbyType.Public, MAX_PLAYERS);
    }
    public void Join(ulong lobby)
    {
        Console.Instance.Post("Found match " + lobby, true);
		Steam.JoinLobby(lobby);
    }
    public void FindMatch()
    {
        if (Lobby != null)
        {
            Console.Instance.Post("You are currently in a match.");
            return;
        }

        Console.Instance.Post("Looking for matches...");
        //Limit to non-full lobbies
        Steam.AddRequestLobbyListFilterSlotsAvailable(1);

        //Limit to games of the same version
        Steam.AddRequestLobbyListStringFilter("version", Constants.Instance.VERSION, Steam.LobbyComparison.LobbyComparisonEqual);

		Steam.RequestLobbyList();
        //Continue on _matchListFound
    }
    public void LeaveMatch()
    {
        if (Lobby == null)
        {
            Console.Instance.Post("You are not currently in a match.");
            return;
        }

		// Send leave request to Steam
		Steam.LeaveLobby(Lobby.ID);

        // Close the lobby
        Lobby.QueueFree();
        Lobby = null;

        Console.Instance.Post("Left Match");
    }
    private void _matchListFound(Godot.Collections.Array lobbies)
    {
        Console.Instance.Post("Lobbies found: " + lobbies, true);
        if (lobbies.Count > 0) 
            Join((ulong)lobbies[0]);
        else
            Console.Instance.Post("No lobbies found.");
    }
}
