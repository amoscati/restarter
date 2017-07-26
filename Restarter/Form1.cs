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
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

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
            loadProcessList();

            refreshRunningProcesses();      
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            //checkedListBox1.Size = checkedListBox1.Parent.Size;
            //flowLayoutPanel1.Size = new Size(this.Size.Width, this.Size.Height - flowLayoutPanel2.Size.Height);
            treeView1.Size = new Size(this.Size.Width, treeView1.Size.Height);
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Save
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream("restarter.bin", FileMode.Create,
                FileAccess.Write, FileShare.None);
            List<string> processes = new List<string>();

            foreach (TreeNode node in treeView1.Nodes)
            {
                foreach (TreeNode cmdLineNode in node.Nodes)
                {
                    if (cmdLineNode.Checked) 
                    {
                        processes.Add(cmdLineNode.Text);
                    }
                }
                
            }
            formatter.Serialize(stream, processes);
            stream.Close();

            loadProcessList();
        }

        private void loadProcessList()
        {
            // Load
            checkedListBox1.Items.Clear();

            try
            {
                IFormatter formatter = new BinaryFormatter();
                Stream stream = new FileStream("restarter.bin", FileMode.Open,
                    FileAccess.Read, FileShare.None);
                List<string> processes = (List<string>)formatter.Deserialize(stream);
                stream.Close();

                foreach (var cmdLine in processes)
                {
                    checkedListBox1.Items.Add(cmdLine, true);
                }
            }
            catch(Exception ex)
            {

            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            foreach (var cmd in checkedListBox1.CheckedItems)
            {
                string cmdLine = cmd.ToString();

                string executable;
                string args;
                if (cmdLine[0] == '"')
                {
                    int i = cmdLine.IndexOf('"', 1);
                    executable = cmdLine.Substring(0, i + 1);
                    args = cmdLine.Substring(i + 1);

                }
                else
                {
                    int i = cmdLine.IndexOf(' ', 1);
                    executable = cmdLine.Substring(0, i + 1);
                    args = cmdLine.Substring(i);
                }

                Process process = new Process();
                // Configure the process using the StartInfo properties.
                process.StartInfo.FileName = executable.Trim();
                process.StartInfo.Arguments = args.Trim();
                process.StartInfo.UseShellExecute = true;
                process.Start();
            }
           
        }

        private void refreshRunningProcesses()
        {
            // Refresh
            treeView1.Nodes.Clear();

            var localProcesses = Process.GetProcesses().Where(
                x => x.SessionId == Process.GetCurrentProcess().SessionId &&
                x.MainWindowTitle.Length > 0).ToDictionary(x => x.Id);

            int maxChars = 0;
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
                    ProcessInfo pi = new ProcessInfo();
                    pi.Name = processName;
                    pi.CmdLine = cmdLine.ToString();

                    var nameNode = treeView1.Nodes[processName];
                    if (nameNode == null)
                    {
                        nameNode = new TreeNode(processName);
                        nameNode.Name = processName;
                        treeView1.Nodes.Add(nameNode);
                    }

                    var cmdLineLength = cmdLine.ToString().Length;
                    maxChars = maxChars < cmdLineLength ? cmdLineLength : maxChars;
                    var cmdNode = new TreeNode(cmdLine.ToString());
                    if (checkedListBox1.Items.Contains(cmdLine)) 
                    {
                        cmdNode.Checked = true;
                    }
                    nameNode.Nodes.Add(cmdNode);
                }
            }

            Size = new Size(maxChars * 6, Size.Height); // Empiric size
            treeView1.ExpandAll();

            Form1_Resize(this, null);
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            refreshRunningProcesses();
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

