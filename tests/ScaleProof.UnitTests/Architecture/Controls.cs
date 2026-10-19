namespace ScaleProof.UnitTests.Architecture.Controls.Violating
{
    // Test-only negative control: no production assembly depends on this type.
    internal sealed class Consumer
    {
        public HttpClient? Client { get; init; }
    }
}

namespace ScaleProof.UnitTests.Architecture.Controls.Allowed
{
    internal sealed class Consumer
    {
        public string? Name { get; init; }
    }
}
