#if SUGARDROP_UGS
using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Pornirea serviciilor Unity și intrarea anonimă, comune pentru clasament și salvarea în cloud
    /// (o singură dată; la eșec se reîncearcă la următoarea cerere).
    /// </summary>
    public static class SugarServices
    {
        private static Task _signIn;

        public static Task SignIn()
        {
            if (_signIn == null || _signIn.IsFaulted || _signIn.IsCanceled) _signIn = SignInAsync();
            return _signIn;
        }

        public static string PlayerId => AuthenticationService.Instance.PlayerId;

        private static async Task SignInAsync()
        {
            // Fără proiect Unity Cloud legat, serviciile nu pot porni (ex. în Editor, înainte de configurare).
            if (string.IsNullOrEmpty(Application.cloudProjectId))
                throw new InvalidOperationException("proiectul nu e legat de Unity Cloud");
            if (UnityServices.State != ServicesInitializationState.Initialized) await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _signIn = null;
    }
}
#endif
