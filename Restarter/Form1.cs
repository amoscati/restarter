using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Management;

namespace Restarter
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public struct ProcessInfo
        {
            public string Name { get; set; }
            public string CmdLine { get; set; }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            // the code that you want to measure comes here
            
            List<object> processes = new List<object>();
            var localProcesses = Process.GetProcesses().Where(
                x => x.SessionId == Process.GetCurrentProcess().SessionId &&
                x.MainWindowTitle.Length > 0).ToDictionary(x => x.Id);

            ManagementClass mgmtClass = new ManagementClass("Win32_Process");
            foreach (ManagementObject process in mgmtClass.GetInstances())
            {
                // Only current session (user)
                if (process["SessionId"].ToString() != Process.GetCurrentProcess().SessionId.ToString())
                {
                    continue;
                }

                // Only UI processes
                uint id = (uint)process["ProcessId"];
                if (!localProcesses.ContainsKey((int)id))
                {
                    continue;
                }

                string processName = process["Name"].ToString();

                object cmdLine = process["CommandLine"];
                if (cmdLine != null)
                {
                    ProcessInfo pi = new ProcessInfo ();
                    pi.Name = processName;
                    pi.CmdLine = cmdLine.ToString();
                    processes.Add(pi);
                }
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;

            //checkedListBox1.Items.AddRange(processes.ToArray());
            //checkedListBox1.DisplayMember = "CmdLine";
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            checkedListBox1.Size = checkedListBox1.Parent.Size;
        }
    }

    public static class Utilities
    {
        public static string GetCommandLine(this Process process)
        {
            var commandLine = new StringBuilder(process.MainModule.FileName);

            commandLine.Append(" ");
            using (var searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
            {
                foreach (var @object in searcher.Get())
                {
                    commandLine.Append(@object["CommandLine"]);
                    commandLine.Append(" ");
                }
            }

            return commandLine.ToString();
        }
    }
}

