using System.Diagnostics.CodeAnalysis;
namespace FishyAPI.Tools;

public static class DataTypes
{
    public struct User
    {
        public ulong UID;
        public ulong OptBLUID;
        public string Username;
        public string UserPfpLink;
        public string UserBio; 
        public ulong DiscordID;
        public string DiscordUsername;
        public DateTime RegistrationDate;
        public string Role;
    }
    
    public struct Map
    {
        public ulong MID;
        public string BSMapID;
        public string MapName;
        public string MapImageLink;
        public ulong MapPoolID;
        public int Season;
    }
    
    public struct MapPool
    {
        public ulong MapPoolID;
        public string MapPoolName;
        public string MapPoolDescription;
        public List<Map> Maps;
        public int Season;
    }
    
    public struct Match
    {
        public ulong MID;
        public ulong MapPoolID;
        public ulong Player1ID;
        public ulong Player2ID;
        public ulong WinnerID;
        public int Player1Score;
        public int Player2Score;
        public DateTime MatchDate;
        public List<MatchScore> Scores;
        public int Season;
    }

    public struct MatchScore
    {
        public ulong ScoreID;
        public ulong UID;
        public ulong MapID;
        public ulong MatchID;
        public int Score;
        public int MaxScore;
        public int Misses;
    }
    
    public struct QualifierScore
    {
        public ulong ScoreID;
        public ulong UID;
        public ulong MapID;
        public int Score;
        public int MaxScore;
        public int Misses;
    }
}