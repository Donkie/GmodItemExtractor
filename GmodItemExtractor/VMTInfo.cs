using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GmodItemExtractor
{
    // ReSharper disable once InconsistentNaming
    public class VMTInfo
    {
        public static string[] GetTextureFiles(string vmtdata)
        {
            Regex r = new Regex(@"\$\w*(?:texture|bumpmap|detail|mask)["" \t]+([^\r\n""]+)", RegexOptions.IgnoreCase);

            return (
                from Match m in r.Matches(vmtdata)
                where m.Groups.Count > 1
                select Program.CleanPath(Path.ChangeExtension(Path.Combine("materials", m.Groups[1].Value), ".vtf"))
            ).ToArray();
        }
    }
}