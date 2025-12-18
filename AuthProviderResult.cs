namespace UniGame.Runtime.GameAuth
{
    using System;

    [Serializable]
    public class AuthProviderResult
    {
        public static readonly AuthProviderResult Failed = new()
        {
            success = false,
            error = "request failed",
            data = null,
        };
        
        public bool success = false;
        public string error = string.Empty;
        public GameAuthData data = new();
    }
    
    [Serializable]
    public class GameAuthResult
    {
        public static readonly GameAuthResult Failed = new()
        {
            success = false,
            error = "request failed",
            data = new GameAuthData(),
        };
        
        public string id = string.Empty;
        public bool success = false;
        public string error = string.Empty;
        public GameAuthData data = new();
    }

    [Serializable]
    public class GameRegisterResult
    {
        public string id = string.Empty;
        public bool success = false;
        public string error = string.Empty;
        public GameAuthData data = new();
    }
}