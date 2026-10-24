using System;
using UnityEngine;

namespace LastBlock.Core
{
    /// <summary>
    /// Session score. The score at the start of a level is remembered so a retry restores it.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public const int ExtractionPoints = 100;
        public const int PerfectBonus = 50;
        public const int LevelCompletePoints = 200;
        public const string BestScoreKey = "LastBlock.BestScore";

        public int Score { get; private set; }
        public int LevelStartScore { get; private set; }
        public int BestScore => PlayerPrefs.GetInt(BestScoreKey, 0);

        public event Action<int> ScoreChanged;

        /// <summary>Call when a new level starts (not on retry).</summary>
        public void BeginLevel()
        {
            LevelStartScore = Score;
        }

        /// <summary>Restores the score the player had when the current level began.</summary>
        public void ResetToLevelStart()
        {
            SetScore(LevelStartScore);
        }

        public void ResetAll()
        {
            LevelStartScore = 0;
            SetScore(0);
        }

        /// <summary>Adds points for a removed block and returns the points awarded.</summary>
        public int AddExtraction(bool perfect)
        {
            int points = ExtractionPoints + (perfect ? PerfectBonus : 0);
            SetScore(Score + points);
            return points;
        }

        public int AddLevelComplete()
        {
            SetScore(Score + LevelCompletePoints);
            return LevelCompletePoints;
        }

        /// <summary>Saves the best score locally. Returns true if this is a new record.</summary>
        public bool SaveBestScore()
        {
            if (Score <= BestScore) return false;
            PlayerPrefs.SetInt(BestScoreKey, Score);
            PlayerPrefs.Save();
            return true;
        }

        void SetScore(int value)
        {
            Score = Mathf.Max(0, value);
            ScoreChanged?.Invoke(Score);
        }
    }
}
