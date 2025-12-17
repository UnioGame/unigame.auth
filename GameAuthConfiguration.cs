namespace UniGame.Runtime.GameAuth
{
    using System;
    using System.Collections.Generic;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using UnityEngine.Serialization;

    [Serializable]
    public class GameAuthConfiguration
    {
        [Tooltip("if true, the auth service will use the device id to identify the user")]
        public bool debugMode = false;

        [Tooltip("If true, the auth service will cache user login data in local storage")]
        public bool userLoginCache = true;
        
        [Tooltip("If true, the auth service will use the target id as user id for server requests")]
        public bool overrideUserId = false;
        
        [FormerlySerializedAs("loginAsUser")]
        [ShowIf("overrideUserId")]
        public string targetUserId = string.Empty;
        
        public float authTimeout = 15f;
        
        [ListDrawerSettings(ListElementLabelName = "@providerName")]
        public List<GameLoginData> loginProviders = new();
    }
}