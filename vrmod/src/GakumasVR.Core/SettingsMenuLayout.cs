namespace GakumasVR.Core;

public enum SettingsMenuAction { None, Previous, Next, Apply, Save, Close, ProfileA, ProfileC, Decrease, Increase, Language }

public static class SettingsMenuLayout
{
    public const int Width = 960;
    public const int Height = 720;
    public const int RowsPerPage = 6;
    public const int ProfileStart = 24;
    public const int ProfileSpacing = 460;
    public const int ProfileWidth = 452;
    public static (SettingsMenuAction Action, int Row) Hit(float u, float v)
    {
        if (float.IsNaN(u) || float.IsNaN(v) || u < 0f || u > 1f || v < 0f || v > 1f)
            return (SettingsMenuAction.None, -1);
        float x = u * Width, y = v * Height;
        if (x >= 700 && x < 936 && y >= 18 && y < 54) return (SettingsMenuAction.Language, -1);
        if (y >= 84 && y < 132)
        {
            if (x >= ProfileStart && x < ProfileStart + ProfileWidth) return (SettingsMenuAction.ProfileA, -1);
            if (x >= ProfileStart + ProfileSpacing && x < ProfileStart + ProfileSpacing + ProfileWidth)
                return (SettingsMenuAction.ProfileC, -1);
        }
        if (y >= 148 && y < 568)
        {
            int row = (int)((y - 148) / 70);
            if ((y - 148) % 70 < 60)
            {
                if (x >= 510 && x < 574) return (SettingsMenuAction.Decrease, row);
                if (x >= 866 && x < 930) return (SettingsMenuAction.Increase, row);
            }
        }
        if (y >= 640 && y < 696 && x >= 24 && x < 936)
        {
            int index = Math.Min(4, (int)((x - 24) / 184));
            if ((x - 24) % 184 < 176) return ((SettingsMenuAction)((int)SettingsMenuAction.Previous + index), -1);
        }
        return (SettingsMenuAction.None, -1);
    }
}
