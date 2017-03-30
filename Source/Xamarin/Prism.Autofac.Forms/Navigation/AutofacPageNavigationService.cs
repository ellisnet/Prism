using System;
using Autofac;
using Prism.Autofac.Immutable;
using Prism.Common;
using Prism.Logging;
using Prism.Navigation;
using Xamarin.Forms;

namespace Prism.Autofac.Navigation
{
    /// <summary>
    /// Page navigation service for using Autofac
    /// </summary>
    public class AutofacPageNavigationService : PageNavigationService
    {
        private IContainer _container;

        /// <summary>
        /// Create a new instance of <see cref="AutofacPageNavigationService"/> with <paramref name="container"/>
        /// </summary>
        /// <param name="applicationProvider">An instance of <see cref="IApplicationProvider"/></param>
        /// <param name="container">An instance of <see cref="IContainer"/></param>
        /// <param name="logger">An instance of <see cref="ILoggerFacade"/></param>
        public AutofacPageNavigationService(IContainer container, IApplicationProvider applicationProvider, ILoggerFacade logger)
            : base(applicationProvider, logger)
        {
            _container = container;
        }

        internal void SetContainer(IContainer container)
        {
            _container = container;
        }

        /// <summary>
        /// Resolve a <see cref="Page"/> from <see cref="_container"/> for <paramref name="segmentName"/>
        /// </summary>
        /// <param name="segmentName">Page to resolve</param>
        /// <returns>A <see cref="Page"/></returns>
        protected override Page CreatePage(string name)
        {
            Page result = null;

            if (PrismApplication.ContainerType == AutofacContainerType.Mutable)
            {
                if (!_container.IsRegisteredWithName<Page>(name))
                    throw new NullReferenceException($"The requested page '{name}' has not been registered.");

                result = _container.ResolveNamed<Page>(name);
            }
            else if (PrismApplication.ContainerType == AutofacContainerType.Immutable &&
                     _container is AutofacContainer afContainer)
            {
                if (!afContainer.IsRegisteredPageName(name))
                    throw new NullReferenceException($"The requested page '{name}' has not been registered.");

                result = afContainer.InternalOnlyContainer.ResolveNamed<Page>(name);
            }

            return result;
        }
    }
}
