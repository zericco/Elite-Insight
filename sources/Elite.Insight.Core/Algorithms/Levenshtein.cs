using System;

namespace Elite.Insight.Core.Algorithms
{
    public class Levenshtein
    {
		  /// <summary>
		  /// Compute the accuracy matching.
		  /// </summary>
		  public static double Match(string s, string t)
		  {
			  int lev = Compute(s,t);
			  if (lev > 0)
			  {
				  var max = Math.Max(s.Length, t.Length);
				  return (100 * (max - lev)) / (double)max;
			  }
			  else
			  {
				  return 0.0;
			  }
		  }

	    public static int Compute(string s, string t)
	    {
		    int n = s.Length;
		    int m = t.Length;
		    // Step 1
		    if (n == 0)
		    {
			    return m;
		    }

		    if (m == 0)
		    {
			    return n;
		    }

		    s = s.ToLowerInvariant();
		    t = t.ToLowerInvariant();

		    int[,] d = new int[n + 1, m + 1];

		    // Step 2
		    for (int i = 0; i <= n; ++i)
		    {
			    d[i, 0] = i;
		    }

		    for (int j = 0; j <= m; ++j)
		    {
			    d[0, j] = j;
		    }

		    // Step 3
		    for (int i = 1; i <= n; ++i)
		    {
			    //Step 4
			    for (int j = 1; j <= m; ++j)
			    {
				    // Step 5
				    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

				    // Step 6
				    d[i, j] = Math.Min(
					    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
					    d[i - 1, j - 1] + cost);
			    }
		    }
		    // Step 7
		    return d[n, m];
	    }
    }
}
