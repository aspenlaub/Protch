using Aspenlaub.Net.GitHub.CSharp.Protch.Interfaces;
using Autofac;
using Microsoft.Extensions.DependencyInjection;

namespace Aspenlaub.Net.GitHub.CSharp.Protch {
    public static class ProtchContainerBuilder {
        public static ContainerBuilder UseProtch(this ContainerBuilder builder) {
            builder.RegisterType<ProjectFactory>().As<IProjectFactory>();
            builder.RegisterType<ProjectLogic>().As<IProjectLogic>();
            return builder;
        }
        // ReSharper disable once UnusedMember.Global
        public static IServiceCollection UseProtch(this IServiceCollection services) {
            services.AddTransient<IProjectFactory, ProjectFactory>();
            services.AddTransient<IProjectLogic, ProjectLogic>();
            return services;
        }
    }
}
