﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Bindings;
using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Microsoft.Azure.WebJobs.Extensions.WebHooks
{
    internal class WebHookTriggerAttributeBindingProvider : ITriggerBindingProvider, IDisposable
    {
        private WebHookDispatcher _dispatcher;
        private bool disposedValue = false;

        public WebHookTriggerAttributeBindingProvider(WebHookDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            ParameterInfo parameter = context.Parameter;
            WebHookTriggerAttribute attribute = parameter.GetCustomAttribute<WebHookTriggerAttribute>(inherit: false);
            if (attribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }

            // Can bind to user types, HttpRequestMessage, WebHookContext, and all the Read
            // Types supported by StreamValueBinder
            IEnumerable<Type> supportedTypes = StreamValueBinder.GetSupportedTypes(FileAccess.Read)
                .Union(new Type[] { typeof(HttpRequestMessage), typeof(WebHookContext) });
            bool isSupportedTypeBinding = ValueBinder.MatchParameterType(parameter, supportedTypes);
            bool isUserTypeBinding = !isSupportedTypeBinding && WebHookTriggerBinding.IsValidUserType(parameter.ParameterType);
            if (!isSupportedTypeBinding && !isUserTypeBinding)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                    "Can't bind WebHookTriggerAttribute to type '{0}'.", parameter.ParameterType));
            }

            if (!isUserTypeBinding && attribute.FromUri)
            {
                throw new InvalidOperationException("'FromUri' can only be set to True when binding to custom Types.");
            }

            // Validate route format
            if (!string.IsNullOrEmpty(attribute.Route))
            {
                string[] routeSegements = attribute.Route.Split('/');
                if (routeSegements.Length > 2)
                {
                    throw new InvalidOperationException("WebHook routes can only have a maximum of two segments.");
                }
            }
            
            return Task.FromResult<ITriggerBinding>(new WebHookTriggerBinding(_dispatcher, context.Parameter, isUserTypeBinding, attribute));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_dispatcher != null)
                    {
                        _dispatcher.Dispose();
                        _dispatcher = null;
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
