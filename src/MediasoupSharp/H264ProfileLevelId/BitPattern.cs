namespace MediasoupSharp.H264ProfileLevelId;

// Class for matching bit patterns such as "x1xx0000" where 'x' is allowed to be
// either 0 or 1.
internal class BitPattern
{
    private readonly byte mask;
    private readonly byte maskedValue;

    public BitPattern(string str)
    {
        mask        = H264ProfileLevelId.ByteMaskString('x', str);
        maskedValue = H264ProfileLevelId.ByteMaskString('1', str);
    }

    public bool IsMatch(byte value)
    {
        return maskedValue == (value & mask);
    }
}