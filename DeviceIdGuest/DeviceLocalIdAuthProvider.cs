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

        public async UniTask<AuthProviderResult> RegisterAsync(IAuthContext context)
        {
            return await LoginAsync(context);
        }

        public bool CheckAuthContext(IAuthContext context)
        {
            return context is LocalDeviceIdContext;
        }

        public async UniTask<AuthProviderResult> LoginAsync(IAuthContext context,CancellationToken cancellationToken = default)
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

        public async UniTask<AuthProviderResult> RestoreAsync(IAuthContext context, CancellationToken cancellationToken = default)
        {
            return new AuthProviderResult()
            {
                data = new GameAuthData()
                {
                    userId = SystemInfo.deviceUniqueIdentifier,
                },
                error = string.Empty,
                success = true,
            };
        }

        public UniTask<AuthSignOutResult> SignOutAsync()
        {
            return UniTask.FromResult(new AuthSignOutResult(){success = true, error = string.Empty});
        }
    }
    
    [Serializable]
    public class LocalDeviceIdContext : IAuthContext
    {
    }
}