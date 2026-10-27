using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Tabla pregătită pentru antrenamentul de la nivelul 1: fără linii gata făcute, cu o mutare evidentă sus
    /// (3 la fel în linie) și una jos (4 la rând → rachetă), departe una de alta, ca primele căderi să nu le strice.
    /// Culorile 0–3 (nivelul 1 are 4 arome). Rândurile sunt scrise de sus (y = 8) în jos (y = 0).
    /// </summary>
    public static class TutorialBoard
    {
        private static readonly string[] Rows =
        {
            "012321230",
            "230223012",
            "012301230",
            "230123012",
            "012301230",
            "230123012",
            "012301230",
            "230123012",
            "011312230",
        };

        /// <summary>Pasul 1: bomboana mutată în sus face 3 la fel pe rândul de sus.</summary>
        public static readonly Vector2Int MatchFrom = new(3, 7);
        public static readonly Vector2Int MatchTo = new(3, 8);

        /// <summary>Pasul 2: bomboana mutată în jos face 4 la rând pe rândul de jos (racheta apare în celula de jos).</summary>
        public static readonly Vector2Int RocketFrom = new(3, 1);
        public static readonly Vector2Int RocketTo = new(3, 0);

        public static int ColorAt(Vector2Int cell)
        {
            int row = Rows.Length - 1 - cell.y;
            if (row < 0 || row >= Rows.Length || cell.x < 0 || cell.x >= Rows[row].Length) return 0;
            return Rows[row][cell.x] - '0';
        }
    }
}
