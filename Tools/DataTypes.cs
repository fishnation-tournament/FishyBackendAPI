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
}