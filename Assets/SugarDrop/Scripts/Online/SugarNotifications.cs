using System;
using UnityEngine;
#if SUGARDROP_NOTIFICATIONS && UNITY_ANDROID
using Unity.Notifications.Android;
#endif

namespace SugarDrop
{
    /// <summary>
    /// Notificările locale (fără server): „Viețile tale sunt pline!”, cadoul și misiunile zilei, și un „ne e dor de
    /// tine” după câteva zile. Se programează când jocul trece în fundal și se anulează când revii. Funcționează pe
    /// Android cu pachetul Mobile Notifications; se pot opri din Setări.
    /// </summary>
    public class SugarNotifications : MonoBehaviour
    {
        private const string Channel = "sugar_drop";
        private const int GiftHour = 10;
        private const int ComebackDays = 3;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var go = new GameObject("SugarNotifications");
            DontDestroyOnLoad(go);
            go.AddComponent<SugarNotifications>();
        }

        /// <summary>Cere permisiunea de notificări (Android 13+), o singură dată, după prima victorie.</summary>
        public static void AskPermissionOnce()
        {
            SugarSaveData data = SugarSave.Data;
            if (data.notificationsAsked) return;
            data.notificationsAsked = true;
            SugarSave.Save();
#if SUGARDROP_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                new PermissionRequest();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Notificări] Permisiunea nu a putut fi cerută: " + e.Message);
            }
#endif
        }

        private void Start() => CancelAll();

        private void OnApplicationPause(bool paused)
        {
            if (paused) Schedule();
            else CancelAll();
        }

        private void OnApplicationQuit() => Schedule();

        /// <summary>Momentul la care viețile vor fi pline (null dacă sunt deja pline).</summary>
        public static DateTime? LivesFullAt(int lives, int maxLives, int secondsToNext, int regenSeconds, DateTime now)
        {
            if (lives >= maxLives) return null;
            return now.AddSeconds(secondsToNext + (maxLives - lives - 1) * regenSeconds);
        }

        /// <summary>Ora cadoului: mâine la 10:00 (sau azi la 10:00, dacă e încă dimineață și cadoul de azi n-a fost luat).</summary>
        public static DateTime GiftReminderAt(DateTime now, bool giftAvailableToday)
        {
            DateTime today = now.Date.AddHours(GiftHour);
            return giftAvailableToday && now < today ? today : now.Date.AddDays(1).AddHours(GiftHour);
        }

#if SUGARDROP_NOTIFICATIONS && UNITY_ANDROID && !UNITY_EDITOR
        private static bool _channelReady;

        private static void EnsureChannel()
        {
            if (_channelReady) return;
            _channelReady = true;
            AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel
            {
                Id = Channel,
                Name = "Drop Quest",
                Importance = Importance.Default,
                Description = SugarLoc.T("Vieți pline, cadoul zilnic și misiuni"),
            });
        }

        private static void CancelAll()
        {
            try
            {
                AndroidNotificationCenter.CancelAllNotifications();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Notificări] " + e.Message);
            }
        }

        private static void Schedule()
        {
            CancelAll();
            if (!SugarSave.Data.notifications) return;
            try
            {
                EnsureChannel();
                DateTime now = DateTime.Now;
                SugarSave.UpdateLives();
                SugarSaveData data = SugarSave.Data;
                DateTime? full = LivesFullAt(data.lives, SugarSave.MaxLives, SugarSave.SecondsToNextLife, SugarSave.LifeRegenSeconds, now);
                if (full.HasValue) Send(SugarLoc.T("Viețile tale sunt pline!"), SugarLoc.T("Bobo te așteaptă la următorul nivel."), full.Value);
                Send(SugarLoc.T("Cadoul zilnic te așteaptă!"), SugarLoc.T("Ia-ți cadoul și vezi misiunile noi de azi."),
                    GiftReminderAt(now, SugarDaily.GiftAvailable));
                Send(SugarLoc.T("Bobo îți duce dorul!"), SugarLoc.T("Bomboanele sunt gata de potrivit. Revii la o partidă?"),
                    now.AddDays(ComebackDays));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Notificări] Nu au putut fi programate: " + e.Message);
            }
        }

        private static void Send(string title, string text, DateTime at)
        {
            var notification = new AndroidNotification
            {
                Title = title,
                Text = text,
                FireTime = at,
            };
            AndroidNotificationCenter.SendNotification(notification, Channel);
        }
#else
        private static void CancelAll()
        {
        }

        private static void Schedule()
        {
        }
#endif
    }
}
