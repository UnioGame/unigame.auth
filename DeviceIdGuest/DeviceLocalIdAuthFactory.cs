namespace UniGame.Runtime.GameAuth.DeviceIdGuest
{
    using System;
    using Core.Runtime;
    using Cysharp.Threading.Tasks;

    [Serializable]
    public class DeviceLocalIdAuthFactory : IAuthProviderFactory
    {
        public async UniTask<IGameAuthProvider> CreateAsync(string id,IContext context)
        {
            return new DeviceLocalIdAuthProvider(id);
        }
    }
}