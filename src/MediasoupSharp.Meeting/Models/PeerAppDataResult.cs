﻿namespace MediasoupSharp.Meeting.Models
{
    public class PeerAppDataResult
    {
        public string SelfPeerId { get; set; }

        public Dictionary<string, object> AppData { get; set; }

        public string[] OtherPeerIds { get; set; }
    }

    public class PeerInternalDataResult
    {
        public Dictionary<string, object> InternalData { get; set; }
    }
}
