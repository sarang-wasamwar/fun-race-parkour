// TestAssert.cs
// Simple assertion framework for editor-based unit tests.
// Throws AssertionException on failure, tracks test pass/fail counts.

#if UNITY_EDITOR
using System;
using UnityEngine;

namespace FunRaceParkour.Tests
{
    public class AssertionException : Exception
    {
        public AssertionException(string message) : base(message) { }
    }

    public static class TestAssert
    {
        public static void AreEqual<T>(T expected, T actual, string message = "")
        {
            if (!object.Equals(expected, actual))
            {
                throw new AssertionException($"Expected: <{expected}> but was: <{actual}>. {message}");
            }
        }

        public static void AreApproximatelyEqual(float expected, float actual, float tolerance = 0.001f, string message = "")
        {
            if (Mathf.Abs(expected - actual) > tolerance)
            {
                throw new AssertionException($"Expected: ~{expected} (+/-{tolerance}) but was: {actual}. {message}");
            }
        }

        public static void IsTrue(bool condition, string message = "")
        {
            if (!condition)
            {
                throw new AssertionException($"Expected true but was false. {message}");
            }
        }

        public static void IsFalse(bool condition, string message = "")
        {
            if (condition)
            {
                throw new AssertionException($"Expected false but was true. {message}");
            }
        }

        public static void IsNotNull(object obj, string message = "")
        {
            if (obj == null)
            {
                throw new AssertionException($"Expected non-null object. {message}");
            }
        }

        public static void Greater(float val1, float val2, string message = "")
        {
            if (val1 <= val2)
            {
                throw new AssertionException($"Expected {val1} > {val2}. {message}");
            }
        }

        public static void Less(float val1, float val2, string message = "")
        {
            if (val1 >= val2)
            {
                throw new AssertionException($"Expected {val1} < {val2}. {message}");
            }
        }
    }
}
#endif
