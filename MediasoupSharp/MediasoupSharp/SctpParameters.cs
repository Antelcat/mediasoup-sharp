namespace MediasoupSharp;

public record SctpCapabilities(NumSctpStreams NumStreams);

public record NumSctpStreams(Number Os,Number Mis);

public record SctpParameters(Number Port,Number Os,Number Mis,Number MaxMessageSize);

public record SctpStreamParameters(
    Number StreamId,
    bool Ordered,
    Number MaxPacketLifeTime,
    Number MaxRetransmits);