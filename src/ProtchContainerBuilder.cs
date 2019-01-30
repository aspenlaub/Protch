using Aspenlaub.Net.GitHub.CSharp.Protch.Interfaces;
using Autofac;

namespace Aspenlaub.Net.GitHub.CSharp.Protch {
    public static class ProtchContainerBuilder {
        public static ContainerBuilder UseProtch(this ContainerBuilder builder) {
            builder.RegisterType<ProjectFactory>().As<IProjectFactory>();
            builder.RegisterType<ProjectLogic>().As<IProjectLogic>();
            return builder;
        }
    }
}
