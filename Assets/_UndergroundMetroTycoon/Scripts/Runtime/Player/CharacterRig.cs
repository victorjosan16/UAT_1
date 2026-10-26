using System;
using UnityEngine;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>
    /// Procedural stylised construction worker (helmet with lamp, reflective vest, gloves, blue work trousers,
    /// boots, shovel) with procedural animation: idle breathing, distance-synced walking (no foot sliding),
    /// turn lean, digging with a soil clump on the blade that is tossed aside, a quick "collect" dip and a
    /// celebration. Placeholder for a rigged, skinned character; reusable for hired workers.
    /// </summary>
    public sealed class CharacterRig : MonoBehaviour
    {
        /// <summary>Normalised point of the dig swing where the blade hits the ground.</summary>
        public const float ImpactPhase = 0.55f;
        private const float TossPhase = 0.84f;
        private const float StrideLength = 0.9f;
        private const float CelebrateDuration = 1.5f;
        private const float CollectDuration = 0.35f;

        private struct DigKey
        {
            public float Time;
            public Vector3 ToolPosition;
            public Vector3 ToolEuler;
            public float TorsoLean;
            public float Crouch;

            public DigKey(float time, Vector3 toolPosition, Vector3 toolEuler, float torsoLean, float crouch)
            {
                Time = time;
                ToolPosition = toolPosition;
                ToolEuler = toolEuler;
                TorsoLean = torsoLean;
                Crouch = crouch;
            }
        }

        // Keys were solved so the blade tip touches the surface (y ~ -0.05) ahead of the feet at impact and stays
        // above ground everywhere else. World blade pitch = tool pitch + torso lean.
        private static readonly DigKey[] DigKeys =
        {
            new DigKey(0.00f, new Vector3(0f, 0.32f, 0.28f), new Vector3(-45f, 0f, 0f), 4f, 0f),
            new DigKey(0.40f, new Vector3(0f, 0.50f, 0.15f), new Vector3(-69f, 0f, 0f), -6f, 0f),
            new DigKey(ImpactPhase, new Vector3(0f, 0.45f, 0.35f), new Vector3(-43f, 0f, 0f), 15f, 0.06f),
            new DigKey(0.72f, new Vector3(0.02f, 0.42f, 0.33f), new Vector3(-60f, 0f, -6f), 10f, 0.04f),
            new DigKey(TossPhase, new Vector3(0.12f, 0.50f, 0.30f), new Vector3(-74f, 0f, -38f), 4f, 0f),
            new DigKey(1.00f, new Vector3(0f, 0.32f, 0.28f), new Vector3(-45f, 0f, 0f), 4f, 0f)
        };

        private static readonly Vector3 CarryToolPosition = new Vector3(0.06f, 0.34f, 0.25f);
        private static readonly Vector3 CarryToolEuler = new Vector3(-50f, 0f, -10f);

        private Transform _body;
        private Transform _hips;
        private Transform _torso;
        private Transform _head;
        private Transform _legL;
        private Transform _legR;
        private Transform _armL;
        private Transform _armR;
        private Transform _tool;
        private Transform _bladeSoil;
        private MeshRenderer _bladeSoilRenderer;
        private float _walkPhase;
        private float _idleTime;
        private float _digBlend;
        private float _walkBlend;
        private float _lean;
        private float _celebrate;
        private float _collect;
        private float _lastDigPhase;

        /// <summary>0 = standing still, 1 = full walking speed.</summary>
        public float MoveSpeed01 { get; set; }
        /// <summary>Ground distance covered this frame; drives the step cycle so feet don't slide.</summary>
        public float MoveDistance { get; set; }
        /// <summary>Signed yaw speed in degrees per second, used to lean into turns.</summary>
        public float TurnRate { get; set; }
        public bool Digging { get; set; }
        /// <summary>0..1 position inside the current dig swing.</summary>
        public float DigPhase { get; set; }
        /// <summary>When true the rig advances its own dig cycle (ambient NPC workers).</summary>
        public bool AutoDig { get; set; }

        /// <summary>Raised when the swing tosses its soil clump, with the blade's world position.</summary>
        public event Action<Vector3> DirtTossed;

        public static CharacterRig Build(Transform parent, MaterialLibrary materials)
        {
            var root = new GameObject("WorkerRig");
            root.transform.SetParent(parent, false);
            CharacterRig rig = root.AddComponent<CharacterRig>();
            rig.Construct(materials);
            return rig;
        }

        /// <summary>Colour of the soil carried on the blade for the next scoop.</summary>
        public void SetBladeSoil(Material material)
        {
            if (_bladeSoilRenderer != null && material != null) _bladeSoilRenderer.sharedMaterial = material;
        }

        public void Celebrate() => _celebrate = CelebrateDuration;

        public void PlayCollect()
        {
            if (_celebrate <= 0f) _collect = CollectDuration;
        }

        private void Construct(MaterialLibrary m)
        {
            Material vest = m.Get(Palette.Vest);
            Material stripe = m.Get(Palette.Reflective);
            Material shirt = m.Get(Palette.Shirt);
            Material pants = m.Get(Palette.WorkBlue);
            Material boots = m.Get(Palette.Boots);
            Material sole = m.Get(Palette.Hex(0x2A1E16));
            Material skin = m.Get(Palette.Skin);
            Material helmet = m.Get(Palette.Yellow);
            Material helmetDark = m.Get(Palette.Hex(0xE6A400));
            Material dark = m.Get(Palette.Hex(0x23242B));
            Material white = m.Get(Palette.White);
            Material glove = m.Get(Palette.Hex(0xF2D16B));
            Material cheek = m.Get(Palette.Hex(0xF29A8A));

            _body = PrimitiveFactory.Pivot("Body", transform, Vector3.zero);
            _hips = PrimitiveFactory.Pivot("Hips", _body, new Vector3(0f, 0.62f, 0f));
            _legL = BuildLeg("LegL", -0.11f, pants, boots, sole);
            _legR = BuildLeg("LegR", 0.11f, pants, boots, sole);
            PrimitiveFactory.Box("Seat", _hips, new Vector3(0f, -0.02f, 0f), new Vector3(0.38f, 0.16f, 0.24f), pants);

            _torso = PrimitiveFactory.Pivot("Torso", _hips, new Vector3(0f, 0.04f, 0f));
            PrimitiveFactory.Box("Shirt", _torso, new Vector3(0f, 0.27f, 0f), new Vector3(0.4f, 0.52f, 0.25f), shirt);
            PrimitiveFactory.Box("Vest", _torso, new Vector3(0f, 0.25f, 0f), new Vector3(0.44f, 0.44f, 0.28f), vest);
            PrimitiveFactory.Box("StripeLow", _torso, new Vector3(0f, 0.13f, 0f), new Vector3(0.45f, 0.045f, 0.29f), stripe, false);
            PrimitiveFactory.Box("StripeHigh", _torso, new Vector3(0f, 0.31f, 0f), new Vector3(0.45f, 0.045f, 0.29f), stripe, false);
            PrimitiveFactory.Box("StrapL", _torso, new Vector3(-0.1f, 0.4f, 0.141f), new Vector3(0.05f, 0.22f, 0.01f), stripe, false);
            PrimitiveFactory.Box("StrapR", _torso, new Vector3(0.1f, 0.4f, 0.141f), new Vector3(0.05f, 0.22f, 0.01f), stripe, false);
            PrimitiveFactory.Box("Pocket", _torso, new Vector3(0.11f, 0.22f, 0.145f), new Vector3(0.1f, 0.08f, 0.01f), m.Get(Palette.Hex(0xE5661A)), false);
            PrimitiveFactory.Box("Belt", _torso, new Vector3(0f, 0.02f, 0f), new Vector3(0.42f, 0.06f, 0.27f), dark, false);
            PrimitiveFactory.Box("Buckle", _torso, new Vector3(0f, 0.02f, 0.14f), new Vector3(0.07f, 0.05f, 0.01f), helmetDark, false);

            _head = PrimitiveFactory.Pivot("Head", _torso, new Vector3(0f, 0.54f, 0f));
            PrimitiveFactory.Create("Neck", PrimitiveType.Cylinder, _head, new Vector3(0f, 0f, 0f), new Vector3(0.13f, 0.05f, 0.13f), skin, false);
            PrimitiveFactory.Create("Face", PrimitiveType.Sphere, _head, new Vector3(0f, 0.2f, 0f), new Vector3(0.4f, 0.38f, 0.38f), skin);
            PrimitiveFactory.Create("EarL", PrimitiveType.Sphere, _head, new Vector3(-0.2f, 0.19f, 0f), Vector3.one * 0.08f, skin, false);
            PrimitiveFactory.Create("EarR", PrimitiveType.Sphere, _head, new Vector3(0.2f, 0.19f, 0f), Vector3.one * 0.08f, skin, false);
            foreach (float side in new[] { -1f, 1f })
            {
                PrimitiveFactory.Create("EyeWhite", PrimitiveType.Sphere, _head, new Vector3(side * 0.075f, 0.21f, 0.165f), new Vector3(0.075f, 0.085f, 0.04f), white, false);
                PrimitiveFactory.Create("Pupil", PrimitiveType.Sphere, _head, new Vector3(side * 0.075f, 0.205f, 0.185f), new Vector3(0.045f, 0.055f, 0.02f), dark, false);
                PrimitiveFactory.Box("Brow", _head, new Vector3(side * 0.08f, 0.27f, 0.17f), new Vector3(0.08f, 0.018f, 0.02f), m.Get(Palette.Hex(0x5A3A22)), false)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, side * -8f);
                PrimitiveFactory.Create("Cheek", PrimitiveType.Sphere, _head, new Vector3(side * 0.12f, 0.14f, 0.15f), new Vector3(0.06f, 0.035f, 0.02f), cheek, false);
            }
            PrimitiveFactory.Create("Nose", PrimitiveType.Sphere, _head, new Vector3(0f, 0.16f, 0.19f), new Vector3(0.05f, 0.045f, 0.05f), m.Get(Palette.Hex(0xE8AE72)), false);
            PrimitiveFactory.Box("Smile", _head, new Vector3(0f, 0.105f, 0.178f), new Vector3(0.08f, 0.018f, 0.02f), dark, false);
            PrimitiveFactory.Box("SmileL", _head, new Vector3(-0.045f, 0.112f, 0.175f), new Vector3(0.025f, 0.018f, 0.02f), dark, false).transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
            PrimitiveFactory.Box("SmileR", _head, new Vector3(0.045f, 0.112f, 0.175f), new Vector3(0.025f, 0.018f, 0.02f), dark, false).transform.localRotation = Quaternion.Euler(0f, 0f, 35f);

            PrimitiveFactory.Create("Helmet", PrimitiveType.Sphere, _head, new Vector3(0f, 0.3f, -0.01f), new Vector3(0.44f, 0.3f, 0.44f), helmet);
            PrimitiveFactory.Create("Brim", PrimitiveType.Cylinder, _head, new Vector3(0f, 0.25f, 0.04f), new Vector3(0.5f, 0.014f, 0.52f), helmet);
            PrimitiveFactory.Box("Ridge", _head, new Vector3(0f, 0.43f, -0.01f), new Vector3(0.06f, 0.05f, 0.38f), helmetDark, false);
            PrimitiveFactory.Box("LampBase", _head, new Vector3(0f, 0.36f, 0.21f), new Vector3(0.09f, 0.07f, 0.04f), dark, false);
            PrimitiveFactory.Create("Lamp", PrimitiveType.Sphere, _head, new Vector3(0f, 0.36f, 0.235f), new Vector3(0.06f, 0.05f, 0.02f), m.Get(Palette.Hex(0xFFF4C2)), false);

            _armL = BuildArm("ArmL", -0.27f, shirt, glove);
            _armR = BuildArm("ArmR", 0.27f, shirt, glove);

            _tool = PrimitiveFactory.Pivot("ToolSocket", _torso, CarryToolPosition);
            BuildShovel(m);

            // Soft contact shadow keeps the character grounded when real-time shadows are off.
            PrimitiveFactory.Create("ContactShadow", PrimitiveType.Cylinder, transform, new Vector3(0f, 0.012f, 0f), new Vector3(0.62f, 0.004f, 0.62f),
                m.Get(new Color(0.42f, 0.32f, 0.22f)), false);
        }

        private Transform BuildLeg(string name, float x, Material pants, Material boots, Material sole)
        {
            Transform hip = PrimitiveFactory.Pivot(name, _hips, new Vector3(x, 0f, 0f));
            PrimitiveFactory.Box("Leg", hip, new Vector3(0f, -0.27f, 0f), new Vector3(0.17f, 0.52f, 0.19f), pants);
            PrimitiveFactory.Box("Cuff", hip, new Vector3(0f, -0.5f, 0f), new Vector3(0.18f, 0.05f, 0.2f), pants, false);
            PrimitiveFactory.Box("Boot", hip, new Vector3(0f, -0.55f, 0.04f), new Vector3(0.19f, 0.12f, 0.29f), boots);
            PrimitiveFactory.Box("Sole", hip, new Vector3(0f, -0.605f, 0.045f), new Vector3(0.2f, 0.03f, 0.3f), sole, false);
            return hip;
        }

        private Transform BuildArm(string name, float x, Material sleeve, Material glove)
        {
            Transform shoulder = PrimitiveFactory.Pivot(name, _torso, new Vector3(x, 0.47f, 0f));
            PrimitiveFactory.Create("Shoulder", PrimitiveType.Sphere, shoulder, Vector3.zero, Vector3.one * 0.14f, sleeve);
            PrimitiveFactory.Box("Arm", shoulder, new Vector3(0f, -0.2f, 0f), new Vector3(0.12f, 0.4f, 0.13f), sleeve);
            PrimitiveFactory.Create("Glove", PrimitiveType.Sphere, shoulder, new Vector3(0f, -0.43f, 0f), new Vector3(0.13f, 0.13f, 0.13f), glove);
            return shoulder;
        }

        private void BuildShovel(MaterialLibrary m)
        {
            Material wood = m.Get(Palette.Wood);
            Material steel = m.Get(Palette.Steel);
            Material steelDark = m.Get(Palette.Hex(0x7E8B9C));
            Material grip = m.Get(Palette.Hex(0x23324D));
            PrimitiveFactory.Create("Handle", PrimitiveType.Cylinder, _tool, new Vector3(0f, -0.22f, 0f), new Vector3(0.05f, 0.56f, 0.05f), wood);
            PrimitiveFactory.Box("GripBar", _tool, new Vector3(0f, 0.36f, 0f), new Vector3(0.17f, 0.045f, 0.045f), grip);
            PrimitiveFactory.Box("GripL", _tool, new Vector3(-0.07f, 0.32f, 0f), new Vector3(0.035f, 0.09f, 0.035f), grip);
            PrimitiveFactory.Box("GripR", _tool, new Vector3(0.07f, 0.32f, 0f), new Vector3(0.035f, 0.09f, 0.035f), grip);
            PrimitiveFactory.Box("Collar", _tool, new Vector3(0f, -0.77f, 0f), new Vector3(0.075f, 0.1f, 0.075f), steelDark);
            PrimitiveFactory.Box("Blade", _tool, new Vector3(0f, -0.94f, 0.015f), new Vector3(0.25f, 0.3f, 0.03f), steel);
            PrimitiveFactory.Box("BladeTip", _tool, new Vector3(0f, -1.1f, 0.02f), new Vector3(0.16f, 0.05f, 0.03f), steel);

            _bladeSoil = PrimitiveFactory.Create("Soil", PrimitiveType.Sphere, _tool, new Vector3(0f, -0.95f, 0.07f), new Vector3(0.22f, 0.2f, 0.12f),
                m.Get(Palette.TerrainTop(Simulation.TerrainMaterial.SoftSoil)), false).transform;
            _bladeSoilRenderer = _bladeSoil.GetComponent<MeshRenderer>();
            _bladeSoil.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            _idleTime += dt;
            if (AutoDig) DigPhase = Mathf.Repeat(DigPhase + dt * 1.05f, 1f);

            _digBlend = Mathf.MoveTowards(_digBlend, Digging ? 1f : 0f, dt * 8f);
            _walkBlend = Mathf.MoveTowards(_walkBlend, Mathf.Clamp01(MoveSpeed01), dt * 6f);
            _walkPhase += MoveDistance / StrideLength * Mathf.PI;
            _lean = Mathf.Lerp(_lean, Mathf.Clamp(-TurnRate * 0.025f, -12f, 12f), dt * 8f);

            float walk = _walkBlend;
            float swing = Mathf.Sin(_walkPhase);
            float legSwing = swing * 38f * walk;
            float bob = Mathf.Abs(Mathf.Cos(_walkPhase)) * 0.045f * walk;
            float breathe = Mathf.Sin(_idleTime * 2.2f) * 0.01f * (1f - walk);

            float digPhase = Mathf.Repeat(DigPhase, 1f);
            SampleDig(digPhase, out Vector3 digPos, out Vector3 digEuler, out float digLean, out float crouch);
            UpdateBladeSoil(digPhase);

            // Collect: a quick dip of the body.
            float collect = 0f;
            if (_collect > 0f)
            {
                _collect -= dt;
                collect = Mathf.Sin(Mathf.Clamp01(1f - _collect / CollectDuration) * Mathf.PI);
            }

            // Celebrate: two hops with arms up.
            float celebrate = 0f, hop = 0f;
            if (_celebrate > 0f)
            {
                _celebrate -= dt;
                float t = 1f - _celebrate / CelebrateDuration;
                celebrate = Mathf.Clamp01(Mathf.Min(t * 6f, (1f - t) * 6f));
                hop = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3f)) * 0.22f * celebrate;
            }

            float dig = _digBlend * (1f - celebrate);
            float knee = (crouch * dig + collect * 0.08f) * 1.0f;
            _body.localPosition = new Vector3(0f, bob + breathe + hop - knee, 0f);
            _body.localRotation = Quaternion.Euler(0f, 0f, _lean);

            _legL.localRotation = Quaternion.Euler(legSwing - knee * 120f, 0f, 0f);
            _legR.localRotation = Quaternion.Euler(-legSwing - knee * 120f, 0f, 0f);

            Vector3 carryEuler = CarryToolEuler + new Vector3(swing * 4f * walk, 0f, 0f);
            Vector3 toolPos = Vector3.Lerp(CarryToolPosition, digPos, dig);
            Vector3 toolEuler = Vector3.Lerp(carryEuler, digEuler, dig);
            // Celebration lifts the shovel overhead.
            toolPos = Vector3.Lerp(toolPos, new Vector3(0.28f, 0.75f, 0.05f), celebrate);
            toolEuler = Vector3.Lerp(toolEuler, new Vector3(0f, 0f, -160f), celebrate);
            _tool.localPosition = toolPos;
            _tool.localRotation = Quaternion.Euler(toolEuler);

            float torsoLean = Mathf.Lerp(3f * walk + collect * 25f, digLean, dig);
            _torso.localRotation = Quaternion.Euler(Mathf.Lerp(torsoLean, -6f, celebrate), Mathf.Sin(_walkPhase) * 6f * walk, 0f);
            _head.localRotation = Quaternion.Euler(Mathf.Lerp(-digLean * 0.4f * dig + collect * 10f, -12f, celebrate), 0f, 0f);

            float armDig = toolEuler.x - 15f;
            float wave = Mathf.Sin(_idleTime * 14f) * 20f * celebrate;
            Quaternion armL = Quaternion.Euler(Mathf.Lerp(-legSwing * 0.8f - 10f * walk, armDig, dig), 0f, Mathf.Lerp(-6f, 14f, dig));
            Quaternion armR = Quaternion.Euler(Mathf.Lerp(legSwing * 0.8f - 30f, armDig + 12f, dig), 0f, Mathf.Lerp(8f, -14f, dig));
            _armL.localRotation = Quaternion.Slerp(armL, Quaternion.Euler(0f, 0f, -160f + wave), celebrate);
            _armR.localRotation = Quaternion.Slerp(armR, Quaternion.Euler(0f, 0f, 150f - wave), celebrate);

            MoveDistance = 0f;
        }

        private void UpdateBladeSoil(float phase)
        {
            bool carrying = Digging && _digBlend > 0.9f && phase >= ImpactPhase && phase < TossPhase;
            if (_bladeSoil.gameObject.activeSelf != carrying) _bladeSoil.gameObject.SetActive(carrying);

            bool crossedToss = Digging && _lastDigPhase < TossPhase && phase >= TossPhase && _lastDigPhase <= phase;
            if (crossedToss) DirtTossed?.Invoke(_bladeSoil.position);
            _lastDigPhase = phase;
        }

        private static void SampleDig(float phase, out Vector3 position, out Vector3 euler, out float lean, out float crouch)
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
                crouch = Mathf.Lerp(a.Crouch, b.Crouch, t);
                return;
            }
            DigKey last = DigKeys[DigKeys.Length - 1];
            position = last.ToolPosition;
            euler = last.ToolEuler;
            lean = last.TorsoLean;
            crouch = last.Crouch;
        }
    }
}
