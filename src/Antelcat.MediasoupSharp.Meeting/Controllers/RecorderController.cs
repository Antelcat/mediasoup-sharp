using FBS.PlainTransport;
using FBS.RtpParameters;
using Antelcat.MediasoupSharp.ClientRequest;
using Antelcat.MediasoupSharp.Meeting.Models;
using Antelcat.MediasoupSharp.RtpParameters;
using Microsoft.AspNetCore.Mvc;

namespace Antelcat.MediasoupSharp.Meeting.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecorderController(ILogger<RecorderController> logger, Scheduler scheduler) : ControllerBase
    {
        [HttpGet]
        public ApiResult Get()
        {
            return new ApiResult();
        }

        [HttpGet("Record.sdp")]
        public async Task<object> Record()
        {
            var recorderPrepareRequest = new RecorderPrepareRequest
            {
                PeerId = "100001@100001",
                RoomId = "0",
                ProducerPeerId = "9",
                ProducerSources = ["audio:mic"]
            };

            // Join
            await scheduler.LeaveAsync(recorderPrepareRequest.PeerId);
            var joinRequest = new JoinRequest
            {
                RtpCapabilities = new RtpCapabilities
                {
                    Codecs =
                    [
                        new()
                        {
                            Kind      = MediaKind.AUDIO,
                            MimeType  = "audio/opus",
                            ClockRate = 48000,
                            Channels  = 2,
                            RtcpFeedback =
                            [
                                new()
                                {
                                    Type = "transport-cc",
                                }

                            ]
                        },
                        //new() {
                        //    Kind = MediaKind.Audio,
                        //    MimeType ="audio/PCMA",
                        //    PreferredPayloadType= 8,
                        //    ClockRate = 8000,
                        //    RtcpFeedback = new RtcpFeedback[]
                        //    {
                        //        new RtcpFeedback{
                        //            Type = "transport-cc",
                        //        },
                        //    }
                        //},

                        new()
                        {
                            Kind      = MediaKind.VIDEO,
                            MimeType  = "video/H264",
                            ClockRate = 90000,
                            Parameters = new Dictionary<string, object>
                            {
                                { "level-asymmetry-allowed", 1 },
                            },
                            RtcpFeedback =
                            [
                                new()
                                {
                                    Type = "nack",
                                },

                                new()
                                {
                                    Type = "nack", Parameter = "pli",
                                },

                                new()
                                {
                                    Type = "ccm", Parameter = "fir",
                                },

                                new()
                                {
                                    Type = "goog-remb",
                                },

                                new()
                                {
                                    Type = "transport-cc",
                                }

                            ]
                        }


                    ],
                },
                DisplayName = $"Recorder:{recorderPrepareRequest.PeerId}",
                Sources = null,
                AppData = new Dictionary<string, object> { ["type"] = "Recorder" },
            };

            await scheduler.JoinAsync(recorderPrepareRequest.PeerId, "", null!, joinRequest);

            // Join room
            var joinRoomRequest = new JoinRoomRequest
            {
                RoomId = recorderPrepareRequest.RoomId,
            };
            var joinRoomResult = await scheduler.JoinRoomAsync(recorderPrepareRequest.PeerId, "", joinRoomRequest);

            // Create PlainTransport
            var transport = await CreatePlainTransportAsync(recorderPrepareRequest.PeerId);
            const string remoteRtpIp = "127.0.0.1";
            const ushort remoteRtpPort = 8787;
            ushort? remoteRtcpPort = transport.Data.RtcpMux ? null : 8788;
            var plainTransportConnectParameters = new ConnectRequestT
            {
                Ip = remoteRtpIp,
                Port = remoteRtpPort,
                RtcpPort = remoteRtcpPort,
            };

            await transport.ConnectAsync(plainTransportConnectParameters);

            // Create Consumers
            var producerPeer = joinRoomResult.Peers.FirstOrDefault(m => m.PeerId == recorderPrepareRequest.ProducerPeerId);
            if(producerPeer == null)
            {
                return new ApiResult { Code = 400, Message = "生产者 Peer 不存在" };
            }

            var recorderPrepareResult = new RecorderPrepareResult
            {
                TransportId = transport.TransportId,
                Ip = plainTransportConnectParameters.Ip,
                Port = plainTransportConnectParameters.Port.Value,
                RtcpPort = plainTransportConnectParameters.RtcpPort,
            };

            var producers = await producerPeer.GetProducersASync();
            foreach(var source in recorderPrepareRequest.ProducerSources)
            {
                if(!producerPeer.Sources.Contains(source))
                {
                    return new ApiResult { Code = 400, Message = $"生产者 Sources 不包含请求的 {source}" };
                }

                var producer = producers.Values.FirstOrDefault(m => m.Source == source);
                if(producer == null)
                {
                    return new ApiResult { Code = 400, Message = $"生产者尚未生产 {source}" };
                }
                var consumer = await scheduler.ConsumeAsync(recorderPrepareRequest.ProducerPeerId, recorderPrepareRequest.PeerId, producer.ProducerId);
                if(consumer == null)
                {
                    return new ApiResult { Code = 400, Message = $"已经在消费 {source}" };
                }

                await consumer.ResumeAsync();

                recorderPrepareResult.ConsumerParameters.Add(new ConsumerParameters
                {
                    Source = source,
                    Kind = consumer.Data.Kind,
                    PayloadType = consumer.Data.RtpParameters.Codecs[0].PayloadType,
                    Ssrc = consumer.Data.RtpParameters!.Encodings![0].Ssrc!.Value
                });
            }

            return Content(recorderPrepareResult.Sdp(0));
        }

        private async Task<PlainTransport.PlainTransport> CreatePlainTransportAsync(string peerId)
        {
            var createPlainTransportRequest = new CreatePlainTransportRequest
            {
                Comedia = false, /* 推流设置为 true*/
                RtcpMux = true, /* 推流设置为 false, FFmpeg 设置为 true, GStreamer 设置为 false */
                Producing = false,
                Consuming = true,
            };
            var transport = await scheduler.CreatePlainTransportAsync(peerId, "", createPlainTransportRequest);
            return transport;
        }
    }
}
