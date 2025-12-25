namespace Nakama
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using Extensions;
    using UniGame.Core.Runtime;
    using UniGame.MetaBackend.Runtime;
    using UniGame.MetaBackend.Runtime.Contracts;
    using UniGame.Runtime.GameAuth;

    [Serializable]
    public class NakamaDeviceIdAuthProvider : IGameAuthProvider
    {
        private bool _authenticated;
        private NakamaAuthContract _nakamaAuthContract = new();
        private NakamaRestoreSessionContract _restoreSessionContract = new();
        private NakamaLogoutContract _nakamaLogoutContract = new();
        
        public bool IsAuthenticated => _authenticated;

        public bool AllowRegisterAccount => true;

        public bool AllowRestoreAccount => throw new NotImplementedException();

        public bool CheckAuthContext(IAuthContext context)
        {
            return context is NakamaDeviceIdContext;
        }

        public async UniTask<AuthProviderResult> LoginAsync(IAuthContext context, CancellationToken cancellationToken = default)
        {
            var deviceContext = context as NakamaDeviceIdContext;
            var deviceId = deviceContext.deviceId;

            _nakamaAuthContract.authData = new NakamaDeviceIdAuthData()
            {
                deviceId = deviceId,
                create = true,
                userName = string.IsNullOrEmpty(deviceContext.userName) ? deviceId : deviceContext.userName
            };

            var response = await _nakamaAuthContract
                .ExecuteAsync(cancellationToken);

            return ToProviderResult(response.data);
        }

        public async UniTask<AuthProviderResult> RestoreAsync(IAuthContext context, CancellationToken cancellationToken = default)
        {
            var response = await _restoreSessionContract.ExecuteAsync(cancellationToken);
            return ToProviderResult(response.data);
        }

        public async UniTask<AuthSignOutResult> SignOutAsync()
        {
            var result = await _nakamaLogoutContract.ExecuteAsync();
            return new AuthSignOutResult()
            {
                success = result.success,
            };
        }
        
        private AuthProviderResult ToProviderResult(NakamaAuthResult response)
        {
            var success = response is { success: true };
            if (!success) return AuthProviderResult.Failed;
            
            var result = new AuthProviderResult()
            {
                success = true,
                error = response.error,
            };
            
            if(!response.success) return AuthProviderResult.Failed;

            result.data = new GameAuthData()
            {
                userId = response.account.User.Id,
                displayName = response.account.User.DisplayName,
                token = response.account.User.Id,
                photoUrl = response.account.User.AvatarUrl,
                email = response.account.Email,
            };

            _authenticated = true;

            return result;
        }
    }

    [Serializable]
    public class NakamaDeviceIdContext : IAuthContext
    {
        public string deviceId;
        public string userName;
        public bool create = true;
    }
    
    [Serializable]
    public class NakamaDeviceIdAuthFactory : IAuthProviderFactory
    {
        public string ProviderId => "nakama_device_id";

        public async UniTask<IGameAuthProvider> CreateAsync(string id, IContext context)
        {
            return new NakamaDeviceIdAuthProvider();
        }
    }
}