using System;

namespace QuarryEmpire
{
    /// <summary>Orice personaj care lovește cu târnăcopul (jucător, muncitor). Folosit de PickaxeSwing.</summary>
    public interface ISwingSource
    {
        event Action<MineBlock> Swung;
    }
}
