using System.Diagnostics;
using System.Runtime.CompilerServices;
using LibuvSharp;
using Process = LibuvSharp.Process;

namespace MediasoupSharp.Test;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task Test1()
    {
        var dic = new Dictionary<string, string>
        {
            { "MEDIASOUP_VERSION", "3.12.4" },
            { "DEBUG", "*INFO* *WARN* *ERROR*" },
            { "INTERACTIVE", "'true'" },
            { "MEDIASOUP_LISTEN_IP", "0.0.0.0" },
            { "MEDIASOUP_ANNOUNCED_IP", "0.0.0.0" },
        };
        Process? process;
        try
        {
            process = Process.Spawn(new ProcessOptions
            {
                Detached = false,
                Arguments = new[]
                {
                    "--logLevel=debug", "--logTag=info",
                    "--logTag=ice", "--logTag=rtx",
                    "--logTag=bwe", "--logTag=score",
                    "--logTag=simulcast", "--logTag=svc",
                    "--logTag=sctp", "--logTag=message",
                    "--rtcMinPort=20000", "--rtcMaxPort=29999"
                },
                Environment = dic.Select(x => $"{x.Key}={x.Value}").ToArray(),
                File =
                    @"D:\Shared\WorkSpace\Git\mediasoup-sharp\src\MediasoupSharp.Test\runtimes\win-x64\native\mediasoup-worker.exe",
                Streams = new List<UVStream> { Pipe(), Pipe(), Pipe(), Pipe(), Pipe(), Pipe(), Pipe(), }
            }, Console.WriteLine);
        }
        catch (Exception e)
        {
            throw e;
        }

        await Task.Delay(10000);

        return;
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Pipe Pipe()
        {
            var ret = new Pipe { Writeable = true, Readable = true };
            ret.Error += _ => Debugger.Break();
            ret.Data  += _ => { };
            return ret;
        }
    }
}