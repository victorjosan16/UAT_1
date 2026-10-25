using System;
using System.Collections.Generic;

namespace SugarDrop
{
    /// <summary>Atacurile unui Boss (toate anunțate cu o mutare înainte).</summary>
    public enum BossAttack
    {
        /// <summary>Pune ciocolată pe câteva celule.</summary>
        ChocolateSpread,
        /// <summary>Îngheață câteva bomboane.</summary>
        Freeze,
        /// <summary>Leagă câteva bomboane cu lanțuri.</summary>
        Lock,
        /// <summary>Amestecă bomboanele.</summary>
        Shuffle,
    }

    /// <summary>O fază a luptei: de la ce procent de viață începe, ce atacuri folosește (pe rând) și cât de des.</summary>
    public sealed class BossPhase
    {
        public float FromHealth;
        public BossAttack[] Attacks;
        public int AttackEvery;
        public int Strength;
    }

    /// <summary>Un Boss: nume, viață și fazele luptei (un Boss obișnuit are o fază, un Mega-Boss trei).</summary>
    public sealed class BossDefinition
    {
        public string Name;
        public int Look;
        public int Health;
        public bool Mega;
        public BossPhase[] Phases;

        /// <summary>
        /// Boss-ul nivelului (la fiecare 10). 10 Regele Ciocolată, 20 Regina Gheții, 30 Dragonul de Bomboane,
        /// 40 Monstrul de Jeleu, 50 Împăratul Zahărului (Mega). De la 60, aceleași personaje combină atacurile,
        /// iar la fiecare 50 vine un Mega-Boss cu trei faze, tot mai puternic.
        /// </summary>
        public static BossDefinition For(int level, int moves)
        {
            int index = Math.Max(1, level / LevelConfig.ChapterSize);
            bool mega = LevelConfig.IsMegaBossLevel(level);
            int tier = (level - 1) / 50; // 0 pentru nivelurile 1–50, 1 pentru 51–100…
            var boss = new BossDefinition { Mega = mega };

            if (mega)
            {
                boss.Name = tier == 0 ? "Împăratul Zahărului" : tier == 1 ? "Regina Dulciurilor" : "Titanul de Caramel";
                boss.Look = 4 + Math.Min(2, tier);
                int every = Math.Max(2, 4 - tier);
                boss.Phases = new[]
                {
                    new BossPhase { FromHealth = 1f, Attacks = new[] { BossAttack.ChocolateSpread }, AttackEvery = every + 1, Strength = 2 + tier },
                    new BossPhase { FromHealth = 0.66f, Attacks = new[] { BossAttack.Freeze, BossAttack.Lock }, AttackEvery = every, Strength = 3 + tier },
                    new BossPhase { FromHealth = 0.33f, Attacks = new[] { BossAttack.ChocolateSpread, BossAttack.Freeze, BossAttack.Lock, BossAttack.Shuffle }, AttackEvery = every, Strength = 3 + tier },
                };
            }
            else
            {
                int character = (index - 1) % 5; // 0..3 = personajele, 4 = loc de Mega (sărit)
                if (character == 4) character = index % 4;
                boss.Look = character;
                boss.Name = character switch
                {
                    0 => "Regele Ciocolată",
                    1 => "Regina Gheții",
                    2 => "Dragonul de Bomboane",
                    _ => "Monstrul de Jeleu",
                };
                BossAttack[] attacks = character switch
                {
                    0 => new[] { BossAttack.ChocolateSpread },
                    1 => new[] { BossAttack.Freeze },
                    2 => new[] { BossAttack.Lock, BossAttack.ChocolateSpread },
                    _ => new[] { BossAttack.Shuffle, BossAttack.Freeze },
                };
                // După primul ciclu de personaje (nivelurile 60–90…), atacurile se combină cu ale altui personaj.
                if (level > 50)
                {
                    var combined = new List<BossAttack>(attacks);
                    foreach (BossAttack extra in character switch
                    {
                        0 => new[] { BossAttack.Freeze },
                        1 => new[] { BossAttack.Lock },
                        2 => new[] { BossAttack.Freeze },
                        _ => new[] { BossAttack.ChocolateSpread },
                    })
                        if (!combined.Contains(extra)) combined.Add(extra);
                    attacks = combined.ToArray();
                }
                boss.Phases = new[]
                {
                    new BossPhase { FromHealth = 1f, Attacks = attacks, AttackEvery = level > 50 ? 3 : 4, Strength = 2 + Math.Min(3, tier) },
                };
            }

            // Viața: cât face jucătorul în mod obișnuit în ~75% din mutări (vezi BalanceSimulator), deci câștigabil,
            // dar cu nevoie de grupuri mari și piese speciale ca să fie confortabil.
            boss.Health = (int)(moves * BossRules.ExpectedDamagePerMove * (mega ? 0.8f : 0.72f));
            return boss;
        }

        public BossPhase PhaseFor(float healthFraction)
        {
            BossPhase current = Phases[0];
            foreach (BossPhase phase in Phases)
                if (healthFraction <= phase.FromHealth) current = phase;
            return current;
        }
    }

    /// <summary>Daunele pe care le face jucătorul Boss-ului (aceleași reguli în joc și în simulator).</summary>
    public static class BossRules
    {
        public const int PerCandy = 10;
        public const int PerActivation = 20;
        public const int Combo = 120;
        /// <summary>Cât face în medie o mutare (măsurat cu jucătorul automat).</summary>
        public const float ExpectedDamagePerMove = 160f;

        /// <summary>Daunele unui pas: fiecare piesă eliminată o singură dată, bonus pentru grupuri mari și speciale.</summary>
        public static int DamageFor(BoardStep step)
        {
            int damage = 0;
            foreach (PieceInfo piece in step.Cleared)
                if (piece.Kind != TileKind.Crate && piece.Kind != TileKind.Chocolate) damage += PerCandy;
            if (step.GroupSize >= BlastRules.RocketMin) damage += (step.GroupSize - 4) * 15;
            damage += step.Activations.Count * PerActivation;
            if (step.Combo) damage += Combo;
            return damage;
        }
    }

    /// <summary>
    /// Starea unei lupte: viața rămasă, faza, numărătoarea până la următorul atac. Atacul vine doar după ce acțiunea
    /// jucătorului s-a terminat (EndOfMove) și e anunțat cu o mutare înainte (AttackWarning).
    /// </summary>
    public sealed class BossBattle
    {
        public readonly BossDefinition Definition;
        public int Health { get; private set; }
        public int PhaseIndex { get; private set; }
        public int MovesUntilAttack { get; private set; }
        public bool Defeated => Health <= 0;
        public float HealthFraction => Definition.Health > 0 ? (float)Health / Definition.Health : 0f;
        public BossPhase Phase => Definition.Phases[PhaseIndex];
        /// <summary>Atacul următor e la o mutare distanță (se arată avertismentul).</summary>
        public bool AttackWarning => !Defeated && MovesUntilAttack == 1;
        public BossAttack NextAttack => Phase.Attacks[_attackIndex % Phase.Attacks.Length];

        private int _attackIndex;

        public BossBattle(BossDefinition definition)
        {
            Definition = definition;
            Health = definition.Health;
            MovesUntilAttack = definition.Phases[0].AttackEvery;
        }

        /// <summary>Aplică daunele; întoarce true dacă s-a schimbat faza (Mega-Boss).</summary>
        public bool Damage(int amount)
        {
            if (amount <= 0 || Defeated) return false;
            Health = Math.Max(0, Health - amount);
            int phase = 0;
            for (int i = 0; i < Definition.Phases.Length; i++)
                if (HealthFraction <= Definition.Phases[i].FromHealth) phase = i;
            if (phase <= PhaseIndex) return false;
            PhaseIndex = phase;
            _attackIndex = 0;
            MovesUntilAttack = Math.Min(MovesUntilAttack, Phase.AttackEvery);
            return true;
        }

        /// <summary>
        /// La finalul unei mutări (după ce totul s-a liniștit): numără și, dacă e momentul, întoarce atacul
        /// (null = niciun atac acum). Un Boss învins nu mai atacă.
        /// </summary>
        public BossAttack? EndOfMove()
        {
            if (Defeated) return null;
            MovesUntilAttack--;
            if (MovesUntilAttack > 0) return null;
            BossAttack attack = NextAttack;
            _attackIndex++;
            MovesUntilAttack = Phase.AttackEvery;
            return attack;
        }
    }
}
