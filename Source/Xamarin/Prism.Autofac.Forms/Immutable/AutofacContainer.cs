using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Core;
using Autofac.Core.Lifetime;
using Autofac.Core.Resolving;
using Prism.Modularity;

namespace Prism.Autofac
{
    /// <summary>
    /// Options for whether the Autofac Container can be updated or not.
    /// </summary>
    public enum AutofacContainerType
    {
        /// <summary>
        /// An Autofac Container that can be updated via the obsolete ContainerBuilder.Update() method.
        /// </summary>
        Mutable = 0,
        /// <summary>
        /// An Autofac Container that is not able to be updated via the updated obsolete ContainerBuilder.Update() method (RECOMMENDED).
        /// </summary>
        Immutable = 1
    }
}

namespace Prism.Autofac.Immutable
{
    public class AutofacContainer : IContainer
    {
        private readonly object _containerLocker = new object();

        private IContainer _container = null;

        private ContainerBuilder _builder = new ContainerBuilder();

        public ContainerBuilder Builder
        {
            get
            {
                if (_container != null)
                {
                    throw new InvalidOperationException("It is not possible to access the ContainerBuilder after the Container has been built.");
                }
                return _builder;
            }
        }

        //TODO: Need to figure out how/when to handle this ModuleInitializer - may not be needed
        //public IModuleInitializer ModuleInitializer
        //{
        //    get
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

        private void CheckBuildContainer()
        {
            if (_container == null)
            {
                lock (_containerLocker)
                {
                    if (_container == null)
                    {
                        _container = _builder.Build();
                    }
                }
            }
        }

        public object ResolveComponent(IComponentRegistration registration, IEnumerable<Parameter> parameters)
        {
            CheckBuildContainer();
            return _container.ResolveComponent(registration, parameters);
        }

        public IComponentRegistry ComponentRegistry
        {
            get
            {
                CheckBuildContainer();
                return _container.ComponentRegistry;
            }
        }

        public void Dispose()
        {
            _builder = null;
            _container?.Dispose();
            _container = null;
        }

        public ILifetimeScope BeginLifetimeScope()
        {
            throw new NotImplementedException();
        }

        public ILifetimeScope BeginLifetimeScope(object tag)
        {
            throw new NotImplementedException();
        }

        public ILifetimeScope BeginLifetimeScope(Action<ContainerBuilder> configurationAction)
        {
            throw new NotImplementedException();
        }

        public ILifetimeScope BeginLifetimeScope(object tag, Action<ContainerBuilder> configurationAction)
        {
            throw new NotImplementedException();
        }

        public IDisposer Disposer
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public object Tag
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public event EventHandler<LifetimeScopeBeginningEventArgs> ChildLifetimeScopeBeginning;
        public event EventHandler<LifetimeScopeEndingEventArgs> CurrentScopeEnding;
        public event EventHandler<ResolveOperationBeginningEventArgs> ResolveOperationBeginning;
    }
}
