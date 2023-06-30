namespace MediasoupSharp;

public record SctpCapabilities(NumSctpStreams numStreams);

public record NumSctpStreams(Number OS,Number MIS);

public record SctpParameters(Number port,Number OS,Number MIS,Number maxMessageSize);

public record SctpStreamParameters(
    Number streamId,
    bool ordered,
    Number maxPacketLifeTime,
    Number maxRetransmits);