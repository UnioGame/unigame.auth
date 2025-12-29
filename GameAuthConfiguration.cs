namespace UniGame.Runtime.GameAuth
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.Serialization;

#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    
    [Serializable]
    public class GameAuthConfiguration
    {
        [Tooltip("If true, the auth service will cache user login data in local storage")]
        public bool userLoginCache = true;
        
        public float authTimeout = 30f;
        
#if ODIN_INSPECTOR
        [ListDrawerSettings(ListElementLabelName = "@providerName")]
#endif
        public List<GameLoginData> loginProviders = new();
    }
}