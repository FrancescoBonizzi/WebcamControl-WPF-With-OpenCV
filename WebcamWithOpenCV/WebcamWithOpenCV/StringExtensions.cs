using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebcamWithOpenCV
{
    public static class StringExtensions
    {
        public static string SafeSubstring(this string text, int start, int length)
        {
            return text.Length <= start ? ""
                : text.Length - start <= length ? text.Substring(start)
                : text.Substring(start, length);
        }
    }
}
