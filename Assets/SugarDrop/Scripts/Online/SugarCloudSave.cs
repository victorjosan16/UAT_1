using System;
using System.Collections;
using UnityEngine;
#if SUGARDROP_CLOUDSAVE && SUGARDROP_UGS
using System.Collections.Generic;
using Unity.Services.CloudSave;
#endif

namespace SugarDrop
{
    /// <summary>
    /// Salvarea în cloud (Unity Cloud Save), legată de contul anonim al jucătorului. La pornire, dacă progresul din
    /// cloud e mai avansat (ex. după reinstalare), îl aduce pe telefon; după fiecare nivel câștigat și la ieșirea
    /// din joc trimite progresul de pe telefon. Fără pachet sau fără internet, jocul merge doar cu salvarea locală.
    /// </summary>
    public class SugarCloudSave : MonoBehaviour
    {
        public static SugarCloudSave Instance { get; private set; }

        /// <summary>Progresul a fost înlocuit cu cel din cloud (ecranele trebuie reîmprospătate).</summary>
        public static event Action Restored;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (Instance != null) return;
            var go = new GameObject("SugarCloudSave");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<SugarCloudSave>();
        }

#if SUGARDROP_CLOUDSAVE && SUGARDROP_UGS
        private const string Key = "progress";
        private bool _synced;
        private bool _uploadPending;
        private bool _uploading;

        private void Start() => Download();

        /// <summary>Cere o încărcare în cloud (se face după câteva secunde, ca mai multe schimbări să plece împreună).</summary>
        public void RequestUpload()
        {
            if (_uploadPending) return;
            _uploadPending = true;
            StartCoroutine(UploadSoon());
        }

        private IEnumerator UploadSoon()
        {
            yield return new WaitForSecondsRealtime(3f);
            _uploadPending = false;
            Upload();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Upload();
        }

        private async void Download()
        {
            try
            {
                await SugarServices.SignIn();
                var result = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { Key });
                if (result.TryGetValue(Key, out var item))
                {
                    SugarSaveData cloud = SugarSave.FromJson(item.Value.GetAs<string>());
                    if (SugarSave.IsAhead(cloud, SugarSave.Data))
                    {
                        Debug.Log("[Cloud] Progresul din cloud e mai avansat: îl aduc pe telefon.");
                        SugarSave.Replace(cloud);
                        Restored?.Invoke();
                    }
                }
                _synced = true;
                // Telefonul e mai avansat (sau cloudul e gol): trimitem progresul de aici.
                Upload();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Cloud] Salvarea din cloud nu a putut fi citită: " + e.Message);
            }
        }

        private async void Upload()
        {
            // Nu trimitem nimic înainte să citim cloudul (altfel am putea suprascrie un progres mai bun).
            if (!_synced || _uploading) return;
            _uploading = true;
            try
            {
                await SugarServices.SignIn();
                await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object> { { Key, SugarSave.ToJson() } });
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Cloud] Progresul nu a putut fi trimis: " + e.Message);
            }
            finally
            {
                _uploading = false;
            }
        }
#else
        public void RequestUpload()
        {
        }
#endif
    }
}
