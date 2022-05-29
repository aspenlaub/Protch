using System.Collections.Generic;
using Aspenlaub.Net.GitHub.CSharp.Protch.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Protch.Entities;

public class Project : IProject {
    public string ProjectFileFullName { get; set; }
    public string ProjectName { get; set; }
    public string TargetFramework { get; set; }
    public string RootNamespace { get; set; }
    public string RepositoryType { get; set; }
    public string RepositoryUrl { get; set; }
    public string RepositoryBranch { get; set; }
    public string PackageId { get; set; }

    public IList<IPropertyGroup> PropertyGroups { get; }

    public IList<string> ReferencedDllFiles { get; }

    public Project() {
        PropertyGroups = new List<IPropertyGroup>();
        ReferencedDllFiles = new List<string>();
    }
}