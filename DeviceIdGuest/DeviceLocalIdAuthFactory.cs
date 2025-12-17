namespace UniGame.Runtime.GameAuth.DeviceIdGuest
{
    using System;
    using Cysharp.Threading.Tasks;

    [Serializable]
    public class DeviceLocalIdAuthFactory : IAuthProviderFactory
    {
        public async UniTask<IGameAuthProvider> CreateAsync(string id)
        {
            return new DeviceLocalIdAuthProvider(id);
        }
    }
}