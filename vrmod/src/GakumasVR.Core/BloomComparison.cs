namespace GakumasVR.Core;

public static class BloomComparison
{
    public static int Diffusion(int authored, float eyeFovDegrees, float projectionScale)
    {
        if (authored <= 0) return authored;
        float scale = float.IsNaN(eyeFovDegrees) || float.IsInfinity(eyeFovDegrees) || eyeFovDegrees <= 1f
            ? 0.3f : Math.Clamp(29.9f / eyeFovDegrees, 0.08f, 1f);
        float value = !float.IsNaN(projectionScale) && !float.IsInfinity(projectionScale) && projectionScale > 0f
            ? authored + MathF.Log(projectionScale, 2f) : authored * scale;
        return Math.Max(1, (int)MathF.Round(value, MidpointRounding.AwayFromZero));
    }
}
