using System.Globalization;

namespace MediasoupSharp.H264ProfileLevelId;

public static class H264ProfileLevelId
{
    // Default ProfileLevelId.
    //
    // TODO: The default should really be profile Baseline and level 1 according to
    // the spec: https://tools.ietf.org/html/rfc6184#section-8.1. In order to not
    // break backwards compatibility with older versions of WebRTC where external
    // codecs don"t have any parameters, use profile ConstrainedBaseline level 3_1
    // instead. This workaround will only be done in an interim period to allow
    // external clients to update their code.
    //
    // http://crbug/webrtc/6337.
    internal static ProfileLevelId DefaultProfileLevelId = new (ProfileConstrainedBaseline, Level31);

    const int ProfileConstrainedBaseline = 1;
    const int ProfileBaseline            = 2;
    const int ProfileMain                = 3;
    const int ProfileConstrainedHigh     = 4;
    const int ProfileHigh                = 5;

    // Convert a string of 8 characters into a byte where the positions containing
    // character c will have their bit set. For example, c = "x", str = "x1xx0000"
    // will return 0b10110000.
    internal static byte ByteMaskString(char c, string str)
    {
        return (byte)(
            ((str[0] == c ? 1 : 0) << 7) | ((str[1] == c ? 1 : 0) << 6) | ((str[2] == c ? 1 : 0) << 5) |
            ((str[3] == c ? 1 : 0) << 4) | ((str[4] == c ? 1 : 0) << 3) | ((str[5] == c ? 1 : 0) << 2) |
            ((str[6] == c ? 1 : 0) << 1) | ((str[7] == c ? 1 : 0) << 0)
        );
    }

    // For level_idc=11 and profile_idc=0x42, 0x4D, or 0x58, the constraint set3
    // flag specifies if level 1b or level 1.1 is used.
    const int ConstraintSet3Flag = 0x10;
    const int Level1B           = 0;
    const int Level1             = 10;
    const int Level11           = 11;
    const int Level12           = 12;
    const int Level13           = 13;
    const int Level2             = 20;
    const int Level21           = 21;
    const int Level22           = 22;
    const int Level3             = 30;
    const int Level31           = 31;
    const int Level32           = 32;
    const int Level4             = 40;
    const int Level41           = 41;
    const int Level42           = 42;
    const int Level5             = 50;
    const int Level51           = 51;
    const int Level52           = 52;

    // This is from https://tools.ietf.org/html/rfc6184#section-8.1.
    private static List<ProfilePattern> profilePatterns = new()
    {
        new ProfilePattern(0x42, new BitPattern("x1xx0000"), ProfileConstrainedBaseline),
        new ProfilePattern(0x4D, new BitPattern("1xxx0000"), ProfileConstrainedBaseline),
        new ProfilePattern(0x58, new BitPattern("11xx0000"), ProfileConstrainedBaseline),
        new ProfilePattern(0x42, new BitPattern("x0xx0000"), ProfileBaseline),
        new ProfilePattern(0x58, new BitPattern("10xx0000"), ProfileBaseline),
        new ProfilePattern(0x4D, new BitPattern("0x0x0000"), ProfileMain),
        new ProfilePattern(0x64, new BitPattern("00000000"), ProfileHigh),
        new ProfilePattern(0x64, new BitPattern("00001100"), ProfileConstrainedHigh)
    };

    /// <summary>
    /// Returns true if the parameters have the same H264 profile, i.e. the same
    /// H264 profile (Baseline, High, etc).
    /// @param {Object} [params1={}] - Codec parameters object.
    /// @param {Object} [params2={}] - Codec parameters object.
    /// </summary>
    /// <param name="params1"></param>
    /// <param name="params2"></param>
    /// <returns></returns>
    internal static bool IsSameProfile(IDictionary<string, object?> params1, IDictionary<string, object?> params2)
    {
        var profileLevelId1 = ParseSdpProfileLevelId(params1);
        var profileLevelId2 = ParseSdpProfileLevelId(params2);

        // Compare H264 profiles, but not levels.
        return
            profileLevelId1 != null &&
            profileLevelId2 != null &&
            profileLevelId1.Profile == profileLevelId2.Profile;
    }

    /// <summary>
    /// Parse profile level id that is represented as a string of 3 hex bytes
    /// contained in an SDP key-value map. A default profile level id will be
    /// returned if the profile-level-id key is missing. Nothing will be returned if
    /// the key is present but the string is invalid.
    /// </summary>
    /// <param name="param"></param>
    /// <returns></returns>
    public static ProfileLevelId? ParseSdpProfileLevelId(IDictionary<string, object?> param) =>
        param["profile-level-id"] is not string profileLevelId
            ? DefaultProfileLevelId
            : ParseProfileLevelId(profileLevelId);
    
    /// <summary>
    /// Parse profile level id that is represented as a string of 3 hex bytes.
    /// Nothing will be returned if the string is not a recognized H264 profile
    /// level id.
    /// @param {String} str - profile-level-id value as a string of 3 hex bytes.
    /// @returns {ProfileLevelId}
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static ProfileLevelId? ParseProfileLevelId(string str)
    {
        // The string should consist of 3 bytes in hexadecimal format.
        if (str.Length != 6)
            return null;

        var profileLevelIdNumeric = int.Parse(str, NumberStyles.HexNumber);

        if (profileLevelIdNumeric == 0)
            return null;

        // Separate into three bytes.
        var levelIdc   = profileLevelIdNumeric         & 0xFF;
        var profileIop = (profileLevelIdNumeric >> 8)  & 0xFF;
        var profileIdc = (profileLevelIdNumeric >> 16) & 0xFF;

        // Parse level based on level_idc and varraint set 3 flag.
        int level;

        switch (levelIdc)
        {
            case Level11:
            {
                level = (profileIop & ConstraintSet3Flag) != 0 ? Level1B : Level11;
                break;
            }
            case Level1:
            case Level12:
            case Level13:
            case Level2:
            case Level21:
            case Level22:
            case Level3:
            case Level31:
            case Level32:
            case Level4:
            case Level41:
            case Level42:
            case Level5:
            case Level51:
            case Level52:
            {
                level = levelIdc;
                break;
            }
            // Unrecognized level_idc.
            default:
            {
                Console.WriteLine("parseProfileLevelId() | unrecognized level_idc:{0}", levelIdc);
                return null;
            }
        }

        // Parse profile_idc/profile_iop into a Profile enum.
        foreach (var pattern in profilePatterns)
        {
            if (
                profileIdc == pattern.ProfileIdc &&
                pattern.ProfileIop.IsMatch((byte)profileIop)
            )
            {
                return new ProfileLevelId(pattern.Profile, level);
            }
        }

        Console.WriteLine("parseProfileLevelId() | unrecognized profile_idc/profile_iop combination");

        return null;
    }

    /// <summary>
    /// Generate codec parameters that will be used as answer in an SDP negotiation
    /// based on local supported parameters and remote offered parameters. Both
    /// local_supported_params and remote_offered_params represent sendrecv media
    /// descriptions, i.e they are a mix of both encode and decode capabilities. In
    /// theory, when the profile in local_supported_params represent a strict superset
    /// of the profile in remote_offered_params, we could limit the profile in the
    /// answer to the profile in remote_offered_params.
    /// However, to simplify the code, each supported H264 profile should be listed
    /// explicitly in the list of local supported codecs, even if they are redundant.
    /// Then each local codec in the list should be tested one at a time against the
    /// remote codec, and only when the profiles are equal should this function be
    /// called. Therefore, this function does not need to handle profile intersection,
    /// and the profile of local_supported_params and remote_offered_params must be
    /// equal before calling this function. The parameters that are used when
    /// negotiating are the level part of profile-level-id and level-asymmetry-allowed.
    /// @param {Object} [local_supported_params={}]
    /// @param {Object} [remote_offered_params={}]
    /// @returns {String} Canonical string representation as three hex bytes of the
    ///   profile level id, or null if no one of the params have profile-level-id.
    /// @throws {TypeError} If Profile mismatch or invalid params.
    /// </summary>
    /// <param name="localSupportedParams"></param>
    /// <param name="remoteOfferedParams"></param>
    public static string? GenerateProfileLevelIdForAnswer(
	    IDictionary<string, object?> localSupportedParams,
	    IDictionary<string, object?> remoteOfferedParams)
    {
	    // If both local and remote params do not contain profile-level-id, they are
	    // both using the default profile. In this case, don"t return anything.
	    if (
		    localSupportedParams["profile-level-id"] != null &&
		    remoteOfferedParams["profile-level-id"]  != null
	    )
	    {
		    Console.WriteLine(
			    "generateProfileLevelIdForAnswer() | no profile-level-id in local and remote params");
		    return null;
	    }

	    // Parse profile-level-ids.
	    var localProfileLevelId =
		    ParseSdpProfileLevelId(localSupportedParams);
	    var remoteProfileLevelId =
		    ParseSdpProfileLevelId(remoteOfferedParams);

	    // The local and remote codec must have valid and equal H264 Profiles.
	    if (localProfileLevelId == null)
		    throw new TypeError("invalid local_profile_level_id");

	    if (remoteProfileLevelId == null)
		    throw new TypeError("invalid remote_profile_level_id");

	    if (localProfileLevelId.Profile != remoteProfileLevelId.Profile)
		    throw new TypeError("H264 Profile mismatch");

	    // Parse level information.
	    var levelAsymmetryAllowed = (
		    IsLevelAsymmetryAllowed(localSupportedParams) &&
		    IsLevelAsymmetryAllowed(remoteOfferedParams)
	    );

	    var localLevel  = localProfileLevelId.Level;
	    var remoteLevel  = remoteProfileLevelId.Level;
	    var minLevel  = MinLevel(localLevel, remoteLevel);
	
	    // Determine answer level. When level asymmetry is not allowed, level upgrade
	    // is not allowed, i.e., the level in the answer must be equal to or lower
	    // than the level in the offer.
	    var answerLevel  = levelAsymmetryAllowed ? localLevel : minLevel;

	    Console.WriteLine(
		    "generateProfileLevelIdForAnswer() | result: [profile:{0}, level:{1}]",
		    localProfileLevelId.Profile, answerLevel);

	    // Return the resulting profile-level-id for the answer parameters.
	    return ProfileLevelIdToString(
		    new ProfileLevelId(localProfileLevelId.Profile, answerLevel));
    }


    /// <summary>
    /// Returns canonical string representation as three hex bytes of the profile
    /// level id, or returns nothing for invalid profile level ids.
    /// @param {ProfileLevelId} profile_level_id
    /// </summary>
    /// <param name="profileLevelId"></param>
    /// <returns></returns>
    public static string? ProfileLevelIdToString(ProfileLevelId profileLevelId)
    {
	    // Handle special case level == 1b.
	    if (profileLevelId.Level == Level1B)
	    {
		    switch (profileLevelId.Profile)
		    {
			    case ProfileConstrainedBaseline:
			    {
				    return "42f00b";
			    }
			    case ProfileBaseline:
			    {
				    return "42100b";
			    }
			    case ProfileMain:
			    {
				    return "4d100b";
			    }
			    // Level 1_b is not allowed for other profiles.
			    default:
			    {
				    Console.WriteLine(
					    "profileLevelIdToString() | Level 1_b not is allowed for profile:{0}",
					    profileLevelId.Profile);

				    return null;
			    }
		    }
	    }

	    string profileIdcIopString;

	    switch (profileLevelId.Profile)
	    {
		    case ProfileConstrainedBaseline:
		    {
			    profileIdcIopString = "42e0";
			    break;
		    }
		    case ProfileBaseline:
		    {
			    profileIdcIopString = "4200";
			    break;
		    }
		    case ProfileMain:
		    {
			    profileIdcIopString = "4d00";
			    break;
		    }
		    case ProfileConstrainedHigh:
		    {
			    profileIdcIopString = "640c";
			    break;
		    }
		    case ProfileHigh:
		    {
			    profileIdcIopString = "6400";
			    break;
		    }
		    default:
		    {
			    Console.WriteLine(
				    "profileLevelIdToString() | unrecognized profile:{0}",
				    profileLevelId.Profile);

			    return null;
		    }
	    }

	    var levelStr = profileLevelId.Level.ToString("X");

	    if (levelStr.Length == 1)
		    levelStr = $"0{levelStr}";

	    return $"{profileIdcIopString}${levelStr}";
    }

    internal static bool IsLessLevel(int a, int b)
    {
	    if (a == Level1B)
		    return b != Level1 && b != Level1B;

	    if (b == Level1B)
		    return a != Level1;

	    return a < b;
    }

    internal static int MinLevel(int a, int b) => IsLessLevel(a, b) ? a : b;

    public static bool IsLevelAsymmetryAllowed(IDictionary<string,object?> param)
    {
	    var levelAsymmetryAllowed = param["level-asymmetry-allowed"];

	    return levelAsymmetryAllowed is 1 or "1";
    }

}