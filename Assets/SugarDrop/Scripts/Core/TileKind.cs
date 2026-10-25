namespace SugarDrop
{
    /// <summary>Tipurile de piese de pe tablă.</summary>
    public enum TileKind
    {
        /// <summary>Capac obișnuit, colorat.</summary>
        Normal,
        /// <summary>Capac dungat orizontal (din 4 în linie): curăță tot rândul.</summary>
        StripedRow,
        /// <summary>Capac dungat vertical (din 4 în coloană): curăță toată coloana.</summary>
        StripedColumn,
        /// <summary>Capac ambalat (din L sau T): explodează pe o zonă 3×3.</summary>
        Wrapped,
        /// <summary>Capac curcubeu (din 5 în linie): schimbat cu o culoare, curăță toate capacele de acea culoare.</summary>
        Rainbow,
        /// <summary>Biscuit (obstacol): nu se mută, nu se aliniază; se sparge de alinierile vecine sau de explozii.</summary>
        Biscuit,
        /// <summary>Bomboană-ingredient: nu se aliniază, coboară odată cu piesele și iese jos, în borcan.</summary>
        Ingredient,
        /// <summary>Ciocolată (obstacol): nu se mută; se sparge de alinierile vecine; crește dacă nu e spartă.</summary>
        Chocolate,
    }
}
