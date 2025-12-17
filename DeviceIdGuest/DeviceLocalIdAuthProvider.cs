namespace UniGame.Runtime.GameAuth.DeviceIdGuest
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UnityEngine.Device;

    [Serializable]
    public class DeviceLocalIdAuthProvider : IGameAuthProvider
    {
        private readonly string _id;

        public DeviceLocalIdAuthProvider(string id)
        {
            _id = id;
        }
        
        public string Id => _id;
        public bool IsAuthenticated => true;
        
        public bool AllowRestoreAccount => false;
        
        public bool AllowRegisterAccount => false;

        public async UniTask<AuthProviderResult> RegisterAsync(ILoginContext context)
        {
            return await LoginAsync(context);
        }
        
        public async UniTask<AuthProviderResult> LoginAsync(ILoginContext context,CancellationToken cancellationToken = default)
        {
            var id = SystemInfo.deviceUniqueIdentifier;
            return new AuthProviderResult()
            {
                data = new GameAuthData()
                {
                    userId = id,
                },
                error = string.Empty,
                success = true,
            };
        }

        public UniTask<SignOutResult> SignOutAsync()
        {
            return UniTask.FromResult(new SignOutResult(){success = true, error = string.Empty});
        }
    }
}