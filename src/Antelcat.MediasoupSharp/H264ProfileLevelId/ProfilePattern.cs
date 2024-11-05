namespace Antelcat.MediasoupSharp.H264ProfileLevelId;

/// <summary>
///  Class for converting between profile_idc/profile_iop to Profile.
/// </summary>
internal class ProfilePattern(int profileIdc, BitPattern profileIop, Profile profile)
{
    public int ProfileIdc { get; } = profileIdc;

    public BitPattern ProfileIop { get; } = profileIop;

    public Profile Profile { get; } = profile;
}