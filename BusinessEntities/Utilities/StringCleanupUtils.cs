namespace ro.stancescu.CDep.BusinessEntities.Utilities
{
    public static class StringCleanupUtils
    {
        public static string CleanupToUtf8(string rawString, out bool cleanedUp)
        {
            cleanedUp = false;

            if (rawString.Contains("Ä\u0083"))
            {
                rawString = rawString.Replace("Ä\u0083", "ă");
                cleanedUp = true;
            }

            if (rawString.Contains("Ĺ\u009f"))
            {
                rawString = rawString.Replace("Ĺ\u009f", "ș");
                cleanedUp = true;
            }

            if (rawString.Contains("ĹŁ"))
            {
                rawString = rawString.Replace("ĹŁ", "ț");
                cleanedUp = true;
            }

            if (rawString.Contains("Ă˘"))
            {
                rawString = rawString.Replace("Ă˘", "â");
                cleanedUp = true;
            }

            if (rawString.Contains("ĂĄ"))
            {
                rawString = rawString.Replace("ĂĄ", "á");
                cleanedUp = true;
            }

            if (rawString.Contains("ĂŠ"))
            {
                rawString = rawString.Replace("ĂŠ", "é");
                cleanedUp = true;
            }

            if (rawString.Contains("ĂŽ"))
            {
                rawString = rawString.Replace("ĂŽ", "î");
                cleanedUp = true;
            }

            if (rawString.Contains("Ăł"))
            {
                rawString = rawString.Replace("Ăł", "ó");
                cleanedUp = true;
            }

            if (rawString.Contains("Ĺ˘"))
            {
                rawString = rawString.Replace("Ĺ˘", "Ţ");
                cleanedUp = true;
            }

            if (rawString.Contains("Ĺ\u009e"))
            {
                rawString = rawString.Replace("Ĺ\u009e", "Ş");
                cleanedUp = true;
            }

            if (rawString.Contains("Ăś"))
            {
                rawString = rawString.Replace("Ăś", "ö");
                cleanedUp = true;
            }

            if (rawString.Contains(" -"))
            {
                rawString = rawString.Replace(" -", "-");
                cleanedUp = true;
            }

            if (rawString.Contains("- "))
            {
                rawString = rawString.Replace("- ", "-");
                cleanedUp = true;
            }

            if (rawString.Contains("Ĺ\u0090"))
            {
                rawString = rawString.Replace("Ĺ\u0090", "Ő");
                cleanedUp = true;
            }

            if (rawString.Contains("Ĺ\u0091"))
            {
                rawString = rawString.Replace("Ĺ\u0091", "ő");
                cleanedUp = true;
            }

            return rawString;
        }
    }
}
