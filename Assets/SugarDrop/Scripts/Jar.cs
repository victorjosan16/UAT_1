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

        public void ResetJar(int goal)
        {
            Goal = goal;
            Count = 0;
            CountChanged?.Invoke(Count);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.TryGetComponent(out Candy candy) || candy.Counted) return;
            candy.Counted = true;
            Count++;
            CountChanged?.Invoke(Count);
        }
    }
}
