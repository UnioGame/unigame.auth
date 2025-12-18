namespace UniGame.Runtime.GameAuth.FirebaseEmail
{
    using System;

    [Serializable]
    public class EmailAuthContext : IAuthContext
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}