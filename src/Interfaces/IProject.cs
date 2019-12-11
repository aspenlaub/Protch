using System.Collections.Generic;

namespace Aspenlaub.Net.GitHub.CSharp.Protch.Interfaces {
    public interface IProject {
        string ProjectFileFullName { get; set; }
        string ProjectName { get; set; }
        string TargetFramework { get; set;  }
        string RootNamespace { get; set; }
        string RepositoryType { get; set; }
        string RepositoryUrl { get; set; }
        string RepositoryBranch { get; set; }

        IList<string> ReferencedDllFiles { get; }
        IList<IPropertyGroup> PropertyGroups { get; }
    }
}
