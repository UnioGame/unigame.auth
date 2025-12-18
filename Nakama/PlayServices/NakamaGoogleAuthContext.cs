namespace PlayGames.NakamaGoogle
{
    using System;
    using UniGame.Runtime.GameAuth;

    [Serializable]
    public class NakamaGoogleAuthContext : IAuthContext
    {
        public bool createAccount = true;
        public bool linkAccount;
    }
}