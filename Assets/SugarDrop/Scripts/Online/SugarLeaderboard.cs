using System;
using System.Collections.Generic;
using UnityEngine;
#if SUGARDROP_UGS
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
#endif

namespace SugarDrop
{
    /// <summary>Un rând din clasament.</summary>
    public readonly struct LeaderboardRow
    {
        /// <summary>Locul (de la 1).</summary>
        public readonly int Rank;
        public readonly string Name;
        public readonly long Score;
        public readonly bool IsMe;

        public LeaderboardRow(int rank, string name, long score, bool isMe)
        {
            Rank = rank;
            Name = name;
            Score = score;
            IsMe = isMe;
        }
    }

    /// <summary>Ce a primit ecranul de clasament: primii jucători, rândul tău și dacă datele sunt online.</summary>
    public class LeaderboardResult
    {
        public bool Online;
        /// <summary>Motivul, când clasamentul online nu merge (se arată sub mesaj, ca să se vadă cauza).</summary>
        public string Error = "";
        public readonly List<LeaderboardRow> Top = new();
        /// <summary>Rândul jucătorului (Rank = 0 dacă încă nu e în clasament).</summary>
        public LeaderboardRow Me;
    }

    /// <summary>
    /// Clasamentul global: punctele sunt suma celor mai bune scoruri de pe fiecare nivel (<see cref="SugarSave.TotalScore"/>),
    /// cu nickname-ul jucătorului. Cu pachetul Unity Leaderboards instalat (simbolul SUGARDROP_UGS) jucătorul intră
    /// anonim în Unity Gaming Services și scorul se trimite online; altfel (sau fără internet) clasamentul arată
    /// doar rândul tău.
    /// </summary>
    public class SugarLeaderboard : MonoBehaviour
    {
        public static SugarLeaderboard Instance { get; private set; }

        /// <summary>Clasamentul online e disponibil în acest build (pachetul e instalat).</summary>
        public static bool OnlineAvailable
        {
            get
            {
#if SUGARDROP_UGS
                return true;
#else
                return false;
#endif
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create() => Ensure();

        public static SugarLeaderboard Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("SugarLeaderboard");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<SugarLeaderboard>();
            return Instance;
        }

        private static string MyName => string.IsNullOrEmpty(SugarSave.Data.nickname) ? "?" : SugarSave.Data.nickname;

        private LeaderboardResult LocalResult()
        {
            var result = new LeaderboardResult { Me = new LeaderboardRow(0, MyName, SugarSave.TotalScore, true) };
            result.Top.Add(new LeaderboardRow(1, MyName, SugarSave.TotalScore, true));
            return result;
        }

#if SUGARDROP_UGS
        [Serializable]
        private class Meta
        {
            public string n;
        }

        private long _sentScore = -1;
        private string _sentName;
        private bool _sending;
        private bool _sendAgain;

        private void Start() => Submit();

        /// <summary>Trimite punctele și nickname-ul (doar dacă s-au schimbat de la ultima trimitere).</summary>
        public async void Submit()
        {
            if (string.IsNullOrEmpty(SugarSave.Data.nickname)) return;
            if (_sending)
            {
                _sendAgain = true;
                return;
            }
            _sending = true;
            try
            {
                do
                {
                    _sendAgain = false;
                    long score = SugarSave.TotalScore;
                    string name = SugarSave.Data.nickname;
                    if (score == _sentScore && name == _sentName) break;
                    await SugarServices.SignIn();
                    if (name != _sentName) await UpdatePlayerName(name);
                    var options = new AddPlayerScoreOptions { Metadata = new Meta { n = name } };
                    await LeaderboardsService.Instance.AddPlayerScoreAsync(SugarOnlineConfig.LeaderboardId, score, options);
                    _sentScore = score;
                    _sentName = name;
                } while (_sendAgain);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Clasament] Scorul nu a putut fi trimis: " + e.Message);
            }
            finally
            {
                _sending = false;
            }
        }

        /// <summary>Numele de cont din Unity (rezervă pentru rândurile fără metadate); nu poate conține spații.</summary>
        private static async Task UpdatePlayerName(string name)
        {
            try
            {
                await AuthenticationService.Instance.UpdatePlayerNameAsync(name.Replace(' ', '_'));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Clasament] Numele nu a putut fi actualizat: " + e.Message);
            }
        }

        /// <summary>Încarcă primii jucători și locul tău. done primește mereu un rezultat (local, dacă nu merge online).</summary>
        public async void Load(Action<LeaderboardResult> done)
        {
            LeaderboardResult result;
            try
            {
                await SugarServices.SignIn();
                string myId = SugarServices.PlayerId;
                result = new LeaderboardResult { Online = true };
                var page = await LeaderboardsService.Instance.GetScoresAsync(SugarOnlineConfig.LeaderboardId,
                    new GetScoresOptions { Limit = SugarOnlineConfig.TopCount, IncludeMetadata = true });
                foreach (var entry in page.Results)
                    result.Top.Add(new LeaderboardRow(entry.Rank + 1, EntryName(entry.Metadata, entry.PlayerName), (long)entry.Score, entry.PlayerId == myId));
                result.Me = new LeaderboardRow(0, MyName, SugarSave.TotalScore, true);
                try
                {
                    var mine = await LeaderboardsService.Instance.GetPlayerScoreAsync(SugarOnlineConfig.LeaderboardId,
                        new GetPlayerScoreOptions { IncludeMetadata = true });
                    result.Me = new LeaderboardRow(mine.Rank + 1, MyName, (long)mine.Score, true);
                }
                catch (Exception)
                {
                    // Încă nu ești în clasament (n-ai trimis niciun scor).
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Clasament] Nu am putut încărca clasamentul: " + e.Message);
                result = LocalResult();
                result.Error = e.GetType().Name + ": " + e.Message;
            }
            if (this != null) done?.Invoke(result);
        }

        /// <summary>Nickname-ul din metadate; altfel numele contului Unity fără sufixul „#1234”.</summary>
        private static string EntryName(string metadata, string playerName)
        {
            if (!string.IsNullOrEmpty(metadata))
            {
                try
                {
                    var meta = JsonUtility.FromJson<Meta>(metadata);
                    if (meta != null && !string.IsNullOrEmpty(meta.n)) return Nickname.Clean(meta.n);
                }
                catch (Exception)
                {
                    // Metadate stricate: folosim numele contului.
                }
            }
            if (string.IsNullOrEmpty(playerName)) return "?";
            int hash = playerName.LastIndexOf('#');
            return (hash > 0 ? playerName.Substring(0, hash) : playerName).Replace('_', ' ');
        }
#else
        public void Submit()
        {
        }

        public void Load(Action<LeaderboardResult> done) => done?.Invoke(LocalResult());
#endif
    }
}
