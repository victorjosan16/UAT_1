// Compile-check-only stand-ins for Unity Test Framework attributes (the real ones ship with com.unity.test-framework).
namespace UnityEngine.TestTools
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class UnityTestAttribute : System.Attribute { }

    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class UnitySetUpAttribute : System.Attribute { }
}
