namespace MediasoupSharp.FlatBuffers.Worker.T;

public class UpdateSettingsRequestT
{
    public string LogLevel { get; set; }

    public List<string> LogTags { get; set; }
}