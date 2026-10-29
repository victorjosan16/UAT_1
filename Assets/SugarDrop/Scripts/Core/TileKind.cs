namespace SugarDrop
{
    /// <summary>Tipurile de piese de pe tablă.</summary>
    public enum TileKind
    {
        /// <summary>Bomboană obișnuită, colorată (se elimină în linii de 3+ piese la fel).</summary>
        Normal,
        /// <summary>Rachetă orizontală (din 4 în linie verticală): curăță tot rândul.</summary>
        RocketH,
        /// <summary>Rachetă verticală (din 4 în linie orizontală): curăță toată coloana.</summary>
        RocketV,
        /// <summary>Bombă (din linii în L sau T): explozie 3×3.</summary>
        Bomb,
        /// <summary>Bomboană curcubeu (din 5 în linie): elimină toate bomboanele unei culori.</summary>
        Rainbow,
        /// <summary>Bomboană-ingredient: coboară odată cu piesele și iese jos, în borcan.</summary>
        Ingredient,
        /// <summary>Ladă de lemn (obstacol): nu se mișcă; se sparge din exploziile vecine (1–2 lovituri).</summary>
        Crate,
        /// <summary>Ciocolată (obstacol): ca lada, dar crește dacă într-o mutare nu e lovită nicio ciocolată.</summary>
        Chocolate,
        /// <summary>Ghiveci cu floare (Pădurea de Mentă): nu se mișcă; liniile vecine îl „udă” (3 lovituri) până înflorește.</summary>
        Pot,
        /// <summary>Foc (Aventurile lui Bobo): nu se mișcă, se întinde spre Bobo și se stinge cu apă (bomboane albastre) sau explozii.</summary>
        Fire,
        /// <summary>Bobo prins pe loc (nivelurile cu foc): nu se mișcă și nu poate fi lovit.</summary>
        Hero,
    }

    /// <summary>Pragurile pentru piesele speciale (configurabile într-un singur loc).</summary>
    public static class BlastRules
    {
        /// <summary>Cel mai mic grup care se poate elimina.</summary>
        public const int MinGroup = 3;
        public const int RocketMin = 5;
        public const int BombMin = 7;
        public const int RainbowMin = 10;
        /// <summary>Latura zonei bombei (3 = 3×3, ca în jocurile clasice); două bombe combinate: 5×5.</summary>
        public const int BombSize = 3;
        public const int BombComboSize = 5;
        public const int MaxIce = 3;
        /// <summary>De câte ori trebuie „udat” un ghiveci până înflorește.</summary>
        public const int PotHits = 3;
        /// <summary>„Culoarea” lui Bobo când e piesă pe tablă (coboară și iese jos, ca bomboanele-ingredient).</summary>
        public const int BoboColor = 50;
        /// <summary>Bomboanele care sunt apă: liniile lor sting focul vecin (afinele albastre).</summary>
        public const int WaterColor = 1;

        public static bool IsSpecial(TileKind kind) =>
            kind == TileKind.RocketH || kind == TileKind.RocketV || kind == TileKind.Bomb || kind == TileKind.Rainbow;

        public static bool IsRocket(TileKind kind) => kind == TileKind.RocketH || kind == TileKind.RocketV;
    }
}
