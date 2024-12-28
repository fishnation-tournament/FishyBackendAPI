using System.Diagnostics.CodeAnalysis;
namespace FishyAPI.Tools;

public static class FrontFacingDataTypes
{
    public struct FrontFacingMap
    {
        public string BSMapID { get; set; }
        public string MapName { get; set; }
        public string MapImageLink { get; set; }
        public ulong MapPoolID { get; set; }
        public int Season { get; set; }
        public string Difficulty { get; set; }
    }
    
    public struct FrontFacingMapPool
    {
        public string MapPoolName { get; set; }
        public string MapPoolDescription { get; set; }
        public List<FrontFacingMap> Maps { get; set; }
        public int Season { get; set; }
    }
    
    public struct FrontFacingMatch
    {
        public ulong MapPoolID { get; set; }
        public ulong Player1ID { get; set; }
        public ulong Player2ID { get; set; }
        public bool Complete { get; set; }
        public ulong WinnerID { get; set; }
        public int Player1Score { get; set; }
        public int Player2Score { get; set; }
        public DateTime MatchDate { get; set; }
        public List<FrontFacingMatchScore> Scores { get; set; }
        public int Season { get; set; }
        
    }

    public struct FrontFacingMatchScore
    {
        public ulong UID { get; set; }
        public ulong MapID { get; set; }
        public ulong MatchID { get; set; }
        public int Score { get; set; }
        public int MaxScore { get; set; }
        public int Misses { get; set; }
    }
    
    public struct FrontFacingQualifierScore
    {
        public ulong UID { get; set; }
        public ulong MapID { get; set; }
        public int Score { get; set; }
        public int MaxScore { get; set; }
        public int Misses { get; set; }
        public ulong QualifierID { get; set; } 
    }
    
    public struct AuthResponse
    {
        public int ExpiresIn { get; set; }
        public string AccessToken { get; set; } 
    }
    
    public struct DiscordUser
    {
        public string id { get; set; }
        public string username { get; set; }
        public string discriminator { get; set; }
        public string avatar { get; set; }
        public bool verified { get; set; }
        public string email { get; set; }
        public int flags { get; set; }
        public string banner { get; set; }
        public int accent_color { get; set; }
        public int premium_type { get; set; }
        public int public_flags { get; set; }
        public Dictionary<string, string> avatar_decoration_data { get; set; }
    }
}