using UnityEngine;

namespace SugarDrop
{
    /// <summary>O bomboană din grămadă. Borcanul o numără o singură dată.</summary>
    public class Candy : MonoBehaviour
    {
        public bool Counted { get; set; }
        public Color Color { get; set; } = Color.white;
    }
}
