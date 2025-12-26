using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Protch.Entities;
using Aspenlaub.Net.GitHub.CSharp.Protch.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Protch;

public class ProjectFactory : IProjectFactory {
    protected XmlNamespaceManager NamespaceManager;

    private const string _startTag = "<Project Path=\"";

    public ProjectFactory() {
        NamespaceManager = new XmlNamespaceManager(new NameTable());
        NamespaceManager.AddNamespace("cp", XmlNamespaces.CsProjNamespaceUri);
    }

    public IProject Load(string solutionFileFullName, string projectFileFullName, IErrorsAndInfos errorsAndInfos) {
        if (!File.Exists(solutionFileFullName)) {
            errorsAndInfos.Errors.Add(string.Format(Properties.Resources.FileNotFound, solutionFileFullName));
            return null;
        }

        var projectFileInfo = new FileInfo(projectFileFullName);
        if (projectFileInfo.Directory == null) {
            errorsAndInfos.Errors.Add(string.Format(Properties.Resources.FileNotFound, projectFileFullName));
            return null;
        }

        XDocument document;
        try {
            document = XDocument.Load(projectFileFullName);
        } catch {
            errorsAndInfos.Errors.Add(string.Format(Properties.Resources.InvalidXmlFile, projectFileFullName));
            return null;
        }

        List<IPropertyGroup> propertyCpGroups = [..
            document.XPathSelectElements("./cp:Project/cp:PropertyGroup", NamespaceManager).Select(x => ReadPropertyGroup(x, true))
        ];
        List<IPropertyGroup> propertyGroups = [..
            propertyCpGroups.Count != 0 ? propertyCpGroups : document.XPathSelectElements("./Project/PropertyGroup", NamespaceManager).Select(x => ReadPropertyGroup(x, false))
        ];
        List<string> cpDllFileFullNames = [..
            document.XPathSelectElements("./cp:Project/cp:ItemGroup/cp:Reference/cp:HintPath", NamespaceManager).Select(x => DllFileFullName(projectFileInfo.DirectoryName, x))
        ];
        List<string> dllFileFullNames = [..
            cpDllFileFullNames.Count != 0 ? cpDllFileFullNames : document.XPathSelectElements("./Project/ItemGroup/Reference/HintPath", NamespaceManager).Select(x => DllFileFullName(projectFileInfo.DirectoryName, x))
        ];
        XElement targetFrameworkCpElement = document.XPathSelectElements("./cp:Project/cp:PropertyGroup/cp:TargetFrameworkVersion", NamespaceManager).FirstOrDefault();
        XElement targetFrameworkElement = document.XPathSelectElements("./Project/PropertyGroup/TargetFramework", NamespaceManager).FirstOrDefault() ?? targetFrameworkCpElement;
        IEnumerable<PackageReference> packageReferences = document.XPathSelectElements("//PackageReference")
              .Where(r => !string.IsNullOrEmpty(r.Attribute("Include")?.Value) && !string.IsNullOrEmpty(r.Attribute("Version")?.Value))
              .Select(r => new PackageReference { Id = r.Attribute("Include")?.Value, Version = r.Attribute("Version")?.Value });

        var project = new Project {
            ProjectFileFullName = projectFileFullName,
            ProjectName = ProjectName(solutionFileFullName, projectFileInfo),
            TargetFramework = targetFrameworkElement?.Value ?? "",
            RootNamespace = propertyGroups.FirstOrDefault(p => p.RootNamespace != "")?.RootNamespace ?? "",
            RepositoryType = propertyGroups.FirstOrDefault(p => p.RepositoryType != "")?.RepositoryType ?? "",
            RepositoryUrl = propertyGroups.FirstOrDefault(p => p.RepositoryType != "")?.RepositoryUrl ?? "",
            RepositoryBranch = propertyGroups.FirstOrDefault(p => p.RepositoryType != "")?.RepositoryBranch ?? "",
            PackageId = propertyGroups.FirstOrDefault(p => p.PackageId != "")?.PackageId ?? ""
        };

        foreach (IPropertyGroup propertyGroup in propertyGroups) {
            project.PropertyGroups.Add(propertyGroup);
        }

        foreach(string dllFileFullName in dllFileFullNames) {
            project.ReferencedDllFiles.Add(dllFileFullName);
        }

        foreach (PackageReference packageReference in packageReferences) {
            project.PackageReferences.Add(packageReference);
        }

        return project;
    }

    protected static string ProjectName(string solutionFileFullName, FileInfo projectFileInfo) {
        string solutionFolder = solutionFileFullName.Substring(0, solutionFileFullName.LastIndexOf('\\'));
        if (solutionFileFullName.EndsWith(".sln")) {
            foreach (string s in File.ReadAllLines(solutionFileFullName).ToList().Where(x => x.StartsWith("Project("))) {
                if (!ExtractProjectLegacy(s, out string projectName, out string projectFile)) { continue; }

                string projectFullFileName = solutionFolder + '\\' + projectFile;
                if (!File.Exists(projectFullFileName)) { continue; }
                if (projectFullFileName != projectFileInfo.FullName) { continue; }

                return projectName;
            }
        } else {
            foreach (string s in File.ReadAllLines(solutionFileFullName)
                    .ToList()
                    .Select(x => x.Trim())
                    .Where(x => x.StartsWith(_startTag))) {
                if (!ExtractProject(s, out string projectName, out string projectFile)) { continue; }

                string projectFullFileName = solutionFolder + '\\' + projectFile;
                if (!File.Exists(projectFullFileName)) { continue; }
                if (projectFullFileName != projectFileInfo.FullName) { continue; }

                return projectName;
            }
        }

        return "";
    }

    protected static bool ExtractProjectLegacy(string s, out string projectName, out string projectFile) {
        projectName = projectFile = "";
        int pos = s.IndexOf("= \"", StringComparison.Ordinal);
        if (pos < 0) { return false; }

        s = s.Remove(0, 3 + pos);
        projectName = s.Substring(0, s.IndexOf('"'));
        pos = s.IndexOf(", \"", StringComparison.Ordinal);
        if (pos < 0) { return false; }

        s = s.Remove(0, 3 + s.IndexOf(", \"", StringComparison.Ordinal));
        projectFile = s.Substring(0, s.IndexOf('"'));
        return true;
    }

    protected static bool ExtractProject(string s, out string projectName, out string projectFile) {
        projectName = projectFile = "";
        int pos = s.IndexOf(_startTag, StringComparison.Ordinal);
        if (pos < 0) { return false; }

        s = s.Substring(pos + _startTag.Length);
        projectFile = s.Substring(0, s.IndexOf('"'));
        projectName = projectFile.Replace(".csproj", "");
        pos = projectName.IndexOf('/');
        if (pos < 0) { return true; }

        projectName = projectName.Substring(pos + 1);
        projectFile = projectFile.Replace('/', '\\');
        return true;
    }

    protected IPropertyGroup ReadPropertyGroup(XElement propertyGroupElement, bool cp) {
        string namespaceSelector = cp ? "cp:" : "";
        var propertyGroup = new PropertyGroup {
            AssemblyName = propertyGroupElement?.XPathSelectElement(namespaceSelector + "AssemblyName", NamespaceManager)?.Value ?? "",
            RootNamespace = propertyGroupElement?.XPathSelectElement(namespaceSelector + "RootNamespace", NamespaceManager)?.Value ?? "",
            RepositoryType = propertyGroupElement?.XPathSelectElement(namespaceSelector + "RepositoryType", NamespaceManager)?.Value ?? "",
            RepositoryUrl = propertyGroupElement?.XPathSelectElement(namespaceSelector + "RepositoryUrl", NamespaceManager)?.Value ?? "",
            RepositoryBranch = propertyGroupElement?.XPathSelectElement(namespaceSelector + "RepositoryBranch", NamespaceManager)?.Value ?? "",
            PackageId = propertyGroupElement?.XPathSelectElement(namespaceSelector + "PackageId", NamespaceManager)?.Value ?? "",
            OutputPath = propertyGroupElement?.XPathSelectElement(namespaceSelector + "OutputPath", NamespaceManager)?.Value ?? "",
            IntermediateOutputPath = propertyGroupElement?.XPathSelectElement(namespaceSelector + "IntermediateOutputPath", NamespaceManager)?.Value ?? "",
            UseVsHostingProcess = propertyGroupElement?.XPathSelectElement(namespaceSelector + "UseVSHostingProcess", NamespaceManager)?.Value ?? "",
            GenerateBuildInfoConfigFile = propertyGroupElement?.XPathSelectElement(namespaceSelector + "GenerateBuildInfoConfigFile", NamespaceManager)?.Value ?? "",
            Condition = propertyGroupElement?.Attribute("Condition")?.Value ?? "",
            AppendTargetFrameworkToOutputPath = propertyGroupElement?.XPathSelectElement(namespaceSelector + "AppendTargetFrameworkToOutputPath", NamespaceManager)?.Value ?? "",
            AllowUnsafeBlocks = propertyGroupElement?.XPathSelectElement(namespaceSelector + "AllowUnsafeBlocks", NamespaceManager)?.Value ?? "",
            NuspecFile = propertyGroupElement?.XPathSelectElement(namespaceSelector + "NuspecFile", NamespaceManager)?.Value ?? "",
            Deterministic = propertyGroupElement?.XPathSelectElement(namespaceSelector + "Deterministic", NamespaceManager)?.Value ?? "",
            GenerateAssemblyInfo = propertyGroupElement?.XPathSelectElement(namespaceSelector + "GenerateAssemblyInfo", NamespaceManager)?.Value ?? "",
        };
        return propertyGroup;
    }

    protected static string DllFileFullName(string projectFolderFullName, XElement hintPathElement) {
        return Path.Combine(projectFolderFullName, hintPathElement.Value);
    }
}