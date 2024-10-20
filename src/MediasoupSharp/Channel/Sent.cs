﻿using FBS.Response;

namespace MediasoupSharp.Channel;

public class Sent
{
    public RequestMessage RequestMessage { get; set; }

    public Action<Response> Resolve { get; set; }

    public Action<Exception> Reject { get; set; }

    public Action Close { get; set; }
}