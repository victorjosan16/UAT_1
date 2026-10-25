namespace SugarDrop
{
    /// <summary>Tipurile de piese de pe tablă.</summary>
    public enum TileKind
    {
        /// <summary>Bomboană obișnuită, colorată (se elimină în grupuri de 3+ piese la fel, lipite).</summary>
        Normal,
        /// <summary>Rachetă orizontală (din grup de 5–6): curăță tot rândul.</summary>
        RocketH,
        /// <summary>Rachetă verticală (din grup de 5–6): curăță toată coloana.</summary>
        RocketV,
        /// <summary>Bombă (din grup de 7–9): explozie 5×5.</summary>
        Bomb,
        /// <summary>Bomboană curcubeu (din grup de 10+): elimină toate bomboanele unei culori.</summary>
        Rainbow,
        /// <summary>Bomboană-ingredient: coboară odată cu piesele și iese jos, în borcan.</summary>
        Ingredient,
        /// <summary>Ladă de lemn (obstacol): nu se mișcă; se sparge din exploziile vecine (1–2 lovituri).</summary>
        Crate,
        /// <summary>Ciocolată (obstacol): ca lada, dar crește dacă într-o mutare nu e lovită nicio ciocolată.</summary>
        Chocolate,
    }

    /// <summary>Pragurile pentru piesele speciale (configurabile într-un singur loc).</summary>
    public static class BlastRules
    {
        /// <summary>Cel mai mic grup care se poate elimina.</summary>
        public const int MinGroup = 3;
        public const int RocketMin = 5;
        public const int BombMin = 7;
        public const int RainbowMin = 10;
        /// <summary>Latura zonei bombei (5 = 5×5).</summary>
        public const int BombSize = 5;
        public const int BombComboSize = 7;
        public const int MaxIce = 3;

        /// <summary>Ce piesă specială iese dintr-un grup de mărimea dată (Normal = niciuna).</summary>
        public static TileKind SpecialForGroup(int size, bool horizontal) =>
            size >= RainbowMin ? TileKind.Rainbow
            : size >= BombMin ? TileKind.Bomb
            : size >= RocketMin ? (horizontal ? TileKind.RocketH : TileKind.RocketV)
            : TileKind.Normal;

        public static bool IsSpecial(TileKind kind) =>
            kind == TileKind.RocketH || kind == TileKind.RocketV || kind == TileKind.Bomb || kind == TileKind.Rainbow;

        public static bool IsRocket(TileKind kind) => kind == TileKind.RocketH || kind == TileKind.RocketV;
    }
}
