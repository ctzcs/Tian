
namespace Engine.Utility;

public enum FontLanguage
{
    SimplifiedChinese,
    TraditionalChinese,
    Japanese,
    Korean,
    Russian
}

public static class FontUtility
{
    public static int[] GetCodepoints(int commonCjkCount, params FontLanguage[]? languages)
    {
        if (commonCjkCount < 0)
            commonCjkCount = 0;

        var set = new HashSet<int>();
        AddCommonSymbols(set);

        if (languages == null || languages.Length == 0)
            languages = [FontLanguage.SimplifiedChinese];

        for (int i = 0; i < languages.Length; i++)
            AddLanguage(set, languages[i], commonCjkCount);

        var result = new int[set.Count];
        set.CopyTo(result);
        System.Array.Sort(result);
        return result;
    }

    static void AddLanguage(HashSet<int> set, FontLanguage lang, int commonCjkCount)
    {
        switch (lang)
        {
            case FontLanguage.SimplifiedChinese:
                AddCommonCjk(set, commonCjkCount);
                break;

            case FontLanguage.TraditionalChinese:
                AddCommonCjk(set, commonCjkCount);
                AddRange(set, 0xF900, 0xFAFF);
                AddRange(set, 0x3100, 0x312F);
                AddRange(set, 0x31A0, 0x31BF);
                break;

            case FontLanguage.Japanese:
                AddRange(set, 0x3040, 0x309F);
                AddRange(set, 0x30A0, 0x30FF);
                AddRange(set, 0x31F0, 0x31FF);
                AddRange(set, 0xFF66, 0xFF9D);
                AddCommonCjk(set, commonCjkCount);
                break;

            case FontLanguage.Korean:
                AddRange(set, 0x1100, 0x11FF);
                AddRange(set, 0x3130, 0x318F);
                AddRange(set, 0xA960, 0xA97F);
                AddRange(set, 0xAC00, 0xD7AF);
                AddRange(set, 0xD7B0, 0xD7FF);
                break;

            case FontLanguage.Russian:
                AddRange(set, 0x0400, 0x04FF);
                AddRange(set, 0x0500, 0x052F);
                AddRange(set, 0x2DE0, 0x2DFF);
                AddRange(set, 0xA640, 0xA69F);
                break;
        }
    }

    static void AddCommonCjk(HashSet<int> set, int commonCjkCount)
    {
        const int cjkStart = 0x4E00;
        const int cjkEndInclusive = 0x9FFF;
        var max = cjkEndInclusive - cjkStart + 1;
        if (commonCjkCount > max)
            commonCjkCount = max;

        if (commonCjkCount > 0)
            AddRange(set, cjkStart, cjkStart + commonCjkCount - 1);
    }

    static void AddCommonSymbols(HashSet<int> set)
    {
        AddRange(set, 32, 126);
        AddRange(set, 160, 255);

        AddRange(set, 0x2000, 0x206F);
        AddRange(set, 0x3000, 0x303F);
        AddRange(set, 0xFF01, 0xFF60);
        AddRange(set, 0xFFE0, 0xFFEE);

        AddString(set, "，。！？；：、（）【】《》〈〉“”‘’…—·～￥（）－＋＝＿／\\|＂＇＃＆＠｀＾％＄\u3000");
    }

    static void AddRange(HashSet<int> set, int startInclusive, int endInclusive)
    {
        if (endInclusive < startInclusive)
            return;

        for (int cp = startInclusive; cp <= endInclusive; cp++)
            set.Add(cp);
    }

    static void AddString(HashSet<int> set, string s)
    {
        if (string.IsNullOrEmpty(s))
            return;

        for (int i = 0; i < s.Length; i++)
        {
            var ch = s[i];
            if (char.IsHighSurrogate(ch) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1]))
            {
                set.Add(char.ConvertToUtf32(ch, s[i + 1]));
                i++;
            }
            else
            {
                set.Add(ch);
            }
        }
    }
}