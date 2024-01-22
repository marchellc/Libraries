using Common.IO.Collections;

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;

namespace Common.Utilities
{
    public class ServiceCollection : IServiceContainer, IServiceProvider
    {
        private LockedDictionary<Type, object> services;

        public ServiceCollection()
            => services = new LockedDictionary<Type, object>();

        public ServiceCollection(int size)
            => services = new LockedDictionary<Type, object>(size);

        public ServiceCollection(IDictionary<Type, object> services)
            => this.services = new LockedDictionary<Type, object>(services);

        public ServiceCollection(ServiceCollection collection)
            => this.services = new LockedDictionary<Type, object>(collection.services);

        public void AddService(Type serviceType, object serviceInstance)
            => services[serviceType] = serviceInstance;

        public void AddService(Type serviceType, object serviceInstance, bool promote)
            => services[serviceType] = serviceInstance;

        public void AddService(Type serviceType, ServiceCreatorCallback callback)
            => services[serviceType] = callback(this, serviceType);

        public void AddService(Type serviceType, ServiceCreatorCallback callback, bool promote)
            => services[serviceType] = callback(this, serviceType);

        public object GetService(Type serviceType)
        {
            if (services.TryGetValue(serviceType, out var service))
                return service;

            throw new Exception($"Service '{serviceType.FullName}' was not found in this collection.");
        }

        public void RemoveService(Type serviceType)
            => services.Remove(serviceType);

        public void RemoveService(Type serviceType, bool promote)
            => services.Remove(serviceType);
    }
}