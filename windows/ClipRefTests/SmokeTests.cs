using System.Reflection;
using ClipRef;

namespace ClipRefTests;

/// <summary>
/// Foundational smoke test for the scaffold: proves the xUnit harness runs and the
/// ClipRef app assembly is referenced and loadable from the test project. Real
/// behavioural tests (decide, uniqueURL, the settings store, prune) arrive with the
/// later Phase 1 port items — there is deliberately no behaviour to test here yet.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void AppAssemblyIsReferencedAndLoads()
    {
        // Referencing a type from the app project proves the ProjectReference is wired
        // and the assembly loads. (Requires [assembly: InternalsVisibleTo("ClipRefTests")]
        // in the app project, since TrayApplicationContext is internal.)
        Assembly appAssembly = typeof(TrayApplicationContext).Assembly;

        Assert.Equal("ClipRef", appAssembly.GetName().Name);
    }

    [Fact]
    public void TestHarnessRuns()
    {
        // A trivial assertion that fails only if the xUnit runner itself is misconfigured.
        Assert.True(true);
    }
}
