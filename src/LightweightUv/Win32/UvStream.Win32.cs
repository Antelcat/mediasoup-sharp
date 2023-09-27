using System.ComponentModel;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace LightweightUv;

public partial class UvStream
{
    private Stream? input;
    private Stream? output;

    public void Write(ArraySegment<byte> data)
    {
        if (input == null) throw new IOException("Stream is not open.");
        input.Write(data);
    }

    public void Close()
    {
        input?.Close();
        output?.Close();
        Closed?.Invoke();
    }

    #region stdio

    internal SafeHandle CreateStdPipe()
    {
        CreatePipe(out var parentHandle, out var childHandle, Writeable);
        
        if (Writeable)
        {
            try
            {
                input = new FileStream(parentHandle, FileAccess.Write, 4096, false);
            }
            catch
            {
                parentHandle.Dispose();
                throw;
            }
        }
        else
        {
            output = new FileStream(parentHandle, FileAccess.Read, 4096, false);
            Task.Factory.StartNew(() => ReaderTask(output), TaskCreationOptions.LongRunning);
        }
        
        childHandle.Dispose();
        return parentHandle;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct SecurityAttributes
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        public int bInheritHandle;
    }
    
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CreatePipe(
        out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe,
        ref SecurityAttributes lpPipeAttributes, int nSize);
    
    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint GetCurrentProcess();
    
    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DuplicateHandle(
        nint hSourceProcessHandle, SafeHandle hSourceHandle,
        nint hTargetProcessHandle, out SafeFileHandle lpTargetHandle,
        int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwOptions);

    private static void CreatePipeWithSecurityAttributes(
        out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe,
        ref SecurityAttributes lpPipeAttributes, int nSize)
    {
        var ret = CreatePipe(out hReadPipe, out hWritePipe, ref lpPipeAttributes, nSize);
        if (!ret || hReadPipe.IsInvalid || hWritePipe.IsInvalid)
        {
            throw new Win32Exception();
        }
    }

    private static void CreatePipe(out SafeFileHandle parentHandle, out SafeFileHandle childHandle, bool parentInputs)
    {
        SecurityAttributes securityAttributesParent = default;
        securityAttributesParent.bInheritHandle = 1;

        SafeFileHandle? hTmp = null;
        try
        {
            if (parentInputs)
            {
                CreatePipeWithSecurityAttributes(
                    out childHandle, out hTmp, ref securityAttributesParent, 0);
            }
            else
            {
                CreatePipeWithSecurityAttributes(out hTmp,
                    out childHandle,
                    ref securityAttributesParent,
                    0);
            }
            
            // Duplicate the parent handle to be non-inheritable so that the child process
            // doesn't have access. This is done for correctness sake, exact reason is unclear.
            // One potential theory is that child process can do something brain dead like
            // closing the parent end of the pipe and there by getting into a blocking situation
            // as parent will not be draining the pipe at the other end anymore.
            var currentProcHandle = GetCurrentProcess();
            if (!DuplicateHandle(currentProcHandle,
                    hTmp,
                    currentProcHandle,
                    out parentHandle,
                    0,
                    false,
                    2 /* DUPLICATE_SAME_ACCESS */))
            {
                throw new Win32Exception();
            }
        }
        finally
        {
            if (hTmp != null && !hTmp.IsInvalid)
            {
                hTmp.Dispose();
            }
        }
    }

    #endregion

    #region Extended stdio

    internal SafeHandle CreateUvPipe()
    {
        if (!Readable && !Writeable)
        {
            throw new InvalidOperationException("Stream is not readable or writeable.");
        }

        var serverDirection = Readable ? PipeDirection.InOut : PipeDirection.In;
        var clientDirection = Readable ? PipeDirection.In : PipeDirection.Out;

        string pipeName;
        while (true)
        {
            pipeName = @$"uv\{Guid.NewGuid().ToString("N")[..6]}\{Environment.ProcessId}";
            try
            {
                input = new NamedPipeServerStream(pipeName, serverDirection,
                    1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                break;
            }
            catch
            {
                // ignored
            }
        }

        var client = new NamedPipeClientStream(".", pipeName, clientDirection, PipeOptions.Asynchronous);
        client.Connect();
        output = client;

        if (Readable)
        {
            Task.Factory.StartNew(() => ReaderTask(input), TaskCreationOptions.LongRunning);
        }
        else
        {
            Task.Factory.StartNew(() => ReaderTask(output), TaskCreationOptions.LongRunning);
        }

        return client.SafePipeHandle;
    }

    #endregion

    private async Task ReaderTask(Stream stream)
    {
        var buffer = new byte[4096];
        while (stream.CanRead)
        {
            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            Data?.Invoke(new ArraySegment<byte>(buffer, 0, bytesRead));
        }

        Complete?.Invoke();
    }
}