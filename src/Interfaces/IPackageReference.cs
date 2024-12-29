namespace Aspenlaub.Net.GitHub.CSharp.Protch.Interfaces;

// ReSharper disable UnusedMemberInSuper.Global

public interface IPackageReference {
    string Id { get; init; }
    string Version { get; init; }
}