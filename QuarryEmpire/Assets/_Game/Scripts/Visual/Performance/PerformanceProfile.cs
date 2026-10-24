using UnityEngine;
using UnityEngine.Rendering;

namespace QuarryEmpire
{
    /// <summary>
    /// Un profil grafic (ex. „Calitate 60 FPS”, „Economic 30 FPS”). Fiecare profil are propriul
    /// asset URP (umbre, MSAA, render scale) creat de VisualSetup în _Game/Settings/Performance.
    /// </summary>
    [CreateAssetMenu(fileName = "PerformanceProfile", menuName = "Quarry Empire/Performance Profile")]
    public class PerformanceProfile : ScriptableObject
    {
        [field: SerializeField] public string DisplayName { get; private set; } = "Calitate (60 FPS)";
        [field: SerializeField, Range(20, 120)] public int TargetFrameRate { get; private set; } = 60;
        [field: Tooltip("Asset-ul URP folosit de profil. Gol = rămâne cel din Project Settings.")]
        [field: SerializeField] public RenderPipelineAsset PipelineAsset { get; private set; }
        [field: Tooltip("Înmulțitor pentru numărul de particule (1 = toate).")]
        [field: SerializeField, Range(0f, 1f)] public float ParticleDensity { get; private set; } = 1f;
        [field: Tooltip("Umbre în timp real pe camera principală (oprit = cel mai ieftin, fără umbre).")]
        [field: SerializeField] public bool CameraShadows { get; private set; } = true;
    }
}
