using System.Text.Json.Serialization;
using Antelcat.MediasoupSharp.RtpParameters;
using Antelcat.MediasoupSharp.SctpParameters;
using FBS.Consumer;
using FBS.RtpParameters;
using FBS.SctpParameters;
using FBS.WebRtcTransport;
using SctpStreamParameters = Antelcat.MediasoupSharp.SctpParameters.SctpStreamParameters;

namespace Antelcat.MediasoupSharp.Demo.Lib;


#region Payloads

public record HasId(string Id);

public record Device(string Flag, string Name, string Version);

[Serializable]
public record JoinRequest(
    string DisplayName,
    Device Device,
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
public record ProduceRequest(
    string TransportId, 
    MediaKind Kind, 
    RtpParameters.RtpParameters RtpParameters, 
    Dictionary<string,object> AppData
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

[Serializable]
public record PeerProducer(string Id, MediaKind Kind);

[Serializable]
public record PeerInfo(string Id, string DisplayName, Device Device, List<PeerProducer> Producers);

[Serializable]
public record PeerInfos(List<PeerInfo> Peers);

public record CreateBroadcasterRequest(
    string Id,
    string DisplayName,
    Device Device,
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

public record CreateBroadcastConsumerResponse(
    string Id,
    string ProducerId,
    MediaKind Kind,
    RtpParameters.RtpParameters RtpParameters,
    FBS.RtpParameters.Type Type);

public record IdWithStreamId(string Id, ushort StreamId);

public record NewDataConsumerRequest(
    string DataProducerId,
    string Id,
    SctpStreamParameters? SctpStreamParameters,
    string Label,
    string Protocol,
    Dictionary<string, object> AppData
)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PeerId { get; set; }
}

#endregion