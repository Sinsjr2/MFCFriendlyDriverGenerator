using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace NUnit.Framework {

    [System.Diagnostics.DebuggerStepThrough]
    [ContractVerification(false)]
    public static class Assertion {

        public static void Is<T>(this T? actual, T? expected) {

            if (typeof(T) != typeof(string) && typeof(IEnumerable).IsAssignableFrom(typeof(T))) {
                CollectionAssert.AreEquivalent(expected as IEnumerable, actual as IEnumerable,
@$"actual: {(actual is null ? "" : string.Join("", actual as IEnumerable) )}
expected: {(expected is null ? "null" : string.Join("", expected as IEnumerable))}");
                return;
            }

            if (actual is null && expected is null) {
                Assert.Pass();
                return;
            }


            if (actual is null || expected is null || !EqualityComparer<T>.Default.Equals(actual, expected)) {
                Assert.Fail(
@$"actual: {actual?.ToString() ?? "null"}
expected: {expected?.ToString() ?? "null"}");
            }
        }
    }
}
