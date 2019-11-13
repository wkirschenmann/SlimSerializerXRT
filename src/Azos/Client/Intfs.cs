﻿/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;
using System.Collections.Generic;
using System.Net.Http;

using Azos.Apps;
using Azos.Collections;
using Azos.Instrumentation;

namespace Azos.Client
{
  /// <summary>
  /// Represents a uniquely named service which provides services via its endpoints.
  /// A single service may serve more than one "Contract" - a logical sub-division of a service.
  /// For example, a "User" service may provide "List" and "Admin" contracts used for querying/listing users and adding/deleting users respectively.
  /// </summary>
  public interface IService : IApplicationComponent, INamed
  {
    /// <summary>
    /// Enumerates endpoints which provide this service. Depending on implementation this property may return
    /// all physical endpoints of higher-order regional endpoints for high-scale systems.
    /// Typically it is used with Http/s and returns actual endpoints which provide services which this instance
    /// represents
    /// </summary>
    IEnumerable<IEndpoint> Endpoints {  get; }

    /// <summary>
    /// Gets default network name for this service if any, e.g. "internoc".
    /// Simple implementations typically do not use named logical networks, so this value is set to "default" or empty
    /// </summary>
    string DefaultNetwork { get; }

    /// <summary>
    /// Gets default binding name for this service, e.g. "https".
    /// </summary>
    string DefaultBinding {  get; }

    /// <summary>
    /// When &gt; 0 imposes a call timeout expressed in milliseconds, otherwise the system uses hard-coded timeout (e.g. 10 sec)
    /// </summary>
    int DefaultTimeoutMs { get; }

    /// <summary>
    /// Returns endpoints which should be re-tried subsequently on failure.
    /// The endpoints are returned in the sequence which depend on implementation.
    /// Typically the sequence is based on network routing efficiency and least/loaded resources.
    /// The optional shardingKey parameter may be passed for multi-sharding scenarios.
    /// </summary>
    /// <param name="remoteAddress">
    ///   The remote service logical address, such as the regional host name for Sky applications.
    ///   The system resolves this address to physical address depending on binding and contract on the remote host
    /// </param>
    /// <param name="contract">Service contract name</param>
    /// <param name="shardKey">Optional sharding parameter. The system will direct the call to the appropriate shard in the service partition if it is used</param>
    /// <param name="network">The name of the logical network to use for a call, or null to use the default network</param>
    /// <param name="binding">
    ///   The service binding to use, or null for default.
    ///   Bindings are connection technology/protocols (such as Http(s)/Glue/GRPC etc..) used to make the call
    /// </param>
    /// <returns>Endpoint(s) which should be (re)tried in the order of enumeration</returns>
    IEnumerable<EndpointAssignment> GetEndpointsForCall(string remoteAddress, string contract, object shardKey = null, string network = null, string binding = null);

    /// <summary>
    /// Gets the physical transport used to make remote calls. Depending on implementation the system
    /// may return a pooled transport, re-use already acquired one (if transport supports multiplexing) etc.
    /// </summary>
    /// <param name="assignment">Endpoint to connect to</param>
    /// <param name="reserve">Pass true to reserve this transport for the caller. The caller must release the reserved transport</param>
    ITransportImplementation AcquireTransport(EndpointAssignment assignment, bool reserve = false);

    /// <summary>
    /// Releases the transport acquired by the AcquireTransport call
    /// </summary>
    void ReleaseTransport(ITransportImplementation transport);
  }


  /// <summary>
  /// Implements an IService, adding transport acquisition/release behavior
  /// </summary>
  public interface IServiceImplementation : IService, IDisposable, IInstrumentable
  {

  }

  /// <summary>
  /// Marks services that have HTTP semantics - the ones based on HttpClient-like operations, REST, RPC, JSON etc...
  /// </summary>
  public interface IHttpService : IService
  {

  }


  /// <summary>
  /// Represents a transport channel which is used to make remote server calls.
  /// For Http this is typically a HttpClient configured with default headers and protocol handlers
  /// </summary>
  public interface ITransport
  {
    /// <summary>
    /// Returns a service endpoint which this transport connects to
    /// </summary>
    EndpointAssignment Assignment { get; }
  }

  /// <summary>
  /// Transport implementation
  /// </summary>
  public interface ITransportImplementation : ITransport, IDisposable{ }

  /// <summary>
  /// Marks transports that have HTTP semantics - the ones based on HttpClient-like operations, REST, RPC, JSON etc...
  /// </summary>
  public interface IHttpTransport : ITransport
  {
    /// <summary>
    /// Returns HttpClient used for making calls
    /// </summary>
    HttpClient Client {  get;}
  }


  /// <summary>
  /// Represents an abstraction of a remote service endpoint.
  /// Endpoints provide connection point for services.
  /// Each endpoint represents a specific connection type via Binding(protocol)
  /// </summary>
  public interface IEndpoint : IApplicationComponent
  {
    /// <summary>
    /// Returns service which this endpoint represents
    /// </summary>
    IService Service { get; }

    /// <summary>
    /// Provides logical network name which this endpoint services, e.g. "noc","internoc","pub" etc.
    /// Depending on implementation, the actual physical remote endpoint address is calculated based on
    /// logical RemoteAddress, logical Network, binding/protocol, and contract - a logical "sub-service"/"port".
    /// </summary>
    string Network { get; }

    /// <summary>
    /// Provides logical binding name for this endpoint, for example "https". Bindings are protocols/connection methods supported.
    /// A typical REST-full system typically uses http/s bindings.
    /// </summary>
    string Binding { get; }

    /// <summary>
    /// Provides remote address routing/logical host/partition name which is used to match the callers address.
    /// For Sky apps this is a metabase host name (regional path) of the target server which provides the service
    /// </summary>
    string RemoteAddress { get; }

    /// <summary>
    /// Provides logical contract name for the functionality which this service endpoint covers.
    /// For Http bindings this typically contains URI root path, such as "/user/admin"
    /// </summary>
    string Contract {  get; }

    /// <summary>
    /// Groups endpoints by logical shard. Shard numbers are positive consecutive integers starting from 0 (e.g. 0,1,2,3...)
    /// If sharding is not used then all endpoints are set to the same shard of 0.
    /// Before service calls are made, the system takes "shardKey" and tries to find the partition based on sharding object,
    /// this way the load may be parallelized in "strands" of execution.
    /// </summary>
    int Shard { get; }

    /// <summary>
    /// Relative order of endpoint per shard.
    /// Endpoints are tried in ascending order e.g. 0=Primary, 1=Secondary etc...
    /// When calculating the destination endpoint, the system uses RemoteAddress (and possibly other parameters such as current QOS/statistics) first
    /// (for the appropriate binding/contract), then shard, then ShardOrder within the shard, thus you may designate
    /// primary/secondary/tertiary etc.. using this parameter
    /// </summary>
    int ShardOrder { get; }

    /// <summary>
    /// When set to a value &gt; 0 imposes a call timeout expressed in milliseconds
    /// </summary>
    int TimeoutMs { get; }

    /// <summary>
    /// When set, provides timestamp when circuit breaker has tripped on this endpoint.
    /// If this property is not null then the endpoint is in the "tripped" state and should not
    /// be tried for a call (until it auto resets - which is up to the implementation).
    /// </summary>
    DateTime? CircuitBreakerTimeStampUtc{ get; }

    /// <summary>
    /// When set, provides timestamp when this endpoint was brought offline.
    /// The difference from CircuitBreaker - offline/online endpoints are controlled manually (via code),
    /// the offline endpoint does NOT auto-reset unlike circuit breaker does
    /// </summary>
    DateTime? OfflineTimeStampUtc { get; }


    /// <summary>
    /// Returns true when endpoint was not purposely put offline and circuit breaker has not tripped
    /// </summary>
    bool IsAvailable{  get;}

    /// <summary>
    /// Returns a short status message of the endpoint, e.g. "Offline until Sun")
    /// </summary>
    string StatusMsg {  get; }
  }

  public interface IEndpointImplementation : IEndpoint, IDisposable
  {
    /// <summary>
    /// Notifies endpoint o call success. This typically used to update call statistics
    /// </summary>
    void NotifyCallSuccess(ITransport transport);


    /// <summary>
    /// Notifies circuit breaker of error. The breaker may trip if error threshold
    /// is reached (as defined by endpoint/config).
    /// Returns true when the cause is deterministic connection/service CALL problem (WebException/Timeout) and it influenced the
    /// breaker state machine vs. business exception which is logical error (e.g. "Bad Request", "Access denied" etc.) which should not trip the
    /// breaker because it is a deterministic failure which does not indicate problems with network/server
    /// </summary>
    bool NotifyCallError(ITransport transport, Exception error);

    /// <summary>
    /// Resets circuit breaker returning true if endpoint circuit was reset.
    /// False is returned if circuit breaker could not be reset (e.g. remote endpoint is still disabled)
    /// </summary>
    bool TryResetCircuitBreaker(string statusMessage);

    /// <summary>
    /// Puts endpoint online
    /// </summary>
    void PutOnline(string statusMsg);

    /// <summary>
    /// Puts endpoint offline
    /// </summary>
    void PutOffline(string statusMsg);
  }


  /// <summary>
  /// Marks endpoints that have HTTP semantics - the ones based on HttpClient-like operations, REST, RPC, JSON etc...
  /// </summary>
  public interface IHttpEndpoint : IEndpoint
  {
    Uri Uri { get; }
  }
}