using System;
using System.Collections.Generic;
using System.Text;

namespace Job_Finder_Bot.Utilities
{
    public static class JobUrlHelper
    {
        public static string NormalizeJobUrl(string sourceUrl)
        {
            try
            {
                var uri = new Uri(sourceUrl);

                var pathParts = uri.AbsolutePath
                    .Split('/', StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < pathParts.Length - 1; i++)
                {
                    if (pathParts[i] == "ad")
                    {
                        return pathParts[i + 1];
                    }
                }

                return uri.GetLeftPart(UriPartial.Path);
            }
            catch
            {
                return sourceUrl;
            }
        }
    }
}
