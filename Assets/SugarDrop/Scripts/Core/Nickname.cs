using System.Text;

namespace SugarDrop
{
    /// <summary>Regulile pentru numele din clasament: litere, cifre, spații și câteva semne, între 3 și 16 caractere.</summary>
    public static class Nickname
    {
        public const int MinLength = 3;
        public const int MaxLength = 16;

        private static readonly string[] Words = { "Bomboana", "Jeleu", "Caramel", "Cupcake", "Acadea", "Praline", "Fondant", "Bezea" };

        /// <summary>Curăță textul scris de jucător: scoate caracterele nepermise, spațiile duble și taie la lungimea maximă.</summary>
        public static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var builder = new StringBuilder(MaxLength);
            foreach (char c in text.Trim())
            {
                if (builder.Length >= MaxLength) break;
                if (char.IsWhiteSpace(c))
                {
                    if (builder.Length > 0 && builder[builder.Length - 1] != ' ') builder.Append(' ');
                }
                else if (IsAllowed(c)) builder.Append(c);
            }
            return builder.ToString().Trim();
        }

        public static bool IsValid(string text)
        {
            string clean = Clean(text);
            return clean.Length >= MinLength && clean == text;
        }

        /// <summary>Un nume propus din oficiu (ex. „Jeleu4821”), pe care jucătorul îl poate păstra sau schimba.</summary>
        public static string Suggest(System.Random random) => Words[random.Next(Words.Length)] + random.Next(100, 10000);

        private static bool IsAllowed(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.';
    }
}
