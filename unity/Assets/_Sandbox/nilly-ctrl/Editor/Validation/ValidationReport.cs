using System.Collections.Generic;
using System.Text;

namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>Counting and formatting for a list of findings. Pure C#, so it can be unit tested.</summary>
    public static class ValidationReport
    {
        public static int Count(IEnumerable<ValidationFinding> findings, ValidationSeverity severity)
        {
            int n = 0;
            if (findings == null)
            {
                return 0;
            }

            foreach (ValidationFinding finding in findings)
            {
                if (finding != null && finding.Severity == severity)
                {
                    n++;
                }
            }

            return n;
        }

        /// <summary>Process exit code for a batch run: 1 if there is any error, else 0.</summary>
        public static int ExitCode(IEnumerable<ValidationFinding> findings)
        {
            return Count(findings, ValidationSeverity.Error) > 0 ? 1 : 0;
        }

        public static string Summary(IEnumerable<ValidationFinding> findings)
        {
            return $"{Count(findings, ValidationSeverity.Error)} error(s), " +
                   $"{Count(findings, ValidationSeverity.Warning)} warning(s), " +
                   $"{Count(findings, ValidationSeverity.Info)} info";
        }

        /// <summary>Plain text, worst first, one line per finding.</summary>
        public static string ToText(IEnumerable<ValidationFinding> findings)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Hold the Hill project validator: " + Summary(findings));
            if (findings == null)
            {
                return sb.ToString();
            }

            for (int severity = (int)ValidationSeverity.Error; severity >= 0; severity--)
            {
                foreach (ValidationFinding finding in findings)
                {
                    if (finding != null && (int)finding.Severity == severity)
                    {
                        sb.AppendLine(finding.ToString());
                    }
                }
            }

            return sb.ToString();
        }
    }
}
