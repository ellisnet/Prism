using System;
using System.Linq;
using System.Reflection;
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
using Prism.Autofac.Forms.Immutable;

// ReSharper disable once CheckNamespace
namespace Prism.Autofac
{
    /// <summary>
    /// Application base class using Autofac
    /// </summary>
    public abstract class PrismApplication : PrismApplicationBase<IContainer>
    {
        /// <summary>
        /// Service key used when registering the <see cref="AutofacPageNavigationService"/> with the container
        /// </summary>
        // ReSharper disable once InconsistentNaming
        const string _navigationServiceName = "AutofacPageNavigationService";

        private IContainer _mutableContainer;
        private IAutofacContainer _immutableContainer;
        private IApplicationProvider _immutableApplicationProvider;
        private INavigationService _initialNavigationService;
        private bool _doModuleManagerRun;

        private static bool _isContainerTypeSet;
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

        /// <summary>
        /// Create a new instance of <see cref="PrismApplication"/>
        /// </summary>
        /// <param name="initializer">Class to initialize platform instances</param>
        /// <remarks>
        /// The method <see cref="IPlatformInitializer.RegisterTypes(IContainer)"/> will be called after <see cref="PrismApplication.RegisterTypes()"/> 
        /// to allow for registering platform specific instances.
        /// </remarks>
        protected PrismApplication(IPlatformInitializer initializer = null)
            : base(initializer)
        {
        }

#if TEST
        //Only want this constructor for testing - allows containerType to be set per PrismApplication instance
        protected PrismApplication(AutofacContainerType containerType, IPlatformInitializer initializer = null)
            : base(initializer)
        {
            _isContainerTypeSet = true;
            _containerType = containerType;
        }
#endif

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
                if (view is Page page)
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
            if (_containerType == AutofacContainerType.Mutable)
            {
                return (_mutableContainer = _mutableContainer ?? new ContainerBuilder().Build());
            }
            else if (_containerType == AutofacContainerType.Immutable)
            {
                return (_immutableContainer = _immutableContainer ?? new AutofacContainer());
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
            else if (_containerType == AutofacContainerType.Immutable && Container is IAutofacContainer afContainer)
            {
                return (afContainer.IsContainerBuilt)
                    ? Container.ResolveNamed<INavigationService>(_navigationServiceName)
                    : _initialNavigationService;
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
                if (_containerType == AutofacContainerType.Mutable)
                {
                    var manager = Container.Resolve<IModuleManager>();
                    manager.Run();
                }
                else if (_containerType == AutofacContainerType.Immutable)
                {
                    //In immutable mode, module initialization is moved to the FinishContainerConfiguration() method
                    _doModuleManagerRun = true;
                }
                else
                {
                    throw new InvalidOperationException($"'{_containerType}' is an unknown container type.");
                }          
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
                _immutableApplicationProvider = _immutableApplicationProvider ?? new ApplicationProvider();
                _initialNavigationService = _initialNavigationService ??
                                             new AutofacPageNavigationService(null, _immutableApplicationProvider, Logger);

                (Container as IAutofacContainer)?.RegisterInstance(Logger).As<ILoggerFacade>().SingleInstance();
                (Container as IAutofacContainer)?.RegisterInstance(ModuleCatalog).As<IModuleCatalog>().SingleInstance();
                (Container as IAutofacContainer)?.RegisterInstance(_immutableApplicationProvider).As<IApplicationProvider>().SingleInstance();
                (Container as IAutofacContainer)?.Register(ctx => new ApplicationStore()).As<IApplicationStore>().SingleInstance();
                (Container as IAutofacContainer)?.Register(ctx => new AutofacPageNavigationService(Container, Container.Resolve<IApplicationProvider>(), Container.Resolve<ILoggerFacade>()))
                    .Named<INavigationService>(_navigationServiceName);
                (Container as IAutofacContainer)?.Register(ctx => new AutofacModuleInitializer(Container)).As<IModuleInitializer>().SingleInstance();
                (Container as IAutofacContainer)?.Register(ctx => new ModuleManager(Container.Resolve<IModuleInitializer>(), Container.Resolve<IModuleCatalog>()))
                    .As<IModuleManager>().SingleInstance();
                (Container as IAutofacContainer)?.Register(ctx => new EventAggregator()).As<IEventAggregator>().SingleInstance();
                (Container as IAutofacContainer)?.Register(ctx => new DependencyService()).As<IDependencyService>().SingleInstance();
                (Container as IAutofacContainer)?.Register(ctx => new PageDialogService(ctx.Resolve<IApplicationProvider>())).As<IPageDialogService>().SingleInstance();
                (Container as IAutofacContainer)?.Register(ctx => new DeviceService()).As<IDeviceService>().SingleInstance();
                (Container as IAutofacContainer)?.RegisterInstance(Container).As<IContainer>().SingleInstance();
                (Container as IAutofacContainer)?.RegisterInstance(Container).As<IAutofacContainer>().SingleInstance();
                (Container as IAutofacContainer)?.Register(ctx => CreateNavigationService()).As<INavigationService>();
            }
            else
            {
                throw new InvalidOperationException($"'{_containerType}' is an unknown container type.");
            }
        }

        private void PreRegisterModuleTypes()
        {
            foreach (Type moduleType in ModuleCatalog
                .Modules
                .Select(s => s.ModuleType)
                .Where(w => w != null && w.GetTypeInfo().ImplementedInterfaces.Contains(typeof(IPreRegisterTypes)))
                .Distinct())
            {
                object instance = null;
                foreach (ConstructorInfo ctor in moduleType.GetTypeInfo().DeclaredConstructors)
                {
                    ParameterInfo[] ctorParams = ctor.GetParameters();
                    if (ctorParams == null || ctorParams.Length == 0)
                    {
                        instance = ctor.Invoke(new object[] { });
                    }
                    else if (ctorParams.Length == 1 && (ctorParams[0].ParameterType == typeof(IContainer) ||
                                                        ctorParams[0].ParameterType == typeof(IAutofacContainer)))
                    {
                        instance = ctor.Invoke(new object[] { (IAutofacContainer)Container });
                    }
                }
                if (instance == null)
                {
                    throw new InvalidOperationException(
                        $"Unable to execute RegisterTypes() on the '{moduleType.Name}' module because a compatible constructor could not be found.");
                }
                (instance as IPreRegisterTypes)?.RegisterTypes((IAutofacContainer)Container);
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
                if (_doModuleManagerRun)
                {
                    //Pre-registering any module types here - using reflection to create an instance of the module and run RegisterTypes() on it
                    //  because the container has not been built yet; so I can't use the container to give me an instance of the module.
                    PreRegisterModuleTypes();
                }

                (Container as IAutofacContainer)?.RegisterSource(new AnyConcreteTypeNotAlreadyRegisteredSource());
                (_initialNavigationService as AutofacPageNavigationService)?.SetContainer(Container);

                if (_doModuleManagerRun)
                {
                    //Finished registering things in the container, so the container can be built and modules initialized
                    Container.Resolve<IModuleManager>().Run();
                }
            }
            else
            {
                throw new InvalidOperationException($"'{_containerType}' is an unknown container type.");
            }
        }
    }
}
