using System;
using System.Linq;
using Prism.Navigation;
using Prism.Mvvm;
using Prism.Common;
using Xamarin.Forms;
using Prism.Logging;
using Prism.Events;
using Prism.Services;
using DependencyService = Prism.Services.DependencyService;
using Prism.Modularity;
using Autofac;
using Autofac.Features.ResolveAnything;
using Prism.Autofac.Forms.Modularity;
using Prism.Autofac.Navigation;
using Prism.Autofac.Forms;
using Prism.AppModel;
using Prism.Autofac.Immutable;

namespace Prism.Autofac
{
    /// <summary>
    /// Application base class using Autofac
    /// </summary>
    public abstract class PrismApplication : PrismApplicationBase<IContainer>
    {
        private static bool _isContainerTypeSet = false;
        private static AutofacContainerType _containerType = AutofacContainerType.Mutable;

        /// <summary>
        /// Allows you to set the type of container that will be used by Prism.Autofac.Forms:
        ///  - Mutable is the traditional style of container that can be updated via ContainerBuilder.Update() (obsolete)
        ///  - Immutable is an updated style of container that cannot be updated after it is built (RECOMMENDED)
        /// Defaults to Mutable for backwards compatibility with Prism.Autofac.Forms v6.3.0 (and earlier).
        /// </summary>
        public static AutofacContainerType ContainerType
        {
            get => _containerType;
            set
            {
                if (_isContainerTypeSet)
                {
                    throw new InvalidOperationException("The ContainerType can only be set once; this should be done early in "
                        + "application initialization, before registering or resolving types.");
                }
                _containerType = value;
                _isContainerTypeSet = true;
            }
        }

        private IContainer mutableContainer;
        private AutofacContainer immutableContainer;

        private IApplicationProvider immutableApplicationProvider;
        private INavigationService initialNavigationService;
        /*
        private ILoggerFacade immutableLogger;
        private IModuleCatalog immutableModuleCatalog;
        private IApplicationProvider immutableApplicationProvider;
        private IModuleInitializer immutableModuleInitializer;
        private INavigationService immutableNavigationService;
        */

        /// <summary>
        /// Service key used when registering the <see cref="AutofacPageNavigationService"/> with the container
        /// </summary>
        const string _navigationServiceName = "AutofacPageNavigationService";

        /// <summary>
        /// Create a new instance of <see cref="PrismApplication"/>
        /// </summary>
        /// <param name="platformInitializer">Class to initialize platform instances</param>
        /// <remarks>
        /// The method <see cref="IPlatformInitializer.RegisterTypes(IContainer)"/> will be called after <see cref="PrismApplication.RegisterTypes()"/> 
        /// to allow for registering platform specific instances.
        /// </remarks>
        protected PrismApplication(IPlatformInitializer initializer = null)
            : base(initializer)
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            FinishContainerConfiguration();
        }

        protected override void ConfigureViewModelLocator()
        {
            ViewModelLocationProvider.SetDefaultViewModelFactory((view, type) =>
            {
                NamedParameter parameter = null;
                var page = view as Page;
                if (page != null)
                {
                    parameter = new NamedParameter("navigationService", CreateNavigationService(page));
                }

                return Container.Resolve(type, parameter);
            });
        }

        /// <summary>
        /// Create a default instance of <see cref="IContainer" />
        /// </summary>
        /// <returns>An instance of <see cref="IContainer" /></returns>
        protected override IContainer CreateContainer()
        {
            _isContainerTypeSet = true;
            //return new ContainerBuilder().Build();
            if (_containerType == AutofacContainerType.Mutable)
            {
                return (mutableContainer = mutableContainer ?? new ContainerBuilder().Build());
            }
            else if (_containerType == AutofacContainerType.Immutable)
            {
                return (immutableContainer = immutableContainer ?? new AutofacContainer());
            }
            else
            {
                throw new InvalidOperationException($"'{_containerType}' is an unknown container type.");
            }
        }

        protected override IModuleManager CreateModuleManager()
        {
            return Container.Resolve<IModuleManager>();
        }

        /// <summary>
        /// Create instance of <see cref="INavigationService"/>
        /// </summary>
        /// <remarks>
        /// The <see cref="_navigationServiceKey"/> is used as service key when resolving
        /// </remarks>
        /// <returns>Instance of <see cref="INavigationService"/></returns>
        protected override INavigationService CreateNavigationService()
        {
            if (_containerType == AutofacContainerType.Mutable)
            {
                return Container.ResolveNamed<INavigationService>(_navigationServiceName);
            }
            else if (_containerType == AutofacContainerType.Immutable && Container is AutofacContainer afContainer)
            {
                return (afContainer.IsContainerBuilt)
                    ? Container.ResolveNamed<INavigationService>(_navigationServiceName)
                    : initialNavigationService;
            }
            else
            {
                throw new InvalidOperationException($"'{_containerType}' is an unknown container type.");
            }           
        }

        protected override void InitializeModules()
        {
            if (ModuleCatalog.Modules.Any())
            {
                var manager = Container.Resolve<IModuleManager>();
                manager.Run();
            }
        }

        protected override void ConfigureContainer()
        {
            if (_containerType == AutofacContainerType.Mutable)
            {
                var builder = new ContainerBuilder();

                builder.RegisterInstance(Logger).As<ILoggerFacade>().SingleInstance();
                builder.RegisterInstance(ModuleCatalog).As<IModuleCatalog>().SingleInstance();

                builder.Register(ctx => new ApplicationProvider()).As<IApplicationProvider>().SingleInstance();
                builder.Register(ctx => new ApplicationStore()).As<IApplicationStore>().SingleInstance();
                builder.Register(ctx => new AutofacPageNavigationService(Container, Container.Resolve<IApplicationProvider>(), Container.Resolve<ILoggerFacade>())).Named<INavigationService>(_navigationServiceName);
                builder.Register(ctx => new ModuleManager(Container.Resolve<IModuleInitializer>(), Container.Resolve<IModuleCatalog>())).As<IModuleManager>().SingleInstance();
                builder.Register(ctx => new AutofacModuleInitializer(Container)).As<IModuleInitializer>().SingleInstance();
                builder.Register(ctx => new EventAggregator()).As<IEventAggregator>().SingleInstance();
                builder.Register(ctx => new DependencyService()).As<IDependencyService>().SingleInstance();
                builder.Register(ctx => new PageDialogService(ctx.Resolve<IApplicationProvider>())).As<IPageDialogService>().SingleInstance();
                builder.Register(ctx => new DeviceService()).As<IDeviceService>().SingleInstance();

                builder.Update(Container);
            }
            else if (_containerType == AutofacContainerType.Immutable)
            {
                immutableApplicationProvider = immutableApplicationProvider ?? new ApplicationProvider();
                initialNavigationService = initialNavigationService ??
                                             new AutofacPageNavigationService(null, immutableApplicationProvider, Logger);

                /*
                immutableLogger = immutableLogger ?? Logger;
                immutableModuleCatalog = immutableModuleCatalog ?? ModuleCatalog;
                immutableApplicationProvider = immutableApplicationProvider ?? new ApplicationProvider();
                immutableModuleInitializer = immutableModuleInitializer ?? new AutofacModuleInitializer(Container);
                immutableNavigationService = immutableNavigationService ??
                                             new AutofacPageNavigationService(Container, immutableApplicationProvider, immutableLogger);

                (Container as AutofacContainer)?.RegisterInstance(immutableLogger).As<ILoggerFacade>().SingleInstance();
                (Container as AutofacContainer)?.RegisterInstance(immutableModuleCatalog).As<IModuleCatalog>().SingleInstance();
                (Container as AutofacContainer)?.Register(ctx => immutableApplicationProvider).As<IApplicationProvider>().SingleInstance();
                (Container as AutofacContainer)?.Register(ctx => new ApplicationStore()).As<IApplicationStore>().SingleInstance();
                (Container as AutofacContainer)?.Register(ctx => immutableNavigationService).Named<INavigationService>(_navigationServiceName);
                (Container as AutofacContainer)?.Register(ctx => new ModuleManager(immutableModuleInitializer, immutableModuleCatalog)).As<IModuleManager>().SingleInstance();
                (Container as AutofacContainer)?.Register(ctx => immutableModuleInitializer).As<IModuleInitializer>().SingleInstance();
                (Container as AutofacContainer)?.Register(ctx => new EventAggregator()).As<IEventAggregator>().SingleInstance();
                (Container as AutofacContainer)?.Register(ctx => new DependencyService()).As<IDependencyService>().SingleInstance();
                (Container as AutofacContainer)?.Register(ctx => new PageDialogService(immutableApplicationProvider)).As<IPageDialogService>().SingleInstance();
                (Container as AutofacContainer)?.Register(ctx => new DeviceService()).As<IDeviceService>().SingleInstance();
                */
                (Container as AutofacContainer)?.RegisterInstance(Logger).As<ILoggerFacade>().SingleInstance();
                (Container as AutofacContainer)?.RegisterInstance(ModuleCatalog).As<IModuleCatalog>().SingleInstance();

                //(Container as AutofacContainer)?.Register(ctx => new ApplicationProvider()).As<IApplicationProvider>().SingleInstance();
                (Container as AutofacContainer)?.RegisterInstance(immutableApplicationProvider).As<IApplicationProvider>().SingleInstance();
                (Container as AutofacContainer)?.Register(ctx => new ApplicationStore()).As<IApplicationStore>().SingleInstance();
                //(Container as AutofacContainer)?.Register(ctx => new AutofacPageNavigationService(null, immutableApplicationProvider, Logger)).Named<INavigationService>(_navigationServiceName);
                (Container as AutofacContainer)?.Register(ctx => new AutofacPageNavigationService(Container, Container.Resolve<IApplicationProvider>(), Container.Resolve<ILoggerFacade>()))
                    .Named<INavigationService>(_navigationServiceName);
                (Container as AutofacContainer)?.Register(ctx => new ModuleManager(Container.Resolve<IModuleInitializer>(), Container.Resolve<IModuleCatalog>())).As<IModuleManager>().SingleInstance();
                (Container as AutofacContainer)?.Register(ctx => new AutofacModuleInitializer(Container)).As<IModuleInitializer>().SingleInstance();
                (Container as AutofacContainer)?.Register(ctx => new EventAggregator()).As<IEventAggregator>().SingleInstance();
                (Container as AutofacContainer)?.Register(ctx => new DependencyService()).As<IDependencyService>().SingleInstance();
                (Container as AutofacContainer)?.Register(ctx => new PageDialogService(ctx.Resolve<IApplicationProvider>())).As<IPageDialogService>().SingleInstance();
                (Container as AutofacContainer)?.Register(ctx => new DeviceService()).As<IDeviceService>().SingleInstance();
            }
            else
            {
                throw new InvalidOperationException($"'{_containerType}' is an unknown container type.");
            }
        }

        /// <summary>
        /// Finish the container's configuration after all other types are registered.
        /// </summary>
        private void FinishContainerConfiguration()
        {
            if (_containerType == AutofacContainerType.Mutable)
            {
                var containerUpdater = new ContainerBuilder();

                // Make sure any not specifically registered concrete type can resolve.
                containerUpdater.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
                containerUpdater.Update(Container);
            }
            else if (_containerType == AutofacContainerType.Immutable)
            {
                (Container as AutofacContainer)?.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
                (initialNavigationService as AutofacPageNavigationService)?.SetContainer(Container);
            }
            else
            {
                throw new InvalidOperationException($"'{_containerType}' is an unknown container type.");
            }
        }
    }
}
