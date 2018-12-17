using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NeuronDemonstrations
{
    class FastProcess
    {
        public RichTextBox RichTextBox { get; private set; }

        public FastProcess(RichTextBox richTextBox)
        {
            RichTextBox = richTextBox;
        }

        private void OutputToTextBox(object sendingProcess, DataReceivedEventArgs outLine)
        {
            string data = outLine.Data;
            if (!string.IsNullOrEmpty(data))
            {
                RichTextBox.Invoke(new MethodInvoker(() => RichTextBox.AppendText("\n" + data)));
            }
        }

        public Process Run(string app, string args, string dir = null, bool adm = false)
        {
            if (string.IsNullOrEmpty(dir)) dir = Environment.CurrentDirectory;
            Process process = new Process();
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = app;
            process.StartInfo.Arguments = args;
            if (adm)
            {
                process.StartInfo.Verb = "runas";
                process.Start();
                return process;
            }
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += new DataReceivedEventHandler(OutputToTextBox);
            process.ErrorDataReceived += new DataReceivedEventHandler(OutputToTextBox);
            process.StartInfo.StandardOutputEncoding = Encoding.GetEncoding(866);
            process.StartInfo.StandardErrorEncoding = Encoding.GetEncoding(866);
            process.StartInfo.WorkingDirectory = dir;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            return process;
        }
    }
}
