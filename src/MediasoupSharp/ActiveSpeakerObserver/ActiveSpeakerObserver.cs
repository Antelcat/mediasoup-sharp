using MediasoupSharp.Channel;
using MediasoupSharp.RtpObserver;
using Microsoft.Extensions.Logging;

namespace MediasoupSharp.ActiveSpeakerObserver
{
    public class ActiveSpeakerObserver<TActiveSpeakerObserverAppData> : 
        RtpObserver<TActiveSpeakerObserverAppData,ActiveSpeakerObserverEvents>
    {
        
        /// <summary>
        /// Logger.
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// <para>Events:</para>
        /// <para>@emits volumes - (volumes: AudioLevelObserverVolume[])</para>
        /// <para>@emits silence</para>
        /// <para>Observer events:</para>
        /// <para>@emits close</para>
        /// <para>@emits pause</para>
        /// <para>@emits resume</para>
        /// <para>@emits addproducer - (producer: Producer)</para>
        /// <para>@emits removeproducer - (producer: Producer)</para>
        /// <para>@emits volumes - (volumes: AudioLevelObserverVolume[])</para>
        /// <para>@emits silence</para>
        /// </summary>
        /// <param name="loggerFactory"></param>
        /// <param name="internal"></param>
        /// <param name="channel"></param>
        /// <param name="payloadChannel"></param>
        /// <param name="appData"></param>
        /// <param name="getProducerById"></param>
        public ActiveSpeakerObserver(ILoggerFactory loggerFactory,
            RtpObserverInternal @internal,
            Channel.Channel channel,
            PayloadChannel.PayloadChannel payloadChannel,
            TActiveSpeakerObserverAppData? appData,
            Func<string, Task<Producer.Producer?>> getProducerById
            ) : base(loggerFactory, @internal, channel, payloadChannel, appData, getProducerById)
        {
            logger = loggerFactory.CreateLogger(GetType());
        }

        public IEnhancedEventEmitter<ActiveSpeakerObserverObserverEvents> Observer => base.Observer as IEnhancedEventEmitter<ActiveSpeakerObserverObserverEvents>;

        private void HandleWorkerNotifications()
        {
            
        }
        
#pragma warning disable VSTHRD100 // Avoid async void methods
        protected override async void OnChannelMessage(string targetId, string @event, string? data)
#pragma warning restore VSTHRD100 // Avoid async void methods
        {
            if (targetId != Internal.RtpObserverId)
            {
                return;
            }

            switch (@event)
            {
                case "dominantspeaker":
                    {
                        var notification = data!.Deserialize<ActiveSpeakerObserverNotificationData>()!;
                        
                        var producer = GetProducerById(notification.ProducerId);
                        if (producer != null)
                        {
                            var dominantSpeaker = new ActiveSpeakerObserverDominantSpeaker
                            {
                                Producer = await GetProducerById(notification.ProducerId)
                            };

                            Emit("dominantspeaker", dominantSpeaker);

                            // Emit observer event.
                            Observer.Emit("dominantspeaker", dominantSpeaker);
                        }

                        break;
                    }
                default:
                    {
                        logger.LogError($"OnChannelMessage() | Ignoring unknown event{@event}");
                        break;
                    }
            }
        }
    }
}
