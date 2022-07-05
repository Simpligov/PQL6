﻿/*
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading;
using Pql.ClientDriver.Protocol;
using Pql.ClientDriver.Wcf;
using Pql.Engine.DataContainer.Engine;
using Pql.Engine.Interfaces;
using Pql.Engine.Interfaces.Services;
using StructureMap;
namespace Pql.Engine.DataContainer
{
    [ErrorBehavior(typeof(DataServiceErrorHandler))]
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single, 
        ConcurrencyMode = ConcurrencyMode.Multiple, 
        IncludeExceptionDetailInFaults = false, 
        AddressFilterMode = AddressFilterMode.Any)]
    public sealed class DataService : IDataService, IDisposable
    {
        private readonly IPqlEngineHostProcess m_process;
        private readonly string m_instanceName;
        private readonly IContainer m_container;
        private readonly ITracer m_tracer;
        private readonly RawDataWriterPerfCounters m_counters;
        private readonly IDataEngineCache m_enginesCache;
        private readonly int m_maxEngineConcurrency;
        private readonly RequestProcessingManager[] m_requestManagers;
        private readonly ObjectPool<RequestProcessingManager> m_requestManagersPool;
        private readonly CancellationTokenSource m_cancellationTokenSource;
        private readonly string m_protocolVersion;

        public DataService(IContainer container, ITracer tracer, IPqlEngineHostProcess process, string instanceName, int maxEngineConcurrency, IDataEngineCache dataEngineCache)
        {
            if (string.IsNullOrEmpty(instanceName))
            {
                throw new ArgumentNullException("instanceName");
            }

            if (maxEngineConcurrency <= 0 || maxEngineConcurrency > 10000)
            {
                throw new ArgumentOutOfRangeException("maxEngineConcurrency", maxEngineConcurrency, "Invalid value");
            }

            m_protocolVersion = "default";
            m_container = container ?? throw new ArgumentNullException(nameof(container));
            m_tracer = tracer ?? throw new ArgumentNullException("tracer");

            m_cancellationTokenSource = new CancellationTokenSource();
            m_process = process ?? throw new ArgumentNullException("process");
            m_instanceName = instanceName;
            m_maxEngineConcurrency = maxEngineConcurrency;
            
            m_counters = new RawDataWriterPerfCounters(instanceName);

            m_requestManagers = new RequestProcessingManager[maxEngineConcurrency];

            // request processing managers will not be dynamically created, 
            // this is to explicitly limit concurrency regardless of service infrastructure settings
            m_requestManagersPool = new ObjectPool<RequestProcessingManager>(m_maxEngineConcurrency, null);
            for (var i = 0; i < m_requestManagersPool.Capacity; i++)
            {
                m_requestManagers[i] = new RequestProcessingManager(m_tracer, m_process, m_counters);
                m_requestManagersPool.Return(m_requestManagers[i]);
            }

            m_enginesCache = dataEngineCache ?? new DataEngineCache(m_container, m_tracer, m_instanceName, m_maxEngineConcurrency);
        }

        public Message Process(Message message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            if (!(message is PqlMessage pqlMessage))
            {
                throw new Exception(string.Format("Invalid message type, expected {0}, got {1}",
                    typeof(PqlMessage).AssemblyQualifiedName, message.GetType().AssemblyQualifiedName));
            }

            try
            {
                // re-establish authentication context as it is on the client side
                var authContext = AuthContextSerializer.GetObject(pqlMessage.AuthTicket);
                PqlEngineSecurityContext.Set(authContext);

                // get a processing manager from pool, start production
                var holder = m_requestManagersPool.Take(m_cancellationTokenSource.Token);
                try
                {
                    var engine = m_enginesCache.GetEngine(authContext.TenantId, pqlMessage.ScopeId);
                    holder.Item.Attach((PqlMessage) message, engine, authContext);
                    engine.BeginExecution(holder.Item.ExecutionContext);
                }
                catch
                {
                    holder.Item.ExecutionContext.Cancel(null);
                    holder.Dispose();
                    throw;
                }

                // return the message to WCF infrastructure
                try
                {
                    holder.Item.ExecutionContext.AttachRequestCompletion();
                    return new PqlMessage(
                        holder.Item, 
                        new IDisposable[]
                            {
                                holder.Item.ExecutionContext.Completion, 
                                holder
                            }, 
                        pqlMessage.AuthTicket, pqlMessage.ScopeId, m_protocolVersion);
                }
                catch (Exception e)
                {
                    holder.Item.ExecutionContext.Cancel(e);
                    holder.Dispose();
                    throw;
                }
            }
            catch (Exception e)
            {
                m_tracer.Exception(e);
                return new PqlMessage(new PqlErrorDataWriter(1, e, true), null, pqlMessage.AuthTicket, pqlMessage.ScopeId, m_protocolVersion);
            }
        }

        public void Dispose()
        {
            // first: signal cancellation to incoming callers on this class - those which have not yet started processing
            if (m_cancellationTokenSource != null)
            {
                m_cancellationTokenSource.Cancel();
            }

            // second: dispose engine - this will signal cancellation to all actively executing requests
            if (m_enginesCache != null)
            {
                m_enginesCache.Dispose();
            }

            // third: now get rid of perf counters
            if (m_counters != null)
            {
                m_counters.Dispose();
            }

            // dispose the pool of contexts
            if (m_requestManagers != null)
            {
                foreach (var item in m_requestManagers)
                {
                    item.Dispose();
                }
            }
        }
    }
}*/