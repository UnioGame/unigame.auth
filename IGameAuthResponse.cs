namespace UniGame.Runtime.GameAuth
{
    using System;

    [Serializable]
    public class GameAuthData
    {
        public string userId = string.Empty;
        public string email = string.Empty;
        public string displayName = string.Empty;
        public string photoUrl = string.Empty;
        public string token = string.Empty;
    }
}