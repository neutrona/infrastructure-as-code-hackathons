
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace shift.ui.architect.ExtensionMethods
{
    static class WinFormControls
    {
        // RichTextBox

        public static void AppendParagraphAutoScroll(this RichTextBox a, string text)
        {
            a.AppendAutoScroll("\n" + text + "\n");
        }

        public static void AppendLineAutoScroll(this RichTextBox a, string text)
        {
            a.AppendAutoScroll(text + "\n");
        }

        public static void AppendAutoScroll(this RichTextBox a, string text)
        {

            if (a.InvokeRequired)
            {
                a.BeginInvoke(new MethodInvoker(() => AppendAutoScroll(a, text)));
            }
            else
            {

                string[] parts = Regex.Split(text, @"(\u001b\[\d+m)");

                var color = a.ForeColor;

                foreach (var part in parts)
                {
                    if (Regex.IsMatch(part, @"\u001b\[\d+m"))
                    {
                        color = a.GetANSI8Color(part);
                    }
                    else
                    {
                        a.SelectionStart = a.TextLength;
                        a.SelectionLength = 0;
                        a.SelectionColor = color;
                        a.AppendText(part);
                    }
                }

                a.SelectionStart = a.Text.Length;
                a.ScrollToCaret();
            }
        }

        public static System.Drawing.Color GetANSI8Color(this RichTextBox a, string code)
        {
            switch (code)
            {
                case "\u001b[0m":
                    return a.ForeColor;
                case "\u001b[30m":
                    return System.Drawing.Color.Black;
                case "\u001b[31m":
                    return System.Drawing.Color.Red;
                case "\u001b[32m":
                    return System.Drawing.Color.Green;
                case "\u001b[33m":
                    return System.Drawing.Color.Yellow;
                case "\u001b[34m":
                    return System.Drawing.Color.Blue;
                case "\u001b[35m":
                    return System.Drawing.Color.Magenta;
                case "\u001b[36m":
                    return System.Drawing.Color.Cyan;
                case "\u001b[37m":
                    return System.Drawing.Color.White;
                default:
                    return a.ForeColor;
            }

            /*
                ANSI 8 

                Black: \u001b[30m
                Red: \u001b[31m
                Green: \u001b[32m
                Yellow: \u001b[33m
                Blue: \u001b[34m
                Magenta: \u001b[35m
                Cyan: \u001b[36m
                White: \u001b[37m

                Reset: \u001b[0m

            */
        }
    }
}
