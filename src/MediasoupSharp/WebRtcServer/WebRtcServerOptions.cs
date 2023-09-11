namespace MediasoupSharp.WebRtcServer;

public class WebRtcServerOptions<TWebRtcServerAppData>
{
	/// <summary>
	/// Listen infos.
	/// </summary>
	public List<WebRtcServerListenInfo> ListenInfos { get; set; } = new();

	/// <summary>
	/// Custom application data.
	/// </summary>
	public TWebRtcServerAppData? AppData { get; set; }
}