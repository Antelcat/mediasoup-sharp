using FBS.Request;

namespace MediasoupSharp.Internals.Converters;

internal class MethodConverter : EnumStringConverter<Method>
{
    protected override IEnumerable<(Method Enum, string Text)> Map()
    {
        yield return (Method.WORKER_CLOSE, "worker.close");
        yield return (Method.WORKER_DUMP, "worker.dump");
        yield return (Method.WORKER_GET_RESOURCE_USAGE, "worker.getResourceUsage");
        yield return (Method.WORKER_UPDATE_SETTINGS, "worker.updateSettings");
        yield return (Method.WORKER_CREATE_WEBRTCSERVER, "worker.createWebRtcServer");
        yield return (Method.WORKER_CREATE_ROUTER, "worker.createRouter");
        yield return (Method.WORKER_WEBRTCSERVER_CLOSE, "worker.closeWebRtcServer");
        yield return (Method.WORKER_CLOSE_ROUTER, "worker.closeRouter");
        yield return (Method.WEBRTCSERVER_DUMP, "webRtcServer.dump");
        yield return (Method.ROUTER_DUMP, "router.dump");
        yield return (Method.ROUTER_CREATE_WEBRTCTRANSPORT, "router.createWebRtcTransport");
        yield return (Method.ROUTER_CREATE_WEBRTCTRANSPORT_WITH_SERVER, "router.createWebRtcTransportWithServer");
        yield return (Method.ROUTER_CREATE_PLAINTRANSPORT, "router.createPlainTransport");
        yield return (Method.ROUTER_CREATE_PIPETRANSPORT, "router.createPipeTransport");
        yield return (Method.ROUTER_CREATE_DIRECTTRANSPORT, "router.createDirectTransport");
        yield return (Method.ROUTER_CLOSE_TRANSPORT, "router.closeTransport");
        yield return (Method.ROUTER_CREATE_ACTIVESPEAKEROBSERVER, "router.createActiveSpeakerObserver");
        yield return (Method.ROUTER_CREATE_AUDIOLEVELOBSERVER, "router.createAudioLevelObserver");
        yield return (Method.ROUTER_CLOSE_RTPOBSERVER, "router.closeRtpObserver");
        yield return (Method.TRANSPORT_DUMP, "transport.dump");
        yield return (Method.TRANSPORT_GET_STATS, "transport.getStats");
        yield return (Method.TRANSPORT_CONNECT, "transport.connect");
        yield return (Method.TRANSPORT_SET_MAX_INCOMING_BITRATE, "transport.setMaxIncomingBitrate");
        yield return (Method.TRANSPORT_SET_MAX_OUTGOING_BITRATE, "transport.setMaxOutgoingBitrate");
        yield return (Method.TRANSPORT_SET_MIN_OUTGOING_BITRATE, "transport.setMinOutgoingBitrate");
        yield return (Method.TRANSPORT_RESTART_ICE, "transport.restartIce");
        yield return (Method.TRANSPORT_PRODUCE, "transport.produce");
        yield return (Method.TRANSPORT_PRODUCE_DATA, "transport.produceData");
        yield return (Method.TRANSPORT_CONSUME, "transport.consume");
        yield return (Method.TRANSPORT_CONSUME_DATA, "transport.consumeData");
        yield return (Method.TRANSPORT_ENABLE_TRACE_EVENT, "transport.enableTraceEvent");
        yield return (Method.TRANSPORT_CLOSE_PRODUCER, "transport.closeProducer");
        yield return (Method.TRANSPORT_CLOSE_CONSUMER, "transport.closeConsumer");
        yield return (Method.TRANSPORT_CLOSE_DATAPRODUCER, "transport.closeDataProducer");
        yield return (Method.TRANSPORT_CLOSE_DATACONSUMER, "transport.closeDataConsumer");
        yield return (Method.PLAINTRANSPORT_CONNECT, "plainTransport.connect");
        yield return (Method.PIPETRANSPORT_CONNECT, "pipeTransport.connect");
        yield return (Method.WEBRTCTRANSPORT_CONNECT, "webRtcTransport.connect");
        yield return (Method.PRODUCER_DUMP, "producer.dump");
        yield return (Method.PRODUCER_GET_STATS, "producer.getStats");
        yield return (Method.PRODUCER_PAUSE, "producer.pause");
        yield return (Method.PRODUCER_RESUME, "producer.resume");
        yield return (Method.PRODUCER_ENABLE_TRACE_EVENT, "producer.enableTraceEvent");
        yield return (Method.CONSUMER_DUMP, "consumer.dump");
        yield return (Method.CONSUMER_GET_STATS, "consumer.getStats");
        yield return (Method.CONSUMER_PAUSE, "consumer.pause");
        yield return (Method.CONSUMER_RESUME, "consumer.resume");
        yield return (Method.CONSUMER_SET_PREFERRED_LAYERS, "consumer.setPreferredLayers");
        yield return (Method.CONSUMER_SET_PRIORITY, "consumer.setPriority");
        yield return (Method.CONSUMER_REQUEST_KEY_FRAME, "consumer.requestKeyFrame");
        yield return (Method.CONSUMER_ENABLE_TRACE_EVENT, "consumer.enableTraceEvent");
        yield return (Method.DATAPRODUCER_DUMP, "dataProducer.dump");
        yield return (Method.DATAPRODUCER_GET_STATS, "dataProducer.getStats");
        yield return (Method.DATAPRODUCER_PAUSE, "dataProducer.pause");
        yield return (Method.DATAPRODUCER_RESUME, "dataProducer.resume");
        yield return (Method.DATACONSUMER_DUMP, "dataConsumer.dump");
        yield return (Method.DATACONSUMER_GET_STATS, "dataConsumer.getStats");
        yield return (Method.DATACONSUMER_PAUSE, "dataConsumer.pause");
        yield return (Method.DATACONSUMER_RESUME, "dataConsumer.resume");
        yield return (Method.DATACONSUMER_GET_BUFFERED_AMOUNT, "dataConsumer.getBufferedAmount");
        yield return (Method.DATACONSUMER_SET_BUFFERED_AMOUNT_LOW_THRESHOLD, "dataConsumer.setBufferedAmountLowThreshold");
        yield return (Method.DATACONSUMER_SEND, "dataConsumer.send");
        yield return (Method.DATACONSUMER_SET_SUBCHANNELS, "dataConsumer.setSubchannels");
        yield return (Method.DATACONSUMER_ADD_SUBCHANNEL, "dataConsumer.addSubchannel");
        yield return (Method.DATACONSUMER_REMOVE_SUBCHANNEL, "dataConsumer.removeSubchannel");
        yield return (Method.RTPOBSERVER_PAUSE, "rtpObserver.pause");
        yield return (Method.RTPOBSERVER_RESUME, "rtpObserver.resume");
        yield return (Method.RTPOBSERVER_ADD_PRODUCER, "rtpObserver.addProducer");
        yield return (Method.RTPOBSERVER_REMOVE_PRODUCER, "rtpObserver.removeProducer");
    }
}