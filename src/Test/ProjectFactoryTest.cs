using System.IO;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Gitty;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Components;
using Aspenlaub.Net.GitHub.CSharp.Gitty.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Gitty.TestUtilities;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Extensions;
using Aspenlaub.Net.GitHub.CSharp.Protch.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Skladasu.Entities;
using Autofac;
using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using IContainer = Autofac.IContainer;

[assembly: DoNotParallelize]
namespace Aspenlaub.Net.GitHub.CSharp.Protch.Test;

[TestClass]
public class ProjectFactoryTest {
    private static readonly TestTargetFolder _pakledConsumerTarget = new(nameof(ProjectFactoryTest), "PakledConsumer");
    private static readonly TestTargetFolder _chabTarget = new(nameof(ProjectFactoryTest), "Chab");
    private static IContainer _container;

    [ClassInitialize]
    public static void ClassInitialize(TestContext _) {
        _container = new ContainerBuilder().UseGittyAndPegh("Protch").UseGittyTestUtilities().UseProtch().Build();
    }

    [TestInitialize]
    public void Initialize() {
        _pakledConsumerTarget.Delete();
        _chabTarget.Delete();
    }

    [TestCleanup]
    public void TestCleanup() {
        _pakledConsumerTarget.Delete();
        _chabTarget.Delete();
    }

    [TestMethod]
    public void CanLoadPakledConsumerProject() {
        var gitUtilities = new GitUtilities();
        var errorsAndInfos = new ErrorsAndInfos();
        const string url = "https://github.com/aspenlaub/PakledConsumer.git";
        gitUtilities.Clone(url, "master", _pakledConsumerTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        gitUtilities.Pull(_pakledConsumerTarget.Folder(), "UserName", "user.name@aspenlaub.org");

        string solutionFileFullName = _pakledConsumerTarget.Folder().SubFolder("src").FullName + @"\" + _pakledConsumerTarget.SolutionId + ".slnx";
        string projectFileFullName = _pakledConsumerTarget.Folder().SubFolder("src").FullName + @"\" + _pakledConsumerTarget.SolutionId + ".csproj";
        Assert.IsTrue(File.Exists(projectFileFullName));
        IProjectFactory sut = _container.Resolve<IProjectFactory>();
        IProject project = sut.Load(solutionFileFullName, projectFileFullName, errorsAndInfos);
        IProjectLogic projectLogic = _container.Resolve<IProjectLogic>();
        Assert.IsFalse(projectLogic.TargetsOldFramework(project));
        Assert.IsTrue(projectLogic.DoAllConfigurationsHaveNuspecs(project));
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        Assert.IsNotNull(project);
        Assert.AreEqual(projectFileFullName, project.ProjectFileFullName);
        Assert.AreEqual((object) _pakledConsumerTarget.SolutionId, project.ProjectName);
        Assert.AreEqual("net10.0", project.TargetFramework);
        Assert.HasCount(3, project.PropertyGroups);
        Assert.AreEqual("git", project.RepositoryType);
        Assert.AreEqual(url, project.RepositoryUrl);
        Assert.AreEqual("master", project.RepositoryBranch);
        Assert.AreEqual("PakledConsumer", project.PackageId);
        string rootNamespace = "";
        foreach (IPropertyGroup propertyGroup in project.PropertyGroups) {
            Assert.IsNotNull(propertyGroup);
            Assert.AreEqual((object) propertyGroup.AssemblyName, propertyGroup.RootNamespace);
            if (propertyGroup.Condition == "") {
                rootNamespace = propertyGroup.RootNamespace;
                Assert.StartsWith("Aspenlaub.Net.GitHub.CSharp." + _pakledConsumerTarget.SolutionId, propertyGroup.AssemblyName, $"Unexpected assembly name \"{propertyGroup.AssemblyName}\"");
                Assert.AreEqual("", propertyGroup.UseVsHostingProcess);
                Assert.AreEqual("false", propertyGroup.GenerateBuildInfoConfigFile);
                Assert.AreEqual("", propertyGroup.IntermediateOutputPath);
                Assert.AreEqual("", propertyGroup.OutputPath);
                Assert.AreEqual("false", propertyGroup.AppendTargetFrameworkToOutputPath);
                Assert.AreEqual("", propertyGroup.AllowUnsafeBlocks);
                Assert.AreEqual("", propertyGroup.NuspecFile);
                Assert.AreEqual("false", propertyGroup.Deterministic);
                Assert.AreEqual("", propertyGroup.GenerateAssemblyInfo);
            } else {
                Assert.AreEqual("", propertyGroup.AssemblyName);
                Assert.AreEqual(propertyGroup.Condition.Contains("Debug|") ? "" : "PakledConsumer.nuspec", propertyGroup.NuspecFile);
                Assert.AreEqual("", propertyGroup.UseVsHostingProcess);
                Assert.AreEqual("", propertyGroup.OutputPath);
                Assert.AreEqual("", propertyGroup.GenerateBuildInfoConfigFile);
                Assert.AreEqual("", propertyGroup.AppendTargetFrameworkToOutputPath);
                Assert.AreEqual("", propertyGroup.AllowUnsafeBlocks);
                Assert.AreEqual("", propertyGroup.Deterministic);
                Assert.AreEqual("", propertyGroup.GenerateAssemblyInfo);
            }
        }

        Assert.IsEmpty(project.ReferencedDllFiles);

        Assert.AreEqual(rootNamespace, project.RootNamespace);

        Assert.HasCount(1, project.PackageReferences);
        Assert.AreEqual("Pakled", project.PackageReferences[0].Id);

        projectFileFullName = _pakledConsumerTarget.Folder().SubFolder("src").FullName + @"\Test\" + _pakledConsumerTarget.SolutionId + ".Test.csproj";
        Assert.IsTrue(File.Exists(projectFileFullName));
        project = sut.Load(solutionFileFullName, projectFileFullName, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        Assert.IsNotNull(project);
        Assert.AreEqual(projectFileFullName, project.ProjectFileFullName);
        Assert.AreEqual(_pakledConsumerTarget.SolutionId + ".Test", project.ProjectName);
    }

    [TestMethod]
    public void CanLoadChabProject() {
        var gitUtilities = new GitUtilities();
        var errorsAndInfos = new ErrorsAndInfos();
        const string url = "https://github.com/aspenlaub/Chab.git";
        gitUtilities.Clone(url, "master", _chabTarget.Folder(), new CloneOptions { BranchName = "master" }, true, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        gitUtilities.Pull(_chabTarget.Folder(), "UserName", "user.name@aspenlaub.org");

        string solutionFileFullName = _chabTarget.Folder().SubFolder("src").FullName + @"\" + _chabTarget.SolutionId + ".slnx";
        string projectFileFullName = _chabTarget.Folder().SubFolder("src").FullName + @"\" + _chabTarget.SolutionId + ".csproj";
        Assert.IsTrue(File.Exists(projectFileFullName));
        IProjectFactory sut = _container.Resolve<IProjectFactory>();
        IProject project = sut.Load(solutionFileFullName, projectFileFullName, errorsAndInfos);
        IProjectLogic projectLogic = _container.Resolve<IProjectLogic>();
        Assert.IsFalse(projectLogic.TargetsOldFramework(project));
        Assert.IsTrue(projectLogic.DoAllConfigurationsHaveNuspecs(project));
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        Assert.IsNotNull(project);
        Assert.AreEqual(projectFileFullName, project.ProjectFileFullName);
        Assert.AreEqual((object) _chabTarget.SolutionId, project.ProjectName);
        Assert.AreEqual("net10.0", project.TargetFramework);
        Assert.HasCount(3, project.PropertyGroups);
        Assert.AreEqual("git", project.RepositoryType);
        Assert.AreEqual(url, project.RepositoryUrl);
        Assert.AreEqual("master", project.RepositoryBranch);
        Assert.AreEqual("Chab", project.PackageId);
        string rootNamespace = "";
        foreach (IPropertyGroup propertyGroup in project.PropertyGroups) {
            Assert.IsNotNull(propertyGroup);
            Assert.AreEqual((object) propertyGroup.AssemblyName, propertyGroup.RootNamespace);
            if (propertyGroup.Condition == "") {
                rootNamespace = propertyGroup.RootNamespace;
                Assert.StartsWith("Aspenlaub.Net.GitHub.CSharp." + _chabTarget.SolutionId, propertyGroup.AssemblyName, $"Unexpected assembly name \"{propertyGroup.AssemblyName}\"");
                Assert.AreEqual("", propertyGroup.UseVsHostingProcess);
                Assert.AreEqual("false", propertyGroup.GenerateBuildInfoConfigFile);
                Assert.AreEqual("", propertyGroup.IntermediateOutputPath);
                Assert.AreEqual("", propertyGroup.OutputPath);
                Assert.AreEqual("false", propertyGroup.AppendTargetFrameworkToOutputPath);
                Assert.AreEqual("", propertyGroup.AllowUnsafeBlocks);
                Assert.AreEqual("", propertyGroup.NuspecFile);
                Assert.AreEqual("false", propertyGroup.Deterministic);
                Assert.AreEqual("", propertyGroup.GenerateAssemblyInfo);
            } else {
                Assert.AreEqual("", propertyGroup.AssemblyName);
                if (propertyGroup.Condition.Contains("Debug|")) {
                    Assert.AreEqual("", propertyGroup.UseVsHostingProcess);
                    Assert.AreEqual("", propertyGroup.OutputPath);
                    Assert.AreEqual("", propertyGroup.AppendTargetFrameworkToOutputPath);
                    Assert.AreEqual("", propertyGroup.AllowUnsafeBlocks);
                    Assert.AreEqual("", propertyGroup.NuspecFile);
                } else {
                    Assert.AreEqual("", propertyGroup.UseVsHostingProcess);
                    Assert.AreEqual("", propertyGroup.OutputPath);
                    Assert.AreEqual("", propertyGroup.AppendTargetFrameworkToOutputPath);
                    Assert.AreEqual("", propertyGroup.AllowUnsafeBlocks);
                    Assert.AreEqual("Chab.nuspec", propertyGroup.NuspecFile);
                }
                Assert.AreEqual("", propertyGroup.GenerateBuildInfoConfigFile);
                Assert.AreEqual("", propertyGroup.Deterministic);
                Assert.AreEqual("", propertyGroup.GenerateAssemblyInfo);
            }
        }

        Assert.IsEmpty(project.ReferencedDllFiles);

        Assert.AreEqual(rootNamespace, project.RootNamespace);

        Assert.HasCount(1, project.PackageReferences);
        Assert.AreEqual("Autofac", project.PackageReferences[0].Id);

        projectFileFullName = _chabTarget.Folder().SubFolder("src").FullName + @"\Test\" + _chabTarget.SolutionId + ".Test.csproj";
        Assert.IsTrue(File.Exists(projectFileFullName));
        project = sut.Load(solutionFileFullName, projectFileFullName, errorsAndInfos);
        Assert.IsFalse(errorsAndInfos.Errors.Any(), errorsAndInfos.ErrorsPlusRelevantInfos());
        Assert.IsNotNull(project);
        Assert.AreEqual(projectFileFullName, project.ProjectFileFullName);
        Assert.AreEqual(_chabTarget.SolutionId + ".Test", project.ProjectName);
    }
}