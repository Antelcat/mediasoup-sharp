using FBS.RtpParameters;
using FBS.WebRtcTransport;
using Antelcat.MediasoupSharp.ClientRequest;
using Antelcat.MediasoupSharp.Meeting.Exceptions;
using Antelcat.MediasoupSharp.Meeting.Models;
using Antelcat.MediasoupSharp.Meeting.Settings;
using Antelcat.MediasoupSharp.Meeting.SignalR.Models;
using Antelcat.MediasoupSharp.Meeting.SignalR.Services;
using Antelcat.MediasoupSharp.RtpParameters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Antelcat.MediasoupSharp.Meeting.SignalR
{
    [Authorize]
    public partial class MeetingHub(
        ILogger<MeetingHub> logger,
        IHubContext<MeetingHub, IHubClient> hubContext,
        BadDisconnectSocketService badDisconnectSocketService,
        Scheduler scheduler,
        MeetingServerOptions meetingServerOptions)
        : Hub<IHubClient>
    {
        private string UserId => Context.UserIdentifier!;

        private string ConnectionId => Context.ConnectionId;

        public override async Task OnConnectedAsync()
        {
            await LeaveAsync();
            badDisconnectSocketService.CacheContext(Context);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await LeaveAsync();
            await base.OnDisconnectedAsync(exception);
        }

        #region Private

        private async Task LeaveAsync()
        {
            try
            {
                var leaveResult = await scheduler.LeaveAsync(UserId);
                if(leaveResult != null)
                {
                    // Notification: peerLeaveRoom
                    SendNotification(leaveResult.OtherPeerIds, "peerLeaveRoom", new PeerLeaveRoomNotification
                    {
                        PeerId = leaveResult.SelfPeer.PeerId
                    });
                    badDisconnectSocketService.DisconnectClient(leaveResult.SelfPeer.ConnectionId);
                }
            }
            catch(MeetingException ex)
            {
                logger.LogError("LeaveAsync 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "LeaveAsync 调用失败.");
            }
        }

        #endregion Private
    }

    public partial class MeetingHub
    {
        public MeetingMessage<object> GetServeMode()
        {
            return MeetingMessage<object>.Success(new { meetingServerOptions.ServeMode }, "GetServeMode 成功");
        }

        #region Room

        /// <summary>
        /// Get RTP capabilities of router.
        /// </summary>
        ///
        public MeetingMessage<RtpCapabilities> GetRouterRtpCapabilities()
        {
            var rtpCapabilities = scheduler.DefaultRtpCapabilities;
            return MeetingMessage<RtpCapabilities>.Success(rtpCapabilities, "GetRouterRtpCapabilities 成功");
        }

        /// <summary>
        /// Join meeting.
        /// </summary>
        public async Task<MeetingMessage> Join(JoinRequest joinRequest)
        {
            try
            {
                var client = hubContext.Clients.User(UserId);
                await scheduler.JoinAsync(UserId, ConnectionId, client, joinRequest);
                return MeetingMessage.Success("Join 成功");
            }
            catch(MeetingException ex)
            {
                logger.LogError("Join 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Join 调用失败.");
            }

            return MeetingMessage.Failure("Join 失败");
        }

        /// <summary>
        /// Join room.
        /// </summary>
        public async Task<MeetingMessage<JoinRoomResponse>> JoinRoom(JoinRoomRequest joinRoomRequest)
        {
            try
            {
                // FIXME: (alby) 明文告知用户进入房间的 Role 存在安全问题, 特别是 Invite 模式下。
                var joinRoomResult = await scheduler.JoinRoomAsync(UserId, ConnectionId, joinRoomRequest);

                // 将自身的信息告知给房间内的其他人
                var otherPeerIds = joinRoomResult.Peers.Select(m => m.PeerId).Where(m => m != joinRoomResult.SelfPeer.PeerId).ToArray();
                // Notification: peerJoinRoom
                SendNotification(otherPeerIds, "peerJoinRoom", new PeerJoinRoomNotification
                {
                    Peer = joinRoomResult.SelfPeer
                });

                // 返回包括自身的房间内的所有人的信息
                var data = new JoinRoomResponse
                {
                    Peers = joinRoomResult.Peers,
                };
                return MeetingMessage<JoinRoomResponse>.Success(data, "JoinRoom 成功");
            }
            catch(MeetingException ex)
            {
                logger.LogError("JoinRoom 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "JoinRoom 调用失败.");
            }

            return MeetingMessage<JoinRoomResponse>.Failure("JoinRoom 失败");
        }

        /// <summary>
        /// Leave room.
        /// </summary>
        ///
        public async Task<MeetingMessage> LeaveRoom()
        {
            try
            {
                // FIXME: (alby) 在 Invite 模式下，清除尚未处理的邀请。避免在会议室A受邀请后，离开会议室A进入会议室B，误受邀请。
                var leaveRoomResult = await scheduler.LeaveRoomAsync(UserId, ConnectionId);

                // Notification: peerLeaveRoom
                SendNotification(leaveRoomResult.OtherPeerIds, "peerLeaveRoom", new PeerLeaveRoomNotification
                {
                    PeerId = UserId
                });

                return MeetingMessage.Success("LeaveRoom 成功");
            }
            catch(MeetingException ex)
            {
                logger.LogError("LeaveRoom 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "LeaveRoom 调用失败.");
            }

            return MeetingMessage.Failure("LeaveRoom 失败");
        }

        #endregion

        #region Transport

        /// <summary>
        /// Create send WebRTC transport.
        /// </summary>
        public Task<MeetingMessage<CreateWebRtcTransportResult>> CreateSendWebRtcTransport(CreateWebRtcTransportRequest createWebRtcTransportRequest)
        {
            return CreateWebRtcTransportAsync(createWebRtcTransportRequest, true);
        }

        /// <summary>
        /// Create recv WebRTC transport.
        /// </summary>
        public Task<MeetingMessage<CreateWebRtcTransportResult>> CreateRecvWebRtcTransport(CreateWebRtcTransportRequest createWebRtcTransportRequest)
        {
            return CreateWebRtcTransportAsync(createWebRtcTransportRequest, false);
        }

        /// <summary>
        /// Create WebRTC transport.
        /// </summary>
        private async Task<MeetingMessage<CreateWebRtcTransportResult>> CreateWebRtcTransportAsync(CreateWebRtcTransportRequest createWebRtcTransportRequest, bool isSend)
        {
            try
            {
                var transport = await scheduler.CreateWebRtcTransportAsync(UserId, ConnectionId, createWebRtcTransportRequest, isSend);
                transport.On("sctpstatechange", obj =>
                {
                    logger.LogDebug("WebRtcTransport \"sctpstatechange\" event [sctpState:{SctpState}]", obj);
                    return Task.CompletedTask;
                });

                transport.On("dtlsstatechange", obj =>
                {
                    var dtlsState = (DtlsState)obj!;
                    if(dtlsState is DtlsState.FAILED or DtlsState.CLOSED)
                    {
                        logger.LogWarning("WebRtcTransport dtlsstatechange event [dtlsState:{SctpState}]", obj);
                    }

                    return Task.CompletedTask;
                });

                // NOTE: For testing.
                //await transport.EnableTraceEventAsync(new[] { TransportTraceEventType.Probation, TransportTraceEventType.BWE });
                //await transport.EnableTraceEventAsync(new[] { TransportTraceEventType.BWE });

                var peerId = UserId;
                transport.On("trace", obj =>
                {
                    // TODO: Fix this
                    // var traceData = (TransportTraceEventData)obj!;
                    // _logger.LogDebug($"transport \"trace\" event [transportId:{transport.TransportId}, trace:{traceData.Type.GetEnumMemberValue()}]");

                    // if(traceData.Type == TransportTraceEventType.BWE && traceData.Direction == TraceEventDirection.Out)
                    // {
                    //     // Notification: downlinkBwe
                    //     SendNotification(peerId, "downlinkBwe", new
                    //     {
                    //         DesiredBitrate = traceData.Info["desiredBitrate"],
                    //         EffectiveDesiredBitrate = traceData.Info["effectiveDesiredBitrate"],
                    //         AvailableBitrate = traceData.Info["availableBitrate"]
                    //     });
                    // }
                    return Task.CompletedTask;
                });

                return MeetingMessage<CreateWebRtcTransportResult>.Success(new CreateWebRtcTransportResult
                {
                    TransportId = transport.Id,
                    IceParameters = transport.Data.IceParameters,
                    IceCandidates = transport.Data.IceCandidates,
                    DtlsParameters = transport.Data.DtlsParameters,
                    SctpParameters = transport.Data.Base.SctpParameters,
                },
                "CreateWebRtcTransport 成功");
            }
            catch(MeetingException ex)
            {
                logger.LogError("CreateWebRtcTransportAsync 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "CreateWebRtcTransportAsync 调用失败.");
            }

            return MeetingMessage<CreateWebRtcTransportResult>.Failure("CreateWebRtcTransportAsync 失败");
        }

        /// <summary>
        /// Connect WebRTC transport.
        /// </summary>
        public async Task<MeetingMessage> ConnectWebRtcTransport(ConnectWebRtcTransportRequest connectWebRtcTransportRequest)
        {
            try
            {
                if(await scheduler.ConnectWebRtcTransportAsync(UserId, ConnectionId, connectWebRtcTransportRequest))
                {
                    return MeetingMessage.Success("ConnectWebRtcTransport 成功");
                }
            }
            catch(MeetingException ex)
            {
                logger.LogError("ConnectWebRtcTransport 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "ConnectWebRtcTransport 调用失败.");
            }

            return MeetingMessage.Failure($"ConnectWebRtcTransport 失败: TransportId: {connectWebRtcTransportRequest.TransportId}");
        }

        public async Task<MeetingMessage> Ready()
        {
            if(meetingServerOptions.ServeMode == ServeMode.Pull)
            {
                return MeetingMessage.Failure("Ready 失败(无需调用)");
            }

            try
            {
                var otherPeers = await scheduler.GetOtherPeersAsync(UserId, ConnectionId);
                foreach(var producerPeer in otherPeers.Where(m => m.PeerId != UserId))
                {
                    var producers = await producerPeer.GetProducersASync();
                    foreach(var producer in producers.Values)
                    {
                        // 本 Peer 消费其他 Peer
                        CreateConsumer(UserId, producerPeer.PeerId, producer).ContinueWithOnFaultedHandleLog(logger);
                    }
                }

                return MeetingMessage.Success("Ready 成功");
            }
            catch(MeetingException ex)
            {
                logger.LogError("Ready 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Ready 调用失败.");
            }

            return MeetingMessage.Failure("Ready 失败");
        }

        #endregion

        #region Pull mode

        /// <summary>
        /// Pull medias.
        /// </summary>
        public async Task<MeetingMessage> Pull(PullRequest pullRequest)
        {
            if(meetingServerOptions.ServeMode != ServeMode.Pull)
            {
                throw new NotSupportedException($"Not supported on \"{meetingServerOptions.ServeMode}\" mode. Needs \"{ServeMode.Pull}\" mode.");
            }

            try
            {
                var pullResult = await scheduler.PullAsync(UserId, ConnectionId, pullRequest);
                var consumerPeer = pullResult.ConsumePeer;
                var producerPeer = pullResult.ProducePeer;

                foreach(var producer in pullResult.ExistsProducers)
                {
                    // 本 Peer 消费其他 Peer
                    CreateConsumer(consumerPeer.PeerId, producerPeer.PeerId, producer).ContinueWithOnFaultedHandleLog(logger);
                }

                if(pullResult.Sources.Any())
                {
                    // Notification: produceSources
                    SendNotification(pullRequest.PeerId, "produceSources", new ProduceSourcesNotification
                    {
                        Sources = pullResult.Sources
                    });
                }

                return MeetingMessage.Success("Pull 成功");
            }
            catch(MeetingException ex)
            {
                logger.LogError("Pull 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Pull 调用失败.");
            }

            return MeetingMessage.Failure("Pull 失败");
        }

        #endregion

        #region Invite mode

        /// <summary>
        /// Invite medias.
        /// </summary>
        public async Task<MeetingMessage> Invite(InviteRequest inviteRequest)
        {
            if(meetingServerOptions.ServeMode != ServeMode.Invite)
            {
                throw new NotSupportedException($"Not supported on \"{meetingServerOptions.ServeMode}\" mode. Needs \"{ServeMode.Invite}\" mode.");
            }

            try
            {
                // 仅会议室管理员可以邀请。
                if(await scheduler.GetPeerRoleAsync(UserId, ConnectionId) != UserRole.Admin)
                {
                    return MeetingMessage.Failure("仅管理员可发起邀请。");
                }

                // 管理员无需邀请自己。
                if(inviteRequest.PeerId == UserId)
                {
                    return MeetingMessage.Failure("管理员请勿邀请自己。");
                }

                if(!inviteRequest.Sources.Any() || inviteRequest.Sources.Any(m => m.IsNullOrWhiteSpace()))
                {
                    return MeetingMessage.Failure("Sources 参数缺失或非法。");
                }

                // NOTE: 暂未校验被邀请方是否有对应的 Source 。不过就算接收邀请也无法生产。

                var setPeerInternalDataRequest = new SetPeerInternalDataRequest
                {
                    PeerId = inviteRequest.PeerId,
                    InternalData = new Dictionary<string, object>()
                };
                foreach(var source in inviteRequest.Sources)
                {
                    setPeerInternalDataRequest.InternalData[$"Invite:{source}"] = true;
                }

                await scheduler.SetPeerInternalDataAsync(setPeerInternalDataRequest);

                // NOTE：如果对应 Source 已经在生产中，是否允许重复邀请？
                // Notification: produceSources
                SendNotification(inviteRequest.PeerId, "produceSources", new ProduceSourcesNotification
                {
                    Sources = inviteRequest.Sources
                });

                return MeetingMessage.Success("Invite 成功");
            }
            catch(MeetingException ex)
            {
                logger.LogError("Invite 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Invite 调用失败.");
            }

            return MeetingMessage.Failure("Invite 失败");
        }

        /// <summary>
        /// Deinvite medias.
        /// </summary>
        public async Task<MeetingMessage> Deinvite(DeinviteRequest deinviteRequest)
        {
            if(meetingServerOptions.ServeMode != ServeMode.Invite)
            {
                throw new NotSupportedException($"Not supported on \"{meetingServerOptions.ServeMode}\" mode. Needs \"{ServeMode.Invite}\" mode.");
            }

            try
            {
                // 仅会议室管理员可以取消邀请。
                if(await scheduler.GetPeerRoleAsync(UserId, ConnectionId) != UserRole.Admin)
                {
                    return MeetingMessage.Failure("仅管理员可取消邀请。");
                }

                // 管理员无需取消邀请自己。
                if(deinviteRequest.PeerId == UserId)
                {
                    return MeetingMessage.Failure("管理员请勿取消邀请自己。");
                }

                if(!deinviteRequest.Sources.Any() || deinviteRequest.Sources.Any(m => m.IsNullOrWhiteSpace()))
                {
                    return MeetingMessage.Failure("Sources 参数缺失或非法。");
                }

                // NOTE: 暂未校验被邀请方是否有对应的 Source 。也未校验对应 Source 是否收到邀请。

                var unSetPeerInternalDataRequest = new UnsetPeerInternalDataRequest
                {
                    PeerId = deinviteRequest.PeerId,
                };

                var keys = new List<string>();
                foreach(var source in deinviteRequest.Sources)
                {
                    keys.Add($"Invite:{source}");
                }

                unSetPeerInternalDataRequest.Keys = keys.ToArray();

                await scheduler.UnsetPeerInternalDataAsync(unSetPeerInternalDataRequest);

                await scheduler.CloseProducerWithSourcesAsync(UserId, ConnectionId, deinviteRequest.PeerId, deinviteRequest.Sources);

                // Notification: closeSources
                SendNotification(deinviteRequest.PeerId, "closeSources", new CloseSourcesNotification
                {
                    Sources = deinviteRequest.Sources
                });

                return MeetingMessage.Success("Deinvite 成功");
            }
            catch(MeetingException ex)
            {
                logger.LogError("Deinvite 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Deinvite 调用失败.");
            }

            return MeetingMessage.Failure("Deinvite 失败");
        }

        /// <summary>
        /// Request produce medias.
        /// </summary>
        public async Task<MeetingMessage> RequestProduce(RequestProduceRequest requestProduceRequest)
        {
            if(meetingServerOptions.ServeMode != ServeMode.Invite)
            {
                throw new NotSupportedException($"Not supported on \"{meetingServerOptions.ServeMode}\" mode. Needs \"{ServeMode.Invite}\" mode.");
            }

            // 管理员无需发出申请。
            if(await scheduler.GetPeerRoleAsync(UserId, ConnectionId) == UserRole.Admin)
            {
                return MeetingMessage.Failure("管理员无需发出申请。");
            }

            try
            {
                if(!requestProduceRequest.Sources.Any() || requestProduceRequest.Sources.Any(m => m.IsNullOrWhiteSpace()))
                {
                    return MeetingMessage.Failure("RequestProduce 失败: Sources 参数缺失或非法。");
                }

                // NOTE: 暂未校验被邀请方是否有对应的 Source 。不过就算接收邀请也无法生产。

                var adminIds = await scheduler.GetOtherPeerIdsAsync(UserId, ConnectionId, UserRole.Admin);

                // Notification: requestProduce
                SendNotification(adminIds, "requestProduce", new RequestProduceNotification
                {
                    PeerId = UserId,
                    Sources = requestProduceRequest.Sources
                });

                return MeetingMessage.Success("RequestProduce 成功");
            }
            catch(MeetingException ex)
            {
                logger.LogError("RequestProduce 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "RequestProduce 调用失败.");
            }

            return MeetingMessage.Failure("RequestProduce 失败");
        }

        #endregion

        #region Producer

        /// <summary>
        /// Produce media.
        /// </summary>
        public async Task<MeetingMessage<ProduceRespose>> Produce(ProduceRequest produceRequest)
        {
            // HACK: (alby) Android 传入 RtpParameters 有误的临时处理方案。See: https://mediasoup.discourse.group/t/audio-codec-channel-not-supported/1877
            if(produceRequest.Kind == MediaKind.AUDIO && produceRequest.RtpParameters.Codecs[0].MimeType == "audio/opus")
            {
                produceRequest.RtpParameters.Codecs[0].Channels = 2;
            }

            try
            {
                var peerId = UserId;
                // 在 Invite 模式下如果不是管理员需校验 Source 是否被邀请。
                if(meetingServerOptions.ServeMode == ServeMode.Invite && await scheduler.GetPeerRoleAsync(UserId, ConnectionId) != UserRole.Admin)
                {
                    var internalData = await scheduler.GetPeerInternalDataAsync(UserId, ConnectionId);
                    var inviteKey = $"Invite:{produceRequest.Source}";
                    if(!internalData.InternalData.TryGetValue(inviteKey, out var inviteValue))
                    {
                        return MeetingMessage<ProduceRespose>.Failure("未受邀请的生产。");
                    }

                    // 清除邀请状态
                    await scheduler.UnsetPeerInternalDataAsync(new UnsetPeerInternalDataRequest
                    {
                        PeerId = UserId,
                        Keys = [inviteKey]
                    });
                }

                var produceResult = await scheduler.ProduceAsync(peerId, ConnectionId, produceRequest);

                var producerPeer = produceResult.ProducerPeer;
                var producer = produceResult.Producer;
                var otherPeers = meetingServerOptions.ServeMode == ServeMode.Pull ?
                    produceResult.PullPaddingConsumerPeers : await producerPeer.GetOtherPeersAsync();

                foreach(var consumerPeer in otherPeers)
                {
                    // 其他 Peer 消费本 Peer
                    CreateConsumer(consumerPeer.PeerId, producerPeer.PeerId, producer).ContinueWithOnFaultedHandleLog(logger);
                }

                // NOTE: For Testing
                //CreateConsumer(producerPeer, producerPeer, producer, "1").ContinueWithOnFaultedHandleLog(_logger);

                // Set Producer events.
                producer.On("score", obj =>
                {
                    // Notification: producerScore
                    SendNotification(peerId, "producerScore", new ProducerScoreNotification
                    {
                        ProducerId = producer.Id,
                        Score = obj
                    });
                    return Task.CompletedTask;
                });

                producer.On("videoorientationchange", obj =>
                {
                    // For Testing
                    //var videoOrientation= (ProducerVideoOrientation?)data;

                    // Notification: producerVideoOrientationChanged
                    SendNotification(peerId, "producerVideoOrientationChanged", new ProducerVideoOrientationChangedNotification
                    {
                        ProducerId = producer.Id,
                        VideoOrientation = obj
                    });
                    return Task.CompletedTask;
                });

                // NOTE: For testing.
                // await producer.enableTraceEvent([ 'rtp', 'keyframe', 'nack', 'pli', 'fir' ]);
                // await producer.enableTraceEvent([ 'pli', 'fir' ]);
                // await producer.enableTraceEvent([ 'keyframe' ]);

                producer.On("trace", obj =>
                {
                    logger.LogDebug("producer \"trace\" event [producerId:{ProducerId}, trace:{Trace}]", producer.Id, obj);
                    return Task.CompletedTask;
                });

                producer.Observer.On("close", _ =>
                {
                    // Notification: producerClosed
                    SendNotification(peerId, "producerClosed", new ProducerClosedNotification
                    {
                        ProducerId = producer.Id
                    });
                    return Task.CompletedTask;
                });

                return MeetingMessage<ProduceRespose>.Success(new ProduceRespose
                {
                    Id = producer.Id,
                    Source = produceRequest.Source
                },
                    "Produce 成功");
            }
            catch(MeetingException ex)
            {
                logger.LogError("Produce 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Produce 调用失败.");
            }

            return MeetingMessage<ProduceRespose>.Failure("Produce 失败");
        }

        /// <summary>
        /// Close producer.
        /// </summary>
        public async Task<MeetingMessage> CloseProducer(string producerId)
        {
            try
            {
                if(await scheduler.CloseProducerAsync(UserId, ConnectionId, producerId))
                {
                    return MeetingMessage.Success("CloseProducer 成功");
                }
            }
            catch(MeetingException ex)
            {
                logger.LogError("CloseProducer 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "CloseProducer 调用失败.");
            }

            return MeetingMessage.Failure("CloseProducer 失败");
        }

        /// <summary>
        /// Pause producer.
        /// </summary>
        public async Task<MeetingMessage> PauseProducer(string producerId)
        {
            try
            {
                if(await scheduler.PauseProducerAsync(UserId, ConnectionId, producerId))
                {
                    return MeetingMessage.Success("PauseProducer 成功");
                }
            }
            catch(MeetingException ex)
            {
                logger.LogError("PauseProducer 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "PauseProducer 调用失败.");
            }

            return MeetingMessage.Failure("CloseProducer 失败");
        }

        /// <summary>
        /// Resume producer.
        /// </summary>
        public async Task<MeetingMessage> ResumeProducer(string producerId)
        {
            try
            {
                if(await scheduler.ResumeProducerAsync(UserId, ConnectionId, producerId))
                {
                    return MeetingMessage.Success("ResumeProducer 成功");
                }
            }
            catch(MeetingException ex)
            {
                logger.LogError("CloseProducer 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "ResumeProducer 调用失败.");
            }

            return MeetingMessage.Failure("CloseProducer 失败");
        }

        #endregion

        #region Consumer

        /// <summary>
        /// Close consumer.
        /// </summary>
        public async Task<MeetingMessage> CloseConsumer(string consumerId)
        {
            try
            {
                if(await scheduler.CloseConsumerAsync(UserId, ConnectionId, consumerId))
                {
                    return MeetingMessage.Success("CloseConsumer 成功");
                }
            }
            catch(MeetingException ex)
            {
                logger.LogError("CloseConsumer 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "CloseConsumer 调用失败.");
            }

            return MeetingMessage.Failure("CloseConsumer 失败");
        }

        /// <summary>
        /// Pause consumer.
        /// </summary>
        public async Task<MeetingMessage> PauseConsumer(string consumerId)
        {
            try
            {
                if(await scheduler.PauseConsumerAsync(UserId, ConnectionId, consumerId))
                {
                    return MeetingMessage.Success("PauseConsumer 成功");
                }
            }
            catch(MeetingException ex)
            {
                logger.LogError("PauseConsumer 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "PauseConsumer 调用失败.");
            }

            return MeetingMessage.Failure("PauseConsumer 失败");
        }

        /// <summary>
        /// Resume consumer.
        /// </summary>
        public async Task<MeetingMessage> ResumeConsumer(string consumerId)
        {
            try
            {
                var consumer = await scheduler.ResumeConsumerAsync(UserId, ConnectionId, consumerId);
                if(consumer != null)
                {
                    // Notification: consumerScore
                    SendNotification(UserId, "consumerScore", new ConsumerScoreNotification
                    {
                        ConsumerId = consumer.Id,
                        Score = consumer.Score
                    });
                }

                return MeetingMessage.Success("ResumeConsumer 成功");
            }
            catch(MeetingException ex)
            {
                logger.LogError("ResumeConsumer 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "ResumeConsumer 调用失败.");
            }

            return MeetingMessage.Failure("ResumeConsumer 失败");
        }

        /// <summary>
        /// Set consumer's preferredLayers.
        /// </summary>
        public async Task<MeetingMessage> SetConsumerPreferedLayers(SetConsumerPreferredLayersRequest setConsumerPreferedLayersRequest)
        {
            try
            {
                if(await scheduler.SetConsumerPreferedLayersAsync(UserId, ConnectionId, setConsumerPreferedLayersRequest))
                {
                    return MeetingMessage.Success("SetConsumerPreferredLayers 成功");
                }
            }
            catch(MeetingException ex)
            {
                logger.LogError("SetConsumerPreferredLayers 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "SetConsumerPreferredLayers 调用失败.");
            }

            return MeetingMessage.Failure("SetConsumerPreferredLayers 失败");
        }

        /// <summary>
        /// Set consumer's priority.
        /// </summary>
        public async Task<MeetingMessage> SetConsumerPriority(SetConsumerPriorityRequest setConsumerPriorityRequest)
        {
            try
            {
                if(await scheduler.SetConsumerPriorityAsync(UserId, ConnectionId, setConsumerPriorityRequest))
                {
                    return MeetingMessage.Success("SetConsumerPriority 成功");
                }
            }
            catch(MeetingException ex)
            {
                logger.LogError("SetConsumerPriority 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "SetConsumerPriority 调用失败.");
            }

            return MeetingMessage.Failure("SetConsumerPreferredLayers 失败");
        }

        /// <summary>
        /// Request key-frame.
        /// </summary>
        public async Task<MeetingMessage> RequestConsumerKeyFrame(string consumerId)
        {
            try
            {
                if(await scheduler.RequestConsumerKeyFrameAsync(UserId, ConnectionId, consumerId))
                {
                    return MeetingMessage.Success("RequestConsumerKeyFrame 成功");
                }
            }
            catch(MeetingException ex)
            {
                logger.LogError("RequestConsumerKeyFrame 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "RequestConsumerKeyFrame 调用失败.");
            }

            return MeetingMessage.Failure("RequestConsumerKeyFrame 失败");
        }

        #endregion

        #region Stats

        /// <summary>
        /// Get transport's state.
        /// </summary>
        public async Task<MeetingMessage<object[]>> GetWebRtcTransportStats(string transportId)
        {
            try
            {
                var data = await scheduler.GetWebRtcTransportStatsAsync(UserId, ConnectionId, transportId);
                return data == null
                    ? MeetingMessage<object[]>.Failure("GetWebRtcTransportStats 失败")
                    : MeetingMessage<object[]>.Success(data, "GetWebRtcTransportStats 成功");
            }
            catch(MeetingException ex)
            {
                logger.LogError("GetWebRtcTransportStats 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "GetWebRtcTransportStats 调用失败.");
            }

            return MeetingMessage<object[]>.Failure("GetWebRtcTransportStats 失败");
        }

        /// <summary>
        /// Get producer's state.
        /// </summary>
        public async Task<MeetingMessage<object[]>> GetProducerStats(string producerId)
        {
            try
            {
                var data = await scheduler.GetProducerStatsAsync(UserId, ConnectionId, producerId);
                return data == null
                    ? MeetingMessage<object[]>.Failure("GetProducerStats 失败")
                    : MeetingMessage<object[]>.Success(data, "GetProducerStats 成功");
            }
            catch(MeetingException ex)
            {
                logger.LogError("GetProducerStats 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "GetProducerStats 调用失败.");
            }

            return MeetingMessage<object[]>.Failure("GetProducerStats 失败");
        }

        /// <summary>
        /// Get consumer's state.
        /// </summary>
        public async Task<MeetingMessage<object[]>> GetConsumerStats(string consumerId)
        {
            try
            {
                var data = await scheduler.GetConsumerStatsAsync(UserId, ConnectionId, consumerId);
                return data == null
                    ? MeetingMessage<object[]>.Failure("GetConsumerStats 失败")
                    : MeetingMessage<object[]>.Success(data, "GetConsumerStats 成功");
            }
            catch(MeetingException ex)
            {
                logger.LogError("GetConsumerStats 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "GetConsumerStats 调用失败.");
            }

            return MeetingMessage<object[]>.Failure("GetConsumerStats 失败");
        }

        /// <summary>
        /// Restart ICE.
        /// </summary>
        public async Task<MeetingMessage<IceParametersT>> RestartIce(string transportId)
        {
            try
            {
                var iceParameters = await scheduler.RestartIceAsync(UserId, ConnectionId, transportId);
                return MeetingMessage<IceParametersT>.Success(iceParameters, "RestartIce 成功");
            }
            catch(MeetingException ex)
            {
                logger.LogError("RestartIce 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "RestartIce 调用失败.");
            }

            return MeetingMessage<IceParametersT>.Failure("RestartIce 失败");
        }

        #endregion

        #region Message

        /// <summary>
        /// Send message to other peers in rooms.
        /// </summary>
        public async Task<MeetingMessage> SendMessage(SendMessageRequest sendMessageRequest)
        {
            try
            {
                var otherPeerIds = await scheduler.GetOtherPeerIdsAsync(UserId, ConnectionId);

                // Notification: newMessage
                SendNotification(otherPeerIds, "newMessage", new NewMessageNotification
                {
                    Message = sendMessageRequest.Message,
                });

                return MeetingMessage.Success("SendMessage 成功");
            }
            catch(MeetingException ex)
            {
                logger.LogError("SendMessage 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "SendMessage 调用失败.");
            }

            return MeetingMessage.Failure("SendMessage 失败");
        }

        #endregion

        #region PeerAppData

        /// <summary>
        /// Set peer's appData. Then notify other peer, if in a room.
        /// </summary>
        public async Task<MeetingMessage> SetPeerAppData(SetPeerAppDataRequest setPeerAppDataRequest)
        {
            try
            {
                var peerPeerAppDataResult = await scheduler.SetPeerAppDataAsync(UserId, ConnectionId, setPeerAppDataRequest);

                // Notification: peerAppDataChanged
                SendNotification(peerPeerAppDataResult.OtherPeerIds, "peerAppDataChanged", new PeerAppDataChangedNotification
                {
                    PeerId = UserId,
                    AppData = peerPeerAppDataResult.AppData,
                });

                return MeetingMessage.Success("SetPeerAppData 成功");
            }
            catch(MeetingException ex)
            {
                logger.LogError("SetPeerAppData 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "SetPeerAppData 调用失败.");
            }

            return MeetingMessage.Failure("SetPeerAppData 失败");
        }

        /// <summary>
        /// Unset peer'ss appData. Then notify other peer, if in a room.
        /// </summary>
        public async Task<MeetingMessage> UnsetPeerAppData(UnsetPeerAppDataRequest unsetPeerAppDataRequest)
        {
            try
            {
                var peerPeerAppDataResult = await scheduler.UnsetPeerAppDataAsync(UserId, ConnectionId, unsetPeerAppDataRequest);

                // Notification: peerAppDataChanged
                SendNotification(peerPeerAppDataResult.OtherPeerIds, "peerAppDataChanged", new PeerAppDataChangedNotification
                {
                    PeerId = UserId,
                    AppData = peerPeerAppDataResult.AppData,
                });

                return MeetingMessage.Success("UnsetPeerAppData 成功");
            }
            catch(MeetingException ex)
            {
                logger.LogError("UnsetPeerAppData 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "UnsetPeerAppData 调用失败.");
            }

            return MeetingMessage.Failure("UnsetPeerAppData 失败");
        }

        /// <summary>
        /// Clear peer's appData. Then notify other peer, if in a room.
        /// </summary>
        ///
        public async Task<MeetingMessage> ClearPeerAppData()
        {
            try
            {
                var peerPeerAppDataResult = await scheduler.ClearPeerAppDataAsync(UserId, ConnectionId);

                // Notification: peerAppDataChanged
                SendNotification(peerPeerAppDataResult.OtherPeerIds, "peerAppDataChanged", new PeerAppDataChangedNotification
                {
                    PeerId = UserId,
                    AppData = peerPeerAppDataResult.AppData,
                });

                return MeetingMessage.Success("ClearPeerAppData 成功");
            }
            catch(MeetingException ex)
            {
                logger.LogError("ClearPeerAppData 调用失败: {Message}", ex.Message);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "ClearPeerAppData 调用失败.");
            }

            return MeetingMessage.Failure("ClearPeerAppData 失败");
        }

        #endregion

        #region Private Methods

        private async Task CreateConsumer(string consumerPeerId, string producerPeerId, Producer.Producer producer)
        {
            logger.LogDebug(
                "CreateConsumer() | [ConsumerPeerId:\"{ConsumerPeerId}\", ProducerPeerId:\"{ProducerPeerId}\", ProducerId:\"{ProducerId}\"]",
                consumerPeerId,
                producerPeerId,
                producer.Id
                );

            // Create the Consumer in paused mode.
            Consumer.Consumer? consumer;

            try
            {
                consumer = await scheduler.ConsumeAsync(producerPeerId, consumerPeerId, producer.Id);
                if(consumer == null)
                {
                    return;
                }
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "CreateConsumer()");
                return;
            }

            // Set Consumer events.
            consumer.On("score", obj =>
            {
                // For Testing
                //var score = (ConsumerScore?)obj;

                // Notification: consumerScore
                SendNotification(consumerPeerId, "consumerScore", new ConsumerScoreNotification
                {
                    ProducerPeerId = producerPeerId,
                    Kind = producer.Data.Kind,
                    ConsumerId = consumer.Id,
                    Score = obj
                });
                return Task.CompletedTask;
            });

            // consumer.On("@close", _ => ...);
            // consumer.On("@producerclose", _ => ...);
            // consumer.On("producerclose", _ => ...);
            // consumer.On("transportclose", _ => ...);
            consumer.Observer.On("close", _ =>
            {
                // Notification: consumerClosed
                SendNotification(consumerPeerId, "consumerClosed", new ConsumerClosedNotification
                {
                    ProducerPeerId = producerPeerId,
                    Kind = producer.Data.Kind,
                    ConsumerId = consumer.Id
                });
                return Task.CompletedTask;
            });

            consumer.On("producerpause", _ =>
            {
                // Notification: consumerPaused
                SendNotification(consumerPeerId, "consumerPaused", new ConsumerPausedNotification
                {
                    ProducerPeerId = producerPeerId,
                    Kind = producer.Data.Kind,
                    ConsumerId = consumer.Id
                });
                return Task.CompletedTask;
            });

            consumer.On("producerresume", _ =>
            {
                // Notification: consumerResumed
                SendNotification(consumerPeerId, "consumerResumed", new ConsumerResumedNotification
                {
                    ProducerPeerId = producerPeerId,
                    Kind = producer.Data.Kind,
                    ConsumerId = consumer.Id
                });
                return Task.CompletedTask;
            });

            consumer.On("layerschange", obj =>
            {
                // For Testing
                //var layers = (ConsumerLayers?)obj;

                // Notification: consumerLayersChanged
                SendNotification(consumerPeerId, "consumerLayersChanged", new ConsumerLayersChangedNotification
                {
                    ProducerPeerId = producerPeerId,
                    Kind = producer.Data.Kind,
                    ConsumerId = consumer.Id,
                    Layers = obj
                });
                return Task.CompletedTask;
            });

            // NOTE: For testing.
            // await consumer.enableTraceEvent([ 'rtp', 'keyframe', 'nack', 'pli', 'fir' ]);
            // await consumer.enableTraceEvent([ 'pli', 'fir' ]);
            // await consumer.enableTraceEvent([ 'keyframe' ]);

            consumer.On("trace", obj =>
            {
                logger.LogDebug("consumer \"trace\" event [consumerId:{ConsumerId}, trace:{Trace}]", consumer.Id, obj);
                return Task.CompletedTask;
            });

            // Send a request to the remote Peer with Consumer parameters.
            // Notification: newConsumer
            SendNotification(consumerPeerId, "newConsumer", new NewConsumerNotification
            {
                ProducerPeerId = producerPeerId,
                Kind = consumer.Data.Kind,
                ProducerId = producer.Id,
                ConsumerId = consumer.Id,
                RtpParameters = consumer.Data.RtpParameters,
                Type = consumer.Data.Type,
                ProducerAppData = producer.AppData,
                ProducerPaused = consumer.ProducerPaused,
            });
        }

        private void SendNotification(string peerId, string type, object data)
        {
            // For Testing
            if(type is "consumerLayersChanged" or "consumerScore" or "producerScore")
            {
                return;
            }

            var client = hubContext.Clients.User(peerId);
            client.Notify(new MeetingNotification
            {
                Type = type,
                Data = data
            }).ContinueWithOnFaultedHandleLog(logger);
        }

        private void SendNotification(IReadOnlyList<string> peerIds, string type, object data)
        {
            if(!peerIds.Any()) return;

            var client = hubContext.Clients.Users(peerIds);
            client.Notify(new MeetingNotification
            {
                Type = type,
                Data = data
            }).ContinueWithOnFaultedHandleLog(logger);
        }

        #endregion Private Methods
    }
}
