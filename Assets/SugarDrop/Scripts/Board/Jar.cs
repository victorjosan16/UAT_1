using System;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>Borcanul de jos: numără bomboanele care intră în el (o singură dată fiecare).</summary>
    public class Jar : MonoBehaviour
    {
        public int Count { get; private set; }
        public int Goal { get; private set; }

        public event Action<int> CountChanged;
        /// <summary>O bomboană a intrat în borcan (poziția și culoarea ei, pentru sclipiri și sunet).</summary>
        public event Action<Vector3, Color> CandyEntered;

        public void ResetJar(int goal)
        {
            Goal = goal;
            Count = 0;
            CountChanged?.Invoke(Count);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out Candy candy) || candy.Counted) return;
            candy.MarkCounted();
            Count++;
            CountChanged?.Invoke(Count);
            CandyEntered?.Invoke(other.transform.position, candy.Color);
        }
    }
}
