using Aspenlaub.Net.GitHub.CSharp.Protch.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Protch.Entities;

public class PackageReference : IPackageReference {
    public string Id { get; init; }
    public string Version { get; init; }
}