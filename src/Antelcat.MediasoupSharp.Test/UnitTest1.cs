using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Antelcat.LibuvSharp;
using Process = Antelcat.LibuvSharp.Process;

namespace Antelcat.MediasoupSharp.Test;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task MultiTest()
    {
        var max = 5;
        while (max-- > 0)
        {
            try
            {
                await Test();
            }
            catch (Exception e)
            {
                Assert.Fail($"第{5 - max}次发生了:\n{e}");
            }
        }
    }

    [Test]
    public async Task Test()
    {
        var dic = new Dictionary<string, string>
        {
            { "MEDIASOUP_VERSION", "3.12.4" },
            { "DEBUG", "*INFO* *WARN* *ERROR*" },
            { "INTERACTIVE", "'true'" },
            { "MEDIASOUP_LISTEN_IP", "0.0.0.0" },
            { "MEDIASOUP_ANNOUNCED_ADDRESS", "0.0.0.0" },
        };
        try
        {
            var process = Process.Spawn(new ProcessOptions
            {
                Arguments = new[]
                {
                    "--logLevel=debug", "--logTag=info",
                    "--logTag=ice", "--logTag=rtx",
                    "--logTag=bwe", "--logTag=score",
                    "--logTag=simulcast", "--logTag=svc",
                    "--logTag=sctp", "--logTag=message",
                    "--rtcMinPort=20000", "--rtcMaxPort=29999",
                },
                Environment = dic.Select(x => $"{x.Key}={x.Value}").ToArray(),
                File =
                    @"D:\Shared\WorkSpace\Git\mediasoup-sharp\src\Antelcat.Antelcat.MediasoupSharp.Test\runtimes\win-x64\native\mediasoup-worker.exe",
                Streams = new List<Pipe> { new(), Pipe(), Pipe(), Pipe(), Pipe(), Pipe(), Pipe(), }.ToArray()
            });
        }
        catch (Exception e)
        {
            Debugger.Break();
        }

        await Task.Delay(5000);
        return;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Pipe Pipe()
        {
            var ret = new Pipe { Writeable = true, Readable = true };
            ret.Error += _ => Debugger.Break();
            ret.Data += data =>
            {
                
            };
            return ret;
        }
    }


    [Test]
    public void TestSerialize()
    {
        var demo   = new Demo();
        var option =  new JsonSerializerOptions
        {
            Converters = { new EnumConverter() }
        };
        var convert = JsonSerializer.Serialize(demo, option);
        var source  = JsonSerializer.Deserialize<Demo>(convert, option);
    }

    public class Demo
    {
        public enum DemoEnum
        {
            A,
            B
        }
        
        public DemoEnum Enum { get; set; }
    }

    public class EnumConverter : JsonConverter<Demo.DemoEnum>
    {
        public override Demo.DemoEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, Demo.DemoEnum value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value switch
            {
                Demo.DemoEnum.A => "a",
                Demo.DemoEnum.B => "b"
            });
        }
    }
}

