﻿using MediasoupSharp.RtpParameters;
using Microsoft.Extensions.Logging;

namespace MediasoupSharp.Consumer;

public interface IConsumer
{
	string Id     { get; }
	bool   Closed { get; }

	bool Paused { get; }

	bool ProducerPaused { get; }

	MediaKind Kind { get; }

	RtpParameters.RtpParameters RtpParameters { get; }

	ConsumerType Type { get; }

	void Close();

	void TransportClosed();

	internal EnhancedEventEmitter Observer { get; }
}


internal class Consumer<TConsumerAppData> : EnhancedEventEmitter<ConsumerEvents>, IConsumer
{
	private readonly ILogger? logger;

	/// <summary>
	/// Internal data.
	/// </summary>
	private readonly ConsumerInternal @internal;

	/// <summary>
	/// Consumer data.
	/// </summary>
	private readonly ConsumerData data;

	/// <summary>
	/// Channel instance.
	/// </summary>
	private readonly Channel.Channel channel;

	/// <summary>
	/// PayloadChannel instance.
	/// </summary>
	private PayloadChannel.PayloadChannel PayloadChannel { get; set; }

	public bool Closed { get; private set; }

	/// <summary>
	/// App custom data.
	/// </summary>
	public object AppData { get; protected set; }

	public bool Paused { get; private set; }

	/// <summary>
	/// Whether the associate Producer is paused.
	/// </summary>
	public bool ProducerPaused { get; private set; }

	/// <summary>
	/// Current priority.
	/// </summary>
	public int Priority { get; private set; } = 1;

	/// <summary>
	/// Current score.
	/// </summary>
	public ConsumerScore Score { get; private set; }

	/// <summary>
	/// Preferred layers.
	/// </summary>
	public ConsumerLayers? PreferredLayers { get; private set; }

	/// <summary>
	/// Curent layers.
	/// </summary>
	public ConsumerLayers? CurrentLayers { get; private set; }

	/// <summary>
	/// Observer instance.
	/// </summary>
	public EnhancedEventEmitter Observer { get; }

	public Consumer(
		ConsumerInternal @internal,
		ConsumerData data,
		Channel.Channel channel,
		PayloadChannel.PayloadChannel payloadChannel,
		TConsumerAppData? appData,
		bool paused,
		bool producerPaused,
		ConsumerScore? score = null,
		ConsumerLayers? preferredLayers = null,
		ILoggerFactory? loggerFactory = null
	) : base(loggerFactory)
	{
		logger          = loggerFactory?.CreateLogger(GetType());
		this.@internal  = @internal;
		this.data       = data;
		this.channel    = channel;
		PayloadChannel  = payloadChannel;
		AppData         = appData ?? typeof(TConsumerAppData).New<TConsumerAppData>()!;
		Paused          = paused;
		ProducerPaused  = producerPaused;
		Score           = score!;
		PreferredLayers = preferredLayers;
		Observer        = new EnhancedEventEmitter<ConsumerObserverEvents>(loggerFactory);

		HandleWorkerNotifications();
	}

	public string Id => @internal.ConsumerId;

	public string ProducerId => data.ProducerId;

	public MediaKind Kind => data.Kind;

	public RtpParameters.RtpParameters RtpParameters => data.RtpParameters;

	public ConsumerType Type => data.Type;

	internal Channel.Channel ChannelForTesting => channel;

	/// <summary>
	/// Close the Producer.
	/// </summary>
	public void Close()
	{
		if (Closed)
		{
			return;
		}

		logger?.LogDebug("CloseAsync() | Consumer:{Id}", Id);

		Closed = true;

		// Remove notification subscriptions.
		channel.RemoveAllListeners(@internal.ConsumerId);
		PayloadChannel.RemoveAllListeners(@internal.ConsumerId);

		//TODO : Check Naming
		var reqData = new { consumerId = @internal.ConsumerId };

		// Fire and forget
		channel.Request("transport.closeConsumer", @internal.TransportId, reqData)
			.ContinueWith(_ => { }, TaskContinuationOptions.OnlyOnFaulted);

		_ = Emit("@close");

		// Emit observer event.
		_ = Observer.SafeEmit("close");
	}

	/// <summary>
	/// Transport was closed.
	/// </summary>
	public void TransportClosed()
	{
		if (Closed)
		{
			return;
		}

		logger?.LogDebug("TransportClosed() | Consumer:{Id}", Id);

		Closed = true;

		// Remove notification subscriptions.
		channel.RemoveAllListeners(@internal.ConsumerId);
		PayloadChannel.RemoveAllListeners(@internal.ConsumerId);

		_ = SafeEmit("transportclose");

		// Emit observer event.
		_ = Observer.SafeEmit("close");
	}

	/// <summary>
	/// Dump DataProducer.
	/// </summary>
	public async Task<object> DumpAsync()
	{
		logger?.LogDebug("DumpAsync() | Consumer:{Id}", Id);

		return (await channel.Request("consumer.dump", @internal.ConsumerId))!;
	}

	/// <summary>
	/// Get DataProducer stats.
	/// </summary>
	public async Task<List<object>> GetStatsAsync()
	{
		logger?.LogDebug("GetStatsAsync() | Consumer:{Id}", Id);

		return (await channel.Request("consumer.getStats", @internal.ConsumerId) as List<object>)!;
	}

	/// <summary>
	/// Pause the Consumer.
	/// </summary>
	public async Task PauseAsync()
	{
		logger?.LogDebug("PauseAsync() | Consumer:{Id}", Id);

		var wasPaused = Paused || ProducerPaused;

		// Fire and forget
		await channel.Request("consumer.pause", @internal.ConsumerId);

		Paused = true;

		// Emit observer event.
		if (!wasPaused)
		{
			_ = Observer.SafeEmit("pause");
		}
	}

	/// <summary>
	/// Resume the Consumer.
	/// </summary>
	public async Task ResumeAsync()
	{
		logger?.LogDebug("ResumeAsync() | Consumer:{Id}", Id);

		var wasPaused = Paused || ProducerPaused;

		// Fire and forget
		await channel.Request("consumer.resume", @internal.ConsumerId);

		Paused = false;

		// Emit observer event.
		if (wasPaused && !ProducerPaused)
		{
			_ = Observer.SafeEmit("resume");
		}
	}

	/// <summary>
	/// Set preferred video layers.
	/// </summary>
	public async Task SetPreferredLayersAsync(ConsumerLayers consumerLayers)
	{
		logger?.LogDebug("SetPreferredLayersAsync() | Consumer:{Id}", Id);

		//TODO : Naming
		var reqData = new { spatialLayer = consumerLayers.SpatialLayer, temporalLayer = consumerLayers.TemporalLayer };

		var data = await channel.Request("consumer.setPreferredLayers", @internal.ConsumerId, reqData);
		PreferredLayers = data as ConsumerLayers;
	}

	/// <summary>
	/// Set priority.
	/// </summary>
	public async Task SetPriorityAsync(int priority)
	{
		logger?.LogDebug("SetPriorityAsync() | Consumer:{Id}", Id);

		//TODO : Check Naming
		var reqData = new { Priority = priority };
		var data    = await channel.Request("consumer.setPriority", @internal.ConsumerId, reqData);
		Priority = ((dynamic)data!).priority;
	}

	/// <summary>
	/// Unset priority.
	/// </summary>
	public async Task UnsetPriorityAsync()
	{
		logger?.LogDebug("UnsetPriorityAsync() | Consumer:{Id}", Id);

		//TODO : Check Naming
		var reqData = new { Priority = 1 };
		var data    = await channel.Request("consumer.setPriority", @internal.ConsumerId, reqData);

		Priority = ((dynamic)data!).priority;
	}

	/// <summary>
	/// Request a key frame to the Producer.
	/// </summary>
	public async Task RequestKeyFrameAsync()
	{
		logger?.LogDebug("RequestKeyFrameAsync() | Consumer:{Id}", Id);

		await channel.Request("consumer.requestKeyFrame", @internal.ConsumerId);
	}

	/// <summary>
	/// Enable "trace" event.
	/// </summary>
	public async Task EnableTraceEventAsync(ConsumerTraceEventType[] types)
	{
		logger?.LogDebug("EnableTraceEventAsync() | Consumer:{Id}", Id);

		var reqData = new
		{
			Types = types
		};

		// Fire and forget
		await channel.Request("consumer.enableTraceEvent", @internal.ConsumerId, reqData);
	}


	private void HandleWorkerNotifications()
	{
		channel.On(@internal.ConsumerId, async args =>
		{
			var @event = args![0];
			var data   = args[1];
			switch (@event)
			{
				case "producerclose":
				{
					if (Closed)
					{
						break;
					}

					Closed = true;

					// Remove notification subscriptions.
					channel.RemoveAllListeners(@internal.ConsumerId);
					PayloadChannel.RemoveAllListeners(@internal.ConsumerId);

					await Emit("@producerclose");
					await SafeEmit("producerclose");

					// Emit observer event.
					await Observer.SafeEmit("close");

					break;
				}

				case "producerpause":
				{
					if (ProducerPaused)
					{
						break;
					}

					var wasPaused = Paused || ProducerPaused;

					ProducerPaused = true;

					await SafeEmit("producerpause");

					// Emit observer event.
					if (!wasPaused)
					{
						await Observer.SafeEmit("pause");
					}

					break;
				}

				case "producerresume":
				{
					if (!ProducerPaused)
					{
						break;
					}

					var wasPaused = Paused || ProducerPaused;

					ProducerPaused = false;

					await SafeEmit("producerresume");

					// Emit observer event.
					if (wasPaused && !Paused)
					{
						await Observer.SafeEmit("resume");
					}

					break;
				}

				case "score":
				{
					var score = (data as ConsumerScore)!;

					Score = score;

					await SafeEmit("score", score);

					// Emit observer event.
					await SafeEmit("score", score);

					break;
				}

				case "layerschange":
				{
					var layers = (data as ConsumerLayers)!;

					CurrentLayers = layers;

					await SafeEmit("layerschange", layers);

					// Emit observer event.
					await Observer.SafeEmit("layerschange", layers);

					break;
				}

				case "trace":
				{
					var trace = data as ConsumerTraceEventData;

					_ = SafeEmit("trace", trace);

					// Emit observer event.
					_ = Observer.SafeEmit("trace", trace);

					break;
				}

				default:
				{
					logger?.LogError("ignoring unknown event  ' %s'", @event);
					break;
				}
			}
		});

		PayloadChannel.On(@internal.ConsumerId, async args =>
		{
			var @event  = (string)args![0];
			var data    = args[1];
			var payload = (byte[])args[2];
			switch (@event)
			{
				case "rtp":
				{
					if (Closed)
					{
						break;
					}

					var packet = payload;

					await SafeEmit("rtp", packet);

					break;
				}

				default:
				{
					logger?.LogError("ignoring unknown event {E}", @event);
					break;
				}
			}
		});
	}

}

