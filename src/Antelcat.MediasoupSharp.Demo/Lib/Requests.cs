using Antelcat.MediasoupSharp.RtpParameters;
using FBS.Consumer;
using FBS.RtpParameters;
using FBS.SctpParameters;
using FBS.WebRtcTransport;

namespace Antelcat.MediasoupSharp.Demo.Lib;


#region Payloads

[Serializable]
public record JoinRequest(
    string DisplayName,
    DeviceR Device,
    RtpCapabilities RtpCapabilities,
    SctpCapabilities SctpCapabilities);
	
[Serializable]
public record CreateWebRtcTransportRequest(
    bool ForceTcp,
    bool Producing,
    bool Consuming,
    SctpCapabilities? SctpCapabilities
);

[Serializable]
public record ConnectWebRtcTransportRequest(
    string TransportId,
    DtlsParametersT DtlsParameters
);

[Serializable]
public record RestartIceRequest(string TransportId);

[Serializable]
public record ProduceRequest<TWorkerAppData>(
    string TransportId, 
    MediaKind Kind, 
    RtpParameters.RtpParameters RtpParameters, 
    TWorkerAppData AppData
);

[Serializable]
public record ProducerRequest(string ProducerId);

[Serializable]
public record ConsumerRequest(string ConsumerId);

[Serializable]
public record SetConsumerPreferredLayersRequest(
    string ConsumerId,
    byte SpatialLayer,
    byte TemporalLayer) 
    : ConsumerRequest(ConsumerId);

[Serializable]
public record SetConsumerPriorityRequest(
    string ConsumerId,
    SetPriorityRequestT Priority) : ConsumerRequest(ConsumerId);

[Serializable]
public record ProduceDataRequest(
    string? TransportId,
    SctpStreamParametersT SctpStreamParameters,
    string Label,
    string Protocol,
    Dictionary<string, object> AppData
    );

[Serializable]
public record ChangeDisplayNameRequest(string DisplayName);

[Serializable]
public record TransportRequest(string TransportId);

[Serializable]
public record DataProducerRequest(string DataProducerId);

[Serializable]
public record DataConsumerRequest(string DataConsumerId);

[Serializable]
public record ApplyNetworkThrottleRequest(
    bool? Secret,
    int? Uplink,
    int? Downlink,
    int? Rtt,
    int? PacketLoss
);

[Serializable]
public record ResetNetworkThrottleRequest(bool? Secret);

public record CreateBroadcasterRequest(
    string Id,
    string DisplayName,
    DeviceR Device,
    RtpCapabilities? RtpCapabilities);

public record CreateBroadcastTransport(
    string BroadcasterId,
    string Type,
    bool RtcpMux = false,
    bool Comedia = true,
    SctpCapabilities? SctpCapabilities = null);

public record ConnectBroadcasterTransportRequest(DtlsParameters DtlsParameters);

public record CreateBroadcasterProducerRequest(
    MediaKind Kind,
    RtpParameters.RtpParameters RtpParameters);

#endregion