using UnityEngine;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>Lightweight looping motion for set dressing (spinning, swaying, driving along a line).</summary>
    public sealed class AmbientMotion : MonoBehaviour
    {
        private enum Mode { Spin, Oscillate, Drive }

        private Mode _mode;
        private Vector3 _axis;
        private float _amount;
        private float _speed;
        private float _phase;
        private Quaternion _baseRotation;
        private Vector3 _from;
        private Vector3 _to;

        /// <summary>Continuous rotation around a local axis in degrees per second.</summary>
        public AmbientMotion Spin(Vector3 axis, float degreesPerSecond)
        {
            _mode = Mode.Spin;
            _axis = axis;
            _speed = degreesPerSecond;
            return this;
        }

        /// <summary>Sine sway around a local axis.</summary>
        public AmbientMotion Oscillate(Vector3 axis, float amplitudeDegrees, float frequency, float phase = 0f)
        {
            _mode = Mode.Oscillate;
            _axis = axis;
            _amount = amplitudeDegrees;
            _speed = frequency;
            _phase = phase;
            _baseRotation = transform.localRotation;
            return this;
        }

        /// <summary>Moves from one local point to another at constant speed, then wraps around.</summary>
        public AmbientMotion Drive(Vector3 from, Vector3 to, float speed, float startOffset01 = 0f)
        {
            _mode = Mode.Drive;
            _from = from;
            _to = to;
            _speed = speed;
            _phase = startOffset01;
            Vector3 dir = to - from;
            if (dir.sqrMagnitude > 0.0001f) transform.localRotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            return this;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            switch (_mode)
            {
                case Mode.Spin:
                    transform.Rotate(_axis, _speed * dt, Space.Self);
                    break;
                case Mode.Oscillate:
                    _phase += dt * _speed * Mathf.PI * 2f;
                    transform.localRotation = _baseRotation * Quaternion.AngleAxis(Mathf.Sin(_phase) * _amount, _axis);
                    break;
                default:
                    float length = Vector3.Distance(_from, _to);
                    if (length < 0.001f) return;
                    _phase = Mathf.Repeat(_phase + _speed * dt / length, 1f);
                    transform.localPosition = Vector3.Lerp(_from, _to, _phase);
                    break;
            }
        }
    }
}
