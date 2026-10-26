using UnityEngine;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>
    /// Procedural stylised construction worker (helmet, safety vest, work trousers, boots, shovel) with
    /// procedural idle, walk and dig animation. Reusable for hired workers in later phases.
    /// </summary>
    public sealed class CharacterRig : MonoBehaviour
    {
        /// <summary>Normalised point of the dig swing where the blade hits the ground.</summary>
        public const float ImpactPhase = 0.55f;

        private struct DigKey
        {
            public float Time;
            public Vector3 ToolPosition;
            public Vector3 ToolEuler;
            public float TorsoLean;

            public DigKey(float time, Vector3 toolPosition, Vector3 toolEuler, float torsoLean)
            {
                Time = time;
                ToolPosition = toolPosition;
                ToolEuler = toolEuler;
                TorsoLean = torsoLean;
            }
        }

        private static readonly DigKey[] DigKeys =
        {
            new DigKey(0.00f, new Vector3(0f, 0.20f, 0.30f), new Vector3(-35f, 0f, 0f), 4f),
            new DigKey(0.40f, new Vector3(0f, 0.38f, 0.20f), new Vector3(-80f, 0f, 0f), -4f),
            new DigKey(ImpactPhase, new Vector3(0f, 0.02f, 0.42f), new Vector3(-22f, 0f, 0f), 22f),
            new DigKey(0.78f, new Vector3(0.05f, 0.30f, 0.34f), new Vector3(-62f, 0f, -22f), 8f),
            new DigKey(1.00f, new Vector3(0f, 0.20f, 0.30f), new Vector3(-35f, 0f, 0f), 4f)
        };

        private static readonly Vector3 CarryToolPosition = new Vector3(0.05f, 0.22f, 0.26f);
        private static readonly Vector3 CarryToolEuler = new Vector3(-30f, 0f, -8f);

        private Transform _body;
        private Transform _torso;
        private Transform _head;
        private Transform _legL;
        private Transform _legR;
        private Transform _armL;
        private Transform _armR;
        private Transform _tool;
        private float _walkPhase;
        private float _idleTime;
        private float _digBlend;

        /// <summary>0 = standing still, 1 = full walking speed.</summary>
        public float MoveSpeed01 { get; set; }
        public bool Digging { get; set; }
        /// <summary>0..1 position inside the current dig swing.</summary>
        public float DigPhase { get; set; }

        public static CharacterRig Build(Transform parent, MaterialLibrary materials)
        {
            var root = new GameObject("WorkerRig");
            root.transform.SetParent(parent, false);
            CharacterRig rig = root.AddComponent<CharacterRig>();
            rig.Construct(materials);
            return rig;
        }

        private void Construct(MaterialLibrary m)
        {
            Material vest = m.Get(Palette.Vest);
            Material stripe = m.Get(Palette.Reflective);
            Material shirt = m.Get(Palette.Shirt);
            Material pants = m.Get(Palette.Pants);
            Material boots = m.Get(Palette.Boots);
            Material skin = m.Get(Palette.Skin);
            Material helmet = m.Get(Palette.Yellow);
            Material dark = m.Get(Palette.Black);

            _body = PrimitiveFactory.Pivot("Body", transform, Vector3.zero);

            _legL = BuildLeg("LegL", -0.11f, pants, boots);
            _legR = BuildLeg("LegR", 0.11f, pants, boots);

            _torso = PrimitiveFactory.Pivot("Torso", _body, new Vector3(0f, 0.85f, 0f));
            PrimitiveFactory.Box("Shirt", _torso, new Vector3(0f, 0.30f, 0f), new Vector3(0.40f, 0.58f, 0.24f), shirt);
            PrimitiveFactory.Box("Vest", _torso, new Vector3(0f, 0.27f, 0f), new Vector3(0.43f, 0.46f, 0.27f), vest);
            PrimitiveFactory.Box("StripeLow", _torso, new Vector3(0f, 0.15f, 0f), new Vector3(0.44f, 0.045f, 0.28f), stripe, false);
            PrimitiveFactory.Box("StripeHigh", _torso, new Vector3(0f, 0.33f, 0f), new Vector3(0.44f, 0.045f, 0.28f), stripe, false);
            PrimitiveFactory.Box("Belt", _torso, new Vector3(0f, 0.03f, 0f), new Vector3(0.42f, 0.06f, 0.26f), dark, false);

            _head = PrimitiveFactory.Pivot("Head", _torso, new Vector3(0f, 0.62f, 0f));
            PrimitiveFactory.Create("Neck", PrimitiveType.Cylinder, _head, new Vector3(0f, -0.02f, 0f), new Vector3(0.12f, 0.05f, 0.12f), skin, false);
            PrimitiveFactory.Create("Face", PrimitiveType.Sphere, _head, new Vector3(0f, 0.14f, 0f), new Vector3(0.30f, 0.30f, 0.30f), skin);
            PrimitiveFactory.Create("EyeL", PrimitiveType.Sphere, _head, new Vector3(-0.06f, 0.13f, 0.135f), Vector3.one * 0.04f, dark, false);
            PrimitiveFactory.Create("EyeR", PrimitiveType.Sphere, _head, new Vector3(0.06f, 0.13f, 0.135f), Vector3.one * 0.04f, dark, false);
            PrimitiveFactory.Create("Helmet", PrimitiveType.Sphere, _head, new Vector3(0f, 0.235f, -0.01f), new Vector3(0.34f, 0.22f, 0.34f), helmet);
            PrimitiveFactory.Create("HelmetBrim", PrimitiveType.Cylinder, _head, new Vector3(0f, 0.17f, 0.035f), new Vector3(0.40f, 0.012f, 0.42f), helmet);
            PrimitiveFactory.Box("HelmetRidge", _head, new Vector3(0f, 0.33f, -0.01f), new Vector3(0.05f, 0.04f, 0.30f), helmet, false);

            _armL = BuildArm("ArmL", -0.27f, shirt, skin);
            _armR = BuildArm("ArmR", 0.27f, shirt, skin);

            _tool = PrimitiveFactory.Pivot("ToolSocket", _torso, CarryToolPosition);
            BuildShovel(_tool, m);

            // Soft contact shadow so the character stays grounded even when real-time shadows are off.
            PrimitiveFactory.Create("ContactShadow", PrimitiveType.Cylinder, transform, new Vector3(0f, 0.01f, 0f), new Vector3(0.6f, 0.005f, 0.6f),
                m.Get(new Color(0.18f, 0.14f, 0.11f)), false);
        }

        private Transform BuildLeg(string name, float x, Material pants, Material boots)
        {
            Transform hip = PrimitiveFactory.Pivot(name, _body, new Vector3(x, 0.85f, 0f));
            PrimitiveFactory.Box("Leg", hip, new Vector3(0f, -0.38f, 0f), new Vector3(0.17f, 0.76f, 0.19f), pants);
            PrimitiveFactory.Box("Boot", hip, new Vector3(0f, -0.79f, 0.04f), new Vector3(0.19f, 0.13f, 0.29f), boots);
            return hip;
        }

        private Transform BuildArm(string name, float x, Material sleeve, Material skin)
        {
            Transform shoulder = PrimitiveFactory.Pivot(name, _torso, new Vector3(x, 0.52f, 0f));
            PrimitiveFactory.Box("Arm", shoulder, new Vector3(0f, -0.24f, 0f), new Vector3(0.12f, 0.48f, 0.13f), sleeve);
            PrimitiveFactory.Create("Hand", PrimitiveType.Sphere, shoulder, new Vector3(0f, -0.51f, 0f), Vector3.one * 0.12f, skin);
            return shoulder;
        }

        private static void BuildShovel(Transform socket, MaterialLibrary m)
        {
            Material wood = m.Get(Palette.Wood);
            Material steel = m.Get(Palette.Steel);
            Material grip = m.Get(Palette.Black);
            PrimitiveFactory.Create("Handle", PrimitiveType.Cylinder, socket, new Vector3(0f, -0.22f, 0f), new Vector3(0.05f, 0.56f, 0.05f), wood);
            PrimitiveFactory.Box("Grip", socket, new Vector3(0f, 0.34f, 0f), new Vector3(0.16f, 0.045f, 0.045f), grip);
            PrimitiveFactory.Box("Collar", socket, new Vector3(0f, -0.76f, 0f), new Vector3(0.07f, 0.08f, 0.07f), steel);
            PrimitiveFactory.Box("Blade", socket, new Vector3(0f, -0.92f, 0.01f), new Vector3(0.24f, 0.28f, 0.03f), steel);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            _idleTime += dt;
            _digBlend = Mathf.MoveTowards(_digBlend, Digging ? 1f : 0f, dt * 8f);
            float walk = Mathf.Clamp01(MoveSpeed01);
            _walkPhase += dt * Mathf.Lerp(0f, 11f, walk);

            float swing = Mathf.Sin(_walkPhase);
            float legSwing = swing * 35f * walk;
            _legL.localRotation = Quaternion.Euler(legSwing, 0f, 0f);
            _legR.localRotation = Quaternion.Euler(-legSwing, 0f, 0f);

            float bob = Mathf.Abs(Mathf.Cos(_walkPhase)) * 0.05f * walk;
            float breathe = Mathf.Sin(_idleTime * 2.2f) * 0.008f * (1f - walk);
            _body.localPosition = new Vector3(0f, bob + breathe, 0f);

            SampleDig(Mathf.Repeat(DigPhase, 1f), out Vector3 digPos, out Vector3 digEuler, out float digLean);

            float carryBounce = swing * 4f * walk;
            Vector3 toolPos = Vector3.Lerp(CarryToolPosition, digPos, _digBlend);
            Vector3 toolEuler = Vector3.Lerp(CarryToolEuler + new Vector3(carryBounce, 0f, 0f), digEuler, _digBlend);
            _tool.localPosition = toolPos;
            _tool.localRotation = Quaternion.Euler(toolEuler);

            _torso.localRotation = Quaternion.Euler(Mathf.Lerp(3f * walk, digLean, _digBlend), 0f, 0f);
            _head.localRotation = Quaternion.Euler(Mathf.Lerp(0f, -digLean * 0.4f, _digBlend), 0f, 0f);

            // Both hands follow the shovel while digging; arms swing naturally while walking.
            float armDig = toolEuler.x - 15f;
            _armL.localRotation = Quaternion.Euler(Mathf.Lerp(-legSwing * 0.7f - 20f * walk, armDig, _digBlend), 0f, Mathf.Lerp(-4f, 12f, _digBlend));
            _armR.localRotation = Quaternion.Euler(Mathf.Lerp(-35f, armDig + 10f, _digBlend), 0f, Mathf.Lerp(8f, -12f, _digBlend));
        }

        private static void SampleDig(float phase, out Vector3 position, out Vector3 euler, out float lean)
        {
            for (int i = 0; i < DigKeys.Length - 1; i++)
            {
                DigKey a = DigKeys[i];
                DigKey b = DigKeys[i + 1];
                if (phase > b.Time) continue;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a.Time, b.Time, phase));
                position = Vector3.Lerp(a.ToolPosition, b.ToolPosition, t);
                euler = Vector3.Lerp(a.ToolEuler, b.ToolEuler, t);
                lean = Mathf.Lerp(a.TorsoLean, b.TorsoLean, t);
                return;
            }
            DigKey last = DigKeys[DigKeys.Length - 1];
            position = last.ToolPosition;
            euler = last.ToolEuler;
            lean = last.TorsoLean;
        }
    }
}
