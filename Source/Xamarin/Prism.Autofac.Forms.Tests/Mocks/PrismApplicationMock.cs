using Prism.DI.Forms.Tests.Mocks.Modules;
using Prism.DI.Forms.Tests.Mocks.Services;
using Prism.DI.Forms.Tests.Mocks.ViewModels;
using Prism.DI.Forms.Tests.Mocks.Views;
using Prism.Modularity;
using Prism.Navigation;
using Xamarin.Forms;
using Autofac;

namespace Prism.Autofac.Forms.Tests.Mocks
{
    public class PrismApplicationMock : PrismApplication
    {
        public PrismApplicationMock(AutofacContainerType containerType) : base(containerType, null)
        { }

        public PrismApplicationMock(Page startPage, AutofacContainerType containerType) : this(containerType)
        {
            Current.MainPage = startPage;
        }

        public new INavigationService NavigationService => base.NavigationService;

        public bool Initialized { get; private set; }

        protected override void OnInitialized()
        {
            Initialized = true;
        }

        protected override void ConfigureModuleCatalog()
        {
            ModuleCatalog.AddModule(new ModuleInfo
            {
                InitializationMode = InitializationMode.WhenAvailable,
                ModuleName = "ModuleMock",
                ModuleType = typeof(ModuleMock)
            });
        }

        protected override void RegisterTypes()
        {
            FormsDependencyService.Register<IDependencyServiceMock>(new DependencyServiceMock());

            if (ContainerType == AutofacContainerType.Mutable)
            {
                var builder = new ContainerBuilder();

                builder.RegisterType<ServiceMock>().As<IServiceMock>();
                builder.RegisterType<AutowireViewModel>();
                builder.RegisterType<ViewModelAMock>();
                builder.Register(ctx => new ViewModelBMock()).Named<ViewModelBMock>(ViewModelBMock.Key);
                builder.RegisterType<ConstructorArgumentViewModel>();
                builder.RegisterType<ModuleMock>().SingleInstance();
                builder.Register(ctx => FormsDependencyService.Get<IDependencyServiceMock>())
                    .As<IDependencyServiceMock>();

                builder.Update(Container);
            }
            else if (ContainerType == AutofacContainerType.Immutable)
            {
                (Container as IAutofacContainer)?.RegisterType<ServiceMock>().As<IServiceMock>();
                (Container as IAutofacContainer)?.RegisterType<AutowireViewModel>();
                (Container as IAutofacContainer)?.RegisterType<ViewModelAMock>();
                (Container as IAutofacContainer)?.Register(ctx => new ViewModelBMock()).Named<ViewModelBMock>(ViewModelBMock.Key);
                (Container as IAutofacContainer)?.RegisterType<ConstructorArgumentViewModel>();
                (Container as IAutofacContainer)?.RegisterType<ModuleMock>().SingleInstance();
                (Container as IAutofacContainer)?.Register(ctx => FormsDependencyService.Get<IDependencyServiceMock>())
                    .As<IDependencyServiceMock>();
            }

            Container.RegisterTypeForNavigation<ViewMock>("view");
            Container.RegisterTypeForNavigation<ViewAMock, ViewModelAMock>();
            Container.RegisterTypeForNavigation<AutowireView, AutowireViewModel>();
            Container.RegisterTypeForNavigation<ConstructorArgumentView, ConstructorArgumentViewModel>();
        }

        public INavigationService CreateNavigationServiceForPage()
        {
            return CreateNavigationService();
        }
    }
}