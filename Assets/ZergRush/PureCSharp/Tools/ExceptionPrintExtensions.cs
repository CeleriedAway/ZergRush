using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

public static partial class ExceptionPrintExtensions
{
    public static string ToError(this Exception e)
    {
        var str = new StringBuilder();
        ToError(e, str, "");
        return str.ToString();
    }
    
    static void ToError(this Exception e, StringBuilder str, string prefix)
    {
        str.Append("<color=red>");
        str.Append(e.Message).Append("\n");
        str.Append("</color>");
        var st = new StackTrace(e, true).GetFrames();
        if (st == null)
        {
            str.Append(" (no stacktrace frames)");
            return;
        }
        foreach (var stackFrame in st)
        {
            var fileName = stackFrame.GetFileName();
            if (string.IsNullOrWhiteSpace(fileName)) continue;
            var shortName = fileName.Substring(fileName.IndexOf("Assets") + 7);
            var fileLineNumber = stackFrame.GetFileLineNumber();
            str.Append(prefix);
            str.Append("    ");
            str.Append("<a href=\"");
            str.Append(fileName);
            str.Append("\" line=\"");
            str.Append(fileLineNumber);
            str.Append("\">");
            str.Append(shortName);
            str.Append(":");
            str.Append(fileLineNumber);
            str.Append("</a>");
            str.Append("\n");
            str.Append(prefix);
            str.Append("    ");
            str.Append(stackFrame.GetMethod().DeclaringType.GetNiceName());
            str.Append(":");
            str.Append(stackFrame.GetMethod().GetFullName());
            str.Append("\n");
        }

        if (e.InnerException != null)
        {
            str.Append(prefix);
            str.Append("    ");
            str.Append("Inner exception --->");
            ToError(e.InnerException, str, prefix + "    ");
        }
    }
}
