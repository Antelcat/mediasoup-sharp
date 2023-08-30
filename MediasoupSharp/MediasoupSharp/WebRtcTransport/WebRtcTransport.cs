using MediasoupSharp.Channel;
using MediasoupSharp.Exceptions;
using MediasoupSharp.PayloadChannel;
using MediasoupSharp.Transport;

namespace MediasoupSharp.WebRtcTransport
{
    public class WebRtcTransport : Transport.Transport
    {
        /// <summary>
        /// Logger.
        /// </summary>
        private readonly ILogger<WebRtcTransport> _logger;

        public WebRtcTransportData Data { get; }

        /// <summary>
        /// <para>Events:</para>
        /// <para>@emits icestatechange - (iceState: IceState)</para>
        /// <para>@emits iceselectedtuplechange - (iceSelectedTuple: TransportTuple)</para>
        /// <para>@emits dtlsstatechange - (dtlsState: DtlsState)</para>
        /// <para>@emits sctpstatechange - (sctpState: SctpState)</para>
        /// <para>@emits trace - (trace: TransportTraceEventData)</para>
        /// <para>Observer events:</para>
        /// <para>@emits close</para>
        /// <para>@emits newproducer - (producer: Producer)</para>
        /// <para>@emits newconsumer - (consumer: Consumer)</para>
        /// <para>@emits newdataproducer - (dataProducer: DataProducer)</para>
        /// <para>@emits newdataconsumer - (dataConsumer: DataConsumer)</para>
        /// <para>@emits icestatechange - (iceState: IceState)</para>
        /// <para>@emits iceselectedtuplechange - (iceSelectedTuple: TransportTuple)</para>
        /// <para>@emits dtlsstatechange - (dtlsState: DtlsState)</para>
        /// <para>@emits sctpstatechange - (sctpState: SctpState)</para>
        /// <para>@emits trace - (trace: TransportTraceEventData)</para>
        /// </summary>
        /// <param name="loggerFactory"></param>
        /// <param name="internal"></param>
        /// <param name="data"></param>
        /// <param name="channel"></param>
        /// <param name="payloadChannel"></param>
        /// <param name="appData"></param>
        /// <param name="getRouterRtpCapabilities"></param>
        /// <param name="getProducerById"></param>
        /// <param name="getDataProducerById"></param>
        public WebRtcTransport(ILoggerFactory loggerFactory,
            TransportInternal @internal,
            WebRtcTransportData data,
            IChannel channel,
            IPayloadChannel payloadChannel,
            Dictionary<string, object>? appData,
            Func<RtpCapabilities> getRouterRtpCapabilities,
            Func<string, Task<Producer.Producer?>> getProducerById,
            Func<string, Task<DataProducer.DataProducer?>> getDataProducerById) : base(loggerFactory, @internal, data, channel, payloadChannel, appData, getRouterRtpCapabilities, getProducerById, getDataProducerById)
        {
            _logger = loggerFactory.CreateLogger<WebRtcTransport>();

            Data = data;

            HandleWorkerNotifications();
        }

        /// <summary>
        /// Close the WebRtcTransport.
        /// </summary>
        protected override Task OnCloseAsync()
        {
            Data.IceState = IceState.Closed;
            Data.IceSelectedTuple = null;
            Data.DtlsState = DtlsState.Closed;

            if (Data.SctpState.HasValue)
            {
                Data.SctpState = SctpState.Closed;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Router was closed.
        /// </summary>
        protected override Task OnRouterClosedAsync()
        {
            return OnCloseAsync();
        }

        /// <summary>
        /// Provide the WebRtcTransport remote parameters.
        /// </summary>
        public override async Task ConnectAsync(object parameters)
        {
            _logger.LogDebug($"ConnectAsync() | WebRtcTransport:{TransportId}");

            if (parameters is not DtlsParameters dtlsParameters)
            {
                throw new ArgumentException($"{nameof(parameters)} type is not DtlsParameters");
            }

            await ConnectAsync(dtlsParameters);
        }

        private async Task ConnectAsync(DtlsParameters dtlsParameters)
        {
            using (await CloseLock.ReadLockAsync())
            {
                if (Closed)
                {
                    throw new InvalidStateException("Transport closed");
                }

                var reqData = new { DtlsParameters = dtlsParameters };
                var resData = await Channel.RequestAsync(MethodId.TRANSPORT_CONNECT, Internal.TransportId, reqData);
                var responseData = resData!.Deserialize<WebRtcTransportConnectResponseData>()!;
                
                // Update data.
                Data.DtlsParameters.Role = responseData.DtlsLocalRole;
            }
        }

        /// <summary>
        /// Restart ICE.
        /// </summary>
        public async Task<IceParameters> RestartIceAsync()
        {
            _logger.LogDebug($"RestartIceAsync() | WebRtcTransport:{TransportId}");

            using (await CloseLock.ReadLockAsync())
            {
                if (Closed)
                {
                    throw new InvalidStateException("Transport closed");
                }

                var resData = await Channel.RequestAsync(MethodId.TRANSPORT_RESTART_ICE, Internal.TransportId);
                var responseData = resData!.Deserialize<WebRtcTransportRestartIceResponseData>()!;

                // Update data.
                Data.IceParameters = responseData.IceParameters;

                return Data.IceParameters;
            }
        }

        #region Event Handlers

        private void HandleWorkerNotifications()
        {
            Channel.MessageEvent += OnChannelMessage;
        }

        private void OnChannelMessage(string targetId, string @event, string? data)
        {
            if (targetId != Internal.TransportId)
            {
                return;
            }

            switch (@event)
            {
                case "icestatechange":
                    {
                        if (data == null)
                        {
                            _logger.LogWarning($"icestatechange event's data is null.");
                            break;
                        }

                        var notification = data!.Deserialize<TransportIceStateChangeNotificationData>()!;
                        Data.IceState = notification.IceState;

                        Emit("icestatechange", Data.IceState);

                        // Emit observer event.
                        Observer.Emit("icestatechange", Data.IceState);

                        break;
                    }

                case "iceselectedtuplechange":
                    {
                        if (data == null)
                        {
                            _logger.LogWarning($"iceselectedtuplechange event's data is null.");
                            break;
                        }

                        var notification =data!.Deserialize<TransportIceSelectedTupleChangeNotificationData>()!;
                        Data.IceSelectedTuple = notification.IceSelectedTuple;

                        Emit("iceselectedtuplechange", Data.IceSelectedTuple);

                        // Emit observer event.
                        Observer.Emit("iceselectedtuplechange", Data.IceSelectedTuple);

                        break;
                    }

                case "dtlsstatechange":
                    {
                        if (data == null)
                        {
                            _logger.LogWarning($"dtlsstatechange event's data is null.");
                            break;
                        }

                        var notification = data!.Deserialize<TransportDtlsStateChangeNotificationData>()!;
                        Data.DtlsState = notification.DtlsState;

                        if (Data.DtlsState == DtlsState.Connecting)
                        {
                            Data.DtlsRemoteCert = notification.DtlsRemoteCert;
                        }

                        Emit("dtlsstatechange", Data.DtlsState);

                        // Emit observer event.
                        Observer.Emit("dtlsstatechange", Data.DtlsState);

                        break;
                    }

                case "sctpstatechange":
                    {
                        if (data == null)
                        {
                            _logger.LogWarning($"sctpstatechange event's data is null.");
                            break;
                        }

                        var notification = data.Deserialize<TransportSctpStateChangeNotificationData>()!;
                        Data.SctpState = notification.SctpState;

                        Emit("sctpstatechange", Data.SctpState);

                        // Emit observer event.
                        Observer.Emit("sctpstatechange", Data.SctpState);

                        break;
                    }

                case "trace":
                    {
                        if (data == null)
                        {
                            _logger.LogWarning($"trace event's data is null.");
                            break;
                        }

                        var trace = data.Deserialize<TransportTraceEventData>();

                        Emit("trace", trace);

                        // Emit observer event.
                        Observer.Emit("trace", trace);

                        break;
                    }

                default:
                    {
                        _logger.LogError($"OnChannelMessage() | WebRtcTransport:{TransportId} Ignoring unknown event{@event}");
                        break;
                    }
            }
        }

        #endregion Event Handlers
    }
}
