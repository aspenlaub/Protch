using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Protch.Entities;
using Aspenlaub.Net.GitHub.CSharp.Protch.Interfaces;

namespace Aspenlaub.Net.GitHub.CSharp.Protch {
    public class ProjectFactory : IProjectFactory {
        protected XmlNamespaceManager NamespaceManager;

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

            var propertyCpGroups = document.XPathSelectElements("./cp:Project/cp:PropertyGroup", NamespaceManager).Select(x => ReadPropertyGroup(x, true)).ToList();
            var propertyGroups = propertyCpGroups.Any() ? propertyCpGroups : document.XPathSelectElements("./Project/PropertyGroup", NamespaceManager).Select(x => ReadPropertyGroup(x, false)).ToList();
            var cpDllFileFullNames = document.XPathSelectElements("./cp:Project/cp:ItemGroup/cp:Reference/cp:HintPath", NamespaceManager).Select(x => DllFileFullName(projectFileInfo.DirectoryName, x)).ToList();
            var dllFileFullNames = cpDllFileFullNames.Any() ? cpDllFileFullNames : document.XPathSelectElements("./Project/ItemGroup/Reference/HintPath", NamespaceManager).Select(x => DllFileFullName(projectFileInfo.DirectoryName, x)).ToList();
            var targetFrameworkCpElement = document.XPathSelectElements("./cp:Project/cp:PropertyGroup/cp:TargetFrameworkVersion", NamespaceManager).FirstOrDefault();
            var targetFrameworkElement = document.XPathSelectElements("./Project/PropertyGroup/TargetFramework", NamespaceManager).FirstOrDefault() ?? targetFrameworkCpElement;

            var project = new Project {
                ProjectFileFullName = projectFileFullName,
                ProjectName = ProjectName(solutionFileFullName, projectFileInfo),
                TargetFramework = targetFrameworkElement?.Value ?? "",
                RootNamespace = propertyGroups.FirstOrDefault(p => p.RootNamespace != "")?.RootNamespace ?? ""
            };

            foreach (var propertyGroup in propertyGroups) {
                project.PropertyGroups.Add(propertyGroup);
            }

            foreach(var dllFileFullName in dllFileFullNames) {
                project.ReferencedDllFiles.Add(dllFileFullName);
            }

            return project;
        }

        protected string ProjectName(string solutionFileFullName, FileInfo projectFileInfo) {
            var solutionFolder = solutionFileFullName.Substring(0, solutionFileFullName.LastIndexOf('\\'));
            foreach (var s in File.ReadAllLines(solutionFileFullName).ToList().Where(x => x.StartsWith("Project("))) {
                if (!ExtractProject(s, out var projectName, out var projectFile)) { continue; }

                var projectFullFileName = solutionFolder + '\\' + projectFile;
                if (!File.Exists(projectFullFileName)) { continue; }
                if (projectFullFileName != projectFileInfo.FullName) { continue; }

                return projectName;
            }

            return "";
        }

        protected bool ExtractProject(string s, out string projectName, out string projectFile) {
            projectName = projectFile = "";
            var pos = s.IndexOf("= \"", StringComparison.Ordinal);
            if (pos < 0) { return false; }

            s = s.Remove(0, 3 + pos);
            projectName = s.Substring(0, s.IndexOf("\"", StringComparison.Ordinal));
            pos = s.IndexOf(", \"", StringComparison.Ordinal);
            if (pos < 0) { return false; }

            s = s.Remove(0, 3 + s.IndexOf(", \"", StringComparison.Ordinal));
            projectFile = s.Substring(0, s.IndexOf("\"", StringComparison.Ordinal));
            return true;
        }

        protected IPropertyGroup ReadPropertyGroup(XElement propertyGroupElement, bool cp) {
            var namespaceSelector = cp ? "cp:" : "";
            var propertyGroup = new PropertyGroup {
                AssemblyName = propertyGroupElement?.XPathSelectElement(namespaceSelector + "AssemblyName", NamespaceManager)?.Value ?? "",
                RootNamespace = propertyGroupElement?.XPathSelectElement(namespaceSelector + "RootNamespace", NamespaceManager)?.Value ?? "",
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

        protected string DllFileFullName(string projectFolderFullName, XElement hintPathElement) {
            return Path.Combine(projectFolderFullName, hintPathElement.Value);
        }
    }
}
