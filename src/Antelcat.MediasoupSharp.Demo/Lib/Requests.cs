using Antelcat.MediasoupSharp.RtpParameters;
using Antelcat.MediasoupSharp.SctpParameters;
using FBS.Consumer;
using FBS.RtpParameters;
using FBS.SctpParameters;
using FBS.WebRtcTransport;

namespace Antelcat.MediasoupSharp.Demo.Lib;


#region Payloads

internal record Device(string Flag, string Name, string Version);

[Serializable]
internal record JoinRequest(
    string DisplayName,
    Device Device,
    RtpCapabilities RtpCapabilities,
    SctpCapabilities SctpCapabilities);
	
[Serializable]
internal record CreateWebRtcTransportRequest(
    bool ForceTcp,
    bool Producing,
    bool Consuming,
    SctpCapabilities? SctpCapabilities
);

[Serializable]
internal record ConnectWebRtcTransportRequest(
    string TransportId,
    DtlsParameters DtlsParameters
);

[Serializable]
internal record RestartIceRequest(string TransportId);

[Serializable]
internal record ProduceRequest(
    string TransportId, 
    MediaKind Kind, 
    RtpParameters.RtpParameters RtpParameters, 
    Dictionary<string,object> AppData
);

[Serializable]
internal record ProducerRequest(string ProducerId);

[Serializable]
internal record ConsumerRequest(string ConsumerId);

[Serializable]
internal record SetConsumerPreferredLayersRequest(
    string ConsumerId,
    byte SpatialLayer,
    byte TemporalLayer) 
    : ConsumerRequest(ConsumerId);

[Serializable]
internal record SetConsumerPriorityRequest(
    string ConsumerId,
    SetPriorityRequestT Priority) : ConsumerRequest(ConsumerId);

[Serializable]
internal record ProduceDataRequest(
    string TransportId,
    SctpStreamParametersT SctpStreamParameters,
    string Label,
    string Protocol,
    Dictionary<string, object> AppData
    );

[Serializable]
internal record ChangeDisplayNameRequest(string DisplayName);

[Serializable]
internal record TransportRequest(string TransportId);

[Serializable]
internal record DataProducerRequest(string DataProducerId);

[Serializable]
internal record DataConsumerRequest(string DataConsumerId);

[Serializable]
internal record ApplyNetworkThrottleRequest(
    bool? Secret,
    int? Uplink,
    int? Downlink,
    int? Rtt,
    int? PacketLoss
);

[Serializable]
internal record ResetNetworkThrottleRequest(bool? Secret);
#endregion