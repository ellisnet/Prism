using System;
using System.Threading.Tasks;
using Prism.Common;
using Prism.Autofac.Forms.Tests.Mocks;
using Prism.DI.Forms.Tests.Mocks.Modules;
using Prism.DI.Forms.Tests.Mocks.Services;
using Prism.DI.Forms.Tests.Mocks.ViewModels;
using Prism.DI.Forms.Tests.Mocks.Views;
using Prism.Autofac.Navigation;
using Prism.Navigation;
using Xamarin.Forms;
using Xunit;
using Autofac;
using Prism.DI.Forms.Tests;
#if TEST
using Application = Prism.FormsApplication;
#endif

namespace Prism.Autofac.Forms.Tests
{
    public class PrismApplicationFixture
    {
        [Theory]
        [InlineData(AutofacContainerType.Mutable)]
        [InlineData(AutofacContainerType.Immutable)]
        public void OnInitialized(AutofacContainerType containerType)
        {
            var app = new PrismApplicationMock(containerType);
            Assert.True(app.Initialized);
        }

        [Theory]
        [InlineData(AutofacContainerType.Mutable)]
        [InlineData(AutofacContainerType.Immutable)]
        public void OnInitialized_SetPage(AutofacContainerType containerType)
        {
            var view = new ViewMock();
            var app = new PrismApplicationMock(view, containerType);
            Assert.True(app.Initialized);
            Assert.NotNull(Application.Current.MainPage);
            Assert.Same(view, Application.Current.MainPage);
        }

        [Theory]
        [InlineData(AutofacContainerType.Mutable)]
        [InlineData(AutofacContainerType.Immutable)]
        public void ResolveTypeRegisteredWithContainer(AutofacContainerType containerType)
        {
            var app = new PrismApplicationMock(containerType);
            var service = app.Container.Resolve<IServiceMock>();
            Assert.NotNull(service);
            Assert.IsType<ServiceMock>(service);
        }

        [Theory]
        [InlineData(AutofacContainerType.Mutable)]
        [InlineData(AutofacContainerType.Immutable)]
        public void ResolveConcreteTypeNotRegisteredWithContainer(AutofacContainerType containerType)
        {
            var app = new PrismApplicationMock(containerType);
            Assert.True(app.Initialized);
            var concreteType = app.Container.Resolve<ConcreteTypeMock>();
            Assert.NotNull(concreteType);
            Assert.IsType<ConcreteTypeMock>(concreteType);
        }

        [Theory]
        [InlineData(AutofacContainerType.Mutable)]
        [InlineData(AutofacContainerType.Immutable)]
        public void ResolveTypeRegisteredWithDependencyService(AutofacContainerType containerType)
        {
            var app = new PrismApplicationMock(containerType);

            //Was: Autofac needs to be updated to support resolving unknown interfaces by using the DependencyService
            //Assert.Throws<ComponentNotRegisteredException>(() => app.Container.Resolve<IDependencyServiceMock>());

            //  Update 2017-04-03: Probably don't need this support, since use of DependencyService is being deprecated.
            //  So added "support" for it by secondarily registering the service directly in the Autofac container.
            var service = app.Container.Resolve<IDependencyServiceMock>();
            Assert.NotNull(service);
            Assert.IsType<DependencyServiceMock>(service);
        }

        [Theory]
        [InlineData(AutofacContainerType.Mutable)]
        [InlineData(AutofacContainerType.Immutable)]
        public void Container_ResolveNavigationService(AutofacContainerType containerType)
        {
            var app = new PrismApplicationMock(containerType);
            var navigationService = app.NavigationService;
            Assert.NotNull(navigationService);
            Assert.IsType<AutofacPageNavigationService>(navigationService);
        }

        [Theory]
        [InlineData(AutofacContainerType.Mutable)]
        [InlineData(AutofacContainerType.Immutable)]
        public void Module_Initialize(AutofacContainerType containerType)
        {
            var app = new PrismApplicationMock(containerType);
            var module = app.Container.Resolve<ModuleMock>();
            Assert.NotNull(module);
            Assert.True(module.Initialized);
        }

        [Theory]
        [InlineData(AutofacContainerType.Mutable)]
        [InlineData(AutofacContainerType.Immutable)]
        public async Task Navigate_UnregisteredView_ThrowNullReferenceException(AutofacContainerType containerType)
        {
            var app = new PrismApplicationMock(containerType);
            var navigationService = ResolveAndSetRootPage(app);
            var exception = await Assert.ThrowsAsync<NullReferenceException>(async () => await navigationService.NavigateAsync("missing"));
            Assert.Contains("missing", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData(AutofacContainerType.Mutable)]
        [InlineData(AutofacContainerType.Immutable)]
        public async Task Navigate_Key(AutofacContainerType containerType)
        {
            var app = new PrismApplicationMock(containerType);
            var navigationService = ResolveAndSetRootPage(app);
            await navigationService.NavigateAsync("view");
            var rootPage = ((IPageAware)navigationService).Page;
            Assert.True(rootPage.Navigation.ModalStack.Count == 1);
            Assert.IsType(typeof(ViewMock), rootPage.Navigation.ModalStack[0]);
        }

        [Theory]
        [InlineData(AutofacContainerType.Mutable)]
        [InlineData(AutofacContainerType.Immutable)]
        public void Container_ResolveByType(AutofacContainerType containerType)
        {
            var app = new PrismApplicationMock(containerType);
            var viewModel = app.Container.Resolve<ViewModelAMock>();
            Assert.NotNull(viewModel);
            Assert.IsType<ViewModelAMock>(viewModel);
        }

        [Theory]
        [InlineData(AutofacContainerType.Mutable)]
        [InlineData(AutofacContainerType.Immutable)]
        public void Container_ResolveByKey(AutofacContainerType containerType)
        {
            var app = new PrismApplicationMock(containerType);
            var viewModel = app.Container.ResolveNamed<ViewModelBMock>(ViewModelBMock.Key);
            Assert.NotNull(viewModel);
            Assert.IsType<ViewModelBMock>(viewModel);
        }

        private static INavigationService ResolveAndSetRootPage(PrismApplicationMock app)
        {
            var navigationService = app.NavigationService;
            ((IPageAware)navigationService).Page = new ContentPage();
            return navigationService;
        }
    }
}