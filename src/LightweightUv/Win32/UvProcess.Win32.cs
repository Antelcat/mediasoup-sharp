using System.Runtime.InteropServices;
using System.Text;

namespace LightweightUv;

public partial class UvProcess
{
    public void Kill(int exitCode)
    {
        var handle = OpenProcess(0x0001 | 0x0010, false, Id);
        if (handle == 0)
        {
            Marshal.ThrowExceptionForHR(Marshal.GetLastWin32Error());
        }

        if (!TerminateProcess(handle, exitCode))
        {
            Marshal.ThrowExceptionForHR(Marshal.GetLastWin32Error());
        }
    }

    public static unsafe UvProcess Spawn(ProcessOptions options, Action<UvProcess>? exitCallback = null)
    {
        // ReSharper disable CommentTypo
        var stdioBlockSize = sizeof(byte) + sizeof(nuint);

        var streams = options.Streams ?? (IList<UvPipe>)Array.Empty<UvPipe>();
        int stdioBufferSize;
        nint stdioBuffer;
        if (streams.Count == 0)
        {
            stdioBufferSize = 0;
            stdioBuffer = 0;
        }
        else
        {
            stdioBufferSize = sizeof(int) + stdioBlockSize * streams.Count;
            stdioBuffer = Marshal.AllocHGlobal(stdioBufferSize);

            *(int*)stdioBuffer = streams.Count;
            for (var i = 0; i < streams.Count; i++)
            {
                var ptr = stdioBuffer + sizeof(int) + i * stdioBlockSize;
                if (i < 3)
                {
                    streams[i].Readable = true;
                    streams[i].Writeable = i == 0;
                    *(nint*)(ptr + sizeof(byte)) = streams[i].CreateStdPipe().DangerousGetHandle();
                    *(byte*)ptr = 0x01 | 0x40; // FOPEN | FDEV
                }
                else
                {
                    *(nint*)(ptr + sizeof(byte)) = streams[i].CreateUvPipe().DangerousGetHandle();
                    *(byte*)ptr = 0x01 | 0x08; // FOPEN | FPIPE
                }
            }
        }

        var startup = new StartupInfo
        {
            cb = Marshal.SizeOf<StartupInfo>(),
            // dwFlags = STARTF_USESTDHANDLES | STARTF_USESHOWWINDOW,
            dwFlags = 0x00000100 | 0x00000001,
            cbReserved2 = (short)stdioBufferSize,
            lpReserved2 = stdioBuffer,
            hStdInput = stdioBuffer + sizeof(int) + 0 * stdioBlockSize + sizeof(byte),
            hStdOutput = stdioBuffer + sizeof(int) + 1 * stdioBlockSize + sizeof(byte),
            hStdError = stdioBuffer + sizeof(int) + 2 * stdioBlockSize + sizeof(byte),
        };

        var applicationName = Path.GetFullPath(
            Environment.ExpandEnvironmentVariables(options.File));

        var cmd = options.BuildArguments();

        string? env = null;
        if (options.Environment != null)
        {
            var builder = new StringBuilder();
            foreach (var pair in options.Environment)
            {
                builder.AppendLine(pair).Append('\0');
            }
            env = builder.ToString();
        }

        var flags = ProcessCreationFlags.CreateUnicodeEnvironment;
        if (options.Detached)
        {
            flags |= ProcessCreationFlags.DetachedProcess | ProcessCreationFlags.CreateNewProcessGroup;
        }

        var info = new ProcessInformation();
        var result = CreateProcessW(
            applicationName,
            cmd,
            IntPtr.Zero,
            IntPtr.Zero,
            true,
            flags,
            env,
            Environment.CurrentDirectory,
            ref startup,
            ref info);
        // ReSharper restore CommentTypo

        if (!result)
        {
            Marshal.ThrowExceptionForHR(Marshal.GetLastWin32Error());
        }

        return new UvProcess(info.dwProcessId);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct StartupInfo
    {
        public int cb;
        public nint lpReserved;
        public nint lpDesktop;
        public nint lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public nint lpReserved2;
        public nint hStdInput;
        public nint hStdOutput;
        public nint hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        public nint hProcess;
        public nint hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    [Flags]
    public enum ProcessCreationFlags : uint
    {
        CreateSuspended = 0x00000004,
        DetachedProcess = 0x00000008,
        CreateNewProcessGroup = 0x00000200,
        CreateUnicodeEnvironment = 0x00000400,
        CreateNoWindow = 0x08000000,
    }

    [LibraryImport("kernel32.dll",
        SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CreateProcessW(string lpApplicationName, string? lpCommandLine,
        IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles,
        ProcessCreationFlags dwCreationFlags, string? lpEnvironment, string lpCurrentDirectory,
        ref StartupInfo lpStartupInfo, ref ProcessInformation lpProcessInformation);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial nint OpenProcess(
        int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool TerminateProcess(nint hProcess, int uExitCode);
}