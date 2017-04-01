using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autofac;
using Autofac.Core;

namespace Prism.Autofac.Forms.Immutable
{
    public class RuntimeComponentRegistry : IComponentRegistry
    {
        private IContainer _container;

        public RuntimeComponentRegistry(IContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        public bool TryGetRegistration(Service service, out IComponentRegistration registration)
        {
            return _container.ComponentRegistry.TryGetRegistration(service, out registration);
        }

        public bool IsRegistered(Service service)
        {
            return _container.ComponentRegistry.IsRegistered(service);
        }

        public void Register(IComponentRegistration registration)
        {
            throw new InvalidOperationException("It is not possible to perform registration operations after the Container has been built.");
        }

        public void Register(IComponentRegistration registration, bool preserveDefaults)
        {
            throw new InvalidOperationException("It is not possible to perform registration operations after the Container has been built.");
        }

        public void AddRegistrationSource(IRegistrationSource source)
        {
            throw new InvalidOperationException("It is not possible to perform registration operations after the Container has been built.");
        }

        public IEnumerable<IComponentRegistration> RegistrationsFor(Service service)
        {
            return _container.ComponentRegistry.RegistrationsFor(service);
        }

        public IEnumerable<IComponentRegistration> Registrations => _container.ComponentRegistry.Registrations;

        public IEnumerable<IRegistrationSource> Sources => _container.ComponentRegistry.Sources;

        public bool HasLocalComponents => _container.ComponentRegistry.HasLocalComponents;

        public event EventHandler<ComponentRegisteredEventArgs> Registered;
        public event EventHandler<RegistrationSourceAddedEventArgs> RegistrationSourceAdded;

        public void Dispose()
        {
            _container = null;
        }
    }
}
