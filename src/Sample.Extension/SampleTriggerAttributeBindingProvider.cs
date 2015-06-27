﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Framework;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Sample.Extension
{
    public class SampleTriggerValue
    {
        // TODO: Define the default type that your trigger binding
        // binds to.
    }

    internal class SampleTriggerAttributeBindingProvider : ITriggerBindingProvider
    {
        private SampleConfiguration _config;

        public SampleTriggerAttributeBindingProvider(SampleConfiguration config)
        {
            _config = config;
        }

        /// <inheritdoc/>
        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            ParameterInfo parameter = context.Parameter;
            SampleTriggerAttribute attribute = parameter.GetCustomAttribute<SampleTriggerAttribute>(inherit: false);
            if (attribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }

            // TODO: Define the types your binding supports here
            if (parameter.ParameterType != typeof(SampleTriggerValue) &&
                parameter.ParameterType != typeof(string))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, 
                    "Can't bind SampleTrigger to type '{0}'.", parameter.ParameterType));
            }

            return Task.FromResult<ITriggerBinding>(new SampleBinding(context.Parameter));
        }

        private class SampleBinding : ITriggerBinding
        {
            private ParameterInfo _parameter;
            private IReadOnlyDictionary<string, Type> _bindingContract;

            public SampleBinding(ParameterInfo parameter)
            {
                _parameter = parameter;
                _bindingContract = CreateBindingDataContract();
            }

            public IReadOnlyDictionary<string, Type> BindingDataContract
            {
                get { return _bindingContract; }
            }

            public Type TriggerValueType
            {
                get { return typeof(SampleTriggerValue); }
            }

            public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
            {
                // TODO: Perform any required conversions on the value
                // E.g. convert from Dashboard invoke string to our trigger
                // value type
                SampleTriggerValue triggerValue = value as SampleTriggerValue;
                return BindAsync(triggerValue, context);
            }

            public IListenerFactory CreateListenerFactory(FunctionDescriptor descriptor, ITriggeredFunctionExecutor executor)
            {
                return new ListenerFactory(executor);
            }

            public ParameterDescriptor ToParameterDescriptor()
            {
                return new SampleTriggerParameterDescriptor
                {
                    Name = _parameter.Name,
                    DisplayHints = new ParameterDisplayHints
                    {
                        // TODO: Customize your Dashboard display strings
                        Prompt = "Sample",
                        Description = "Sample trigger fired",
                        DefaultValue = "Sample"
                    }
                };
            }

            private IReadOnlyDictionary<string, object> GetBindingData(SampleTriggerValue value)
            {
                Dictionary<string, object> bindingData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                bindingData.Add("SampleTrigger", value);

                // TODO: Add any additional binding data

                return bindingData;
            }

            private IReadOnlyDictionary<string, Type> CreateBindingDataContract()
            {
                Dictionary<string, Type> contract = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
                contract.Add("SampleTrigger", typeof(SampleTriggerValue));

                // TODO: Add any additional binding contract members

                return contract;
            }

            private class SampleTriggerParameterDescriptor : TriggerParameterDescriptor
            {
                public override string GetTriggerReason(IDictionary<string, string> arguments)
                {
                    // TODO: Customize your Dashboard display string
                    return string.Format("Sample trigger fired at {0}", DateTime.UtcNow.ToString("o"));
                }
            }

            private class SampleValueBinder : ValueBinder
            {
                private object _value;

                public SampleValueBinder(ParameterInfo parameter, SampleTriggerValue value)
                    : base (parameter.ParameterType)
                {
                    _value = value;
                }

                public override object GetValue()
                {
                    // TODO: Perform any required conversions
                    if (Type == typeof(string))
                    {
                        return _value.ToString();
                    }
                    return _value;
                }

                public override string ToInvokeString()
                {
                    // TODO: Customize your Dashboard invoke string
                    return "Sample";
                }
            }

            private class ListenerFactory : IListenerFactory
            {
                private ITriggeredFunctionExecutor _executor;

                public ListenerFactory(ITriggeredFunctionExecutor executor)
                {
                    _executor = executor;
                }

                public Task<IListener> CreateAsync(ListenerFactoryContext context)
                {
                    return Task.FromResult<IListener>(new Listener(_executor));
                }

                private class Listener : IListener
                {
                    private ITriggeredFunctionExecutor _executor;
                    private System.Timers.Timer _timer;

                    public Listener(ITriggeredFunctionExecutor executor)
                    {
                        _executor = executor;

                        // TODO: For this sample, we're using a timer to generate
                        // trigger events. You'll replace this with your event source.
                        _timer = new System.Timers.Timer(10 * 1000)
                        {
                            AutoReset = true
                        };
                        _timer.Elapsed += OnTimer;
                    }

                    public Task StartAsync(CancellationToken cancellationToken)
                    {
                        // TODO: Start monitoring your event source
                        _timer.Start();
                        return Task.FromResult(true);
                    }

                    public Task StopAsync(CancellationToken cancellationToken)
                    {
                        // TODO: Stop monitoring your event source
                        _timer.Stop();
                        return Task.FromResult(true);
                    }

                    public void Dispose()
                    {
                        // TODO: Perform any final cleanup
                        _timer.Dispose();
                    }

                    public void Cancel()
                    {
                        // TODO: cancel any outstanding tasks initiated by this listener
                    }

                    private void OnTimer(object sender, System.Timers.ElapsedEventArgs e)
                    {
                        // TODO: When you receive new events from your event source,
                        // invoke the function executor
                        TriggeredFunctionData input = new TriggeredFunctionData
                        {
                            TriggerValue = new SampleTriggerValue()
                        };
                        _executor.TryExecuteAsync(input, CancellationToken.None).Wait();
                    }
                }
            }
        }
    }
}
