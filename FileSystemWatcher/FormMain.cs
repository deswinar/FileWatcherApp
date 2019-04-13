using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
//using System.Threading.Tasks;


namespace FileChangeNotifier
{
    public partial class frmNotifier : Form
    {
        private StringBuilder m_Sb;
        private bool m_bDirty;
        private System.IO.FileSystemWatcher m_Watcher;
        private bool m_bIsWatching;
        private string text;

        public frmNotifier()
        {
            InitializeComponent();
            m_Sb = new StringBuilder();            
            m_bDirty = false;
            m_bIsWatching = false;
        }

        void runPython()
        {
            // full path of python interpreter
            string python = @"C:\Python27\pythonw.exe";

            // python app to call
            string myPythonApp = @"H:\testcode\write_file.py";

            // Create new process start info
            ProcessStartInfo myProcessStartInfo = new ProcessStartInfo(python);

            myProcessStartInfo.UseShellExecute = false;
            myProcessStartInfo.RedirectStandardOutput = true;

            myProcessStartInfo.Arguments = myPythonApp;

            Process myProcess = new Process();
            // assign start information to the process
            myProcess.StartInfo = myProcessStartInfo;
            
            // start the process
            myProcess.Start();

            myProcess.WaitForExit();
            myProcess.Close();
        }

        private void btnWatchFile_Click(object sender, EventArgs e)
        {
            // Create Thread to run python file
            Thread t = new Thread(new ThreadStart(runPython));
            
            // Stop Watching
            if (m_bIsWatching)
            {
                // Stop Thread to run python file
                t.Abort();
                // Kill started python file
                Process[] processlist = Process.GetProcesses();                
                foreach (Process theprocess in processlist)
                {
                    if (theprocess.ProcessName == "pythonw")
                    {
                        theprocess.Kill();
                    }
                }
                

                m_bIsWatching = false;
                m_Watcher.EnableRaisingEvents = false;
                m_Watcher.Dispose();
                btnWatchFile.BackColor = Color.LightSkyBlue;
                btnWatchFile.Text = "Start Watching";
                
            }
            // Start Watching
            else
            {
                // Start Thread to run python file
                t.Start();

                m_bIsWatching = true;
                btnWatchFile.BackColor = Color.Red;
                btnWatchFile.Text = "Stop Watching";

                m_Watcher = new System.IO.FileSystemWatcher();
                if (rdbDir.Checked)
                {
                    m_Watcher.Filter = "*.*";
                    m_Watcher.Path = txtFile.Text + "\\";
                }
                else
                {
                    m_Watcher.Filter = txtFile.Text.Substring(txtFile.Text.LastIndexOf('\\') + 1);
                    m_Watcher.Path = txtFile.Text.Substring(0, txtFile.Text.Length - m_Watcher.Filter.Length);
                    //m_Watcher.Filter = "test.txt";
                    //m_Watcher.Path = "H:/C Sharp Projects/";
                }

                if (chkSubFolder.Checked)
                {
                    m_Watcher.IncludeSubdirectories = true;
                }

                m_Watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                     | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                m_Watcher.Changed += new FileSystemEventHandler(OnChanged);
                m_Watcher.Created += new FileSystemEventHandler(OnChanged);
                m_Watcher.Deleted += new FileSystemEventHandler(OnChanged);
                m_Watcher.Renamed += new RenamedEventHandler(OnRenamed);
                m_Watcher.EnableRaisingEvents = true;
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (!m_bDirty)
            {
                FileStream logFileStream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                StreamReader logFileReader = new StreamReader(logFileStream);

                text = logFileReader.ReadLine();

                if(!String.IsNullOrEmpty(text))
                {
                    m_Sb.Remove(0, m_Sb.Length);
                    //m_Sb.Append(e.FullPath);
                    //m_Sb.Append(" ");
                    //m_Sb.Append(e.ChangeType.ToString());
                    //m_Sb.Append("    ");
                    //m_Sb.Append(DateTime.Now.ToString());
                    m_Sb.Append("Value = ");
                    m_Sb.Append(text);
                    m_bDirty = true;
                }
                
                

                // Clean up
                logFileReader.Close();
                logFileStream.Close();
            }
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            if (!m_bDirty)
            {
                m_Sb.Remove(0, m_Sb.Length);
                m_Sb.Append(e.OldFullPath);
                m_Sb.Append(" ");
                m_Sb.Append(e.ChangeType.ToString());
                m_Sb.Append(" ");
                m_Sb.Append("to ");
                m_Sb.Append(e.Name);
                m_Sb.Append("    ");
                m_Sb.Append(DateTime.Now.ToString());
                m_bDirty = true;
                if (rdbFile.Checked)
                {
                    m_Watcher.Filter = e.Name;
                    m_Watcher.Path = e.FullPath.Substring(0, e.FullPath.Length - m_Watcher.Filter.Length);
                }
            }            
        }

        private void tmrEditNotify_Tick(object sender, EventArgs e)
        {
            if (m_bDirty)
            {
                lstNotification.BeginUpdate();
                lstNotification.Items.Add(m_Sb.ToString());
                lstNotification.EndUpdate();
                textBox1.Text = text;
                m_bDirty = false;
            }
        }

        private void btnBrowseFile_Click(object sender, EventArgs e)
        {
            if (rdbDir.Checked)
            {
                DialogResult resDialog = dlgOpenDir.ShowDialog();
                if (resDialog.ToString() == "OK")
                {
                    txtFile.Text = dlgOpenDir.SelectedPath;
                }
            }
            else
            {
                DialogResult resDialog = dlgOpenFile.ShowDialog();
                if (resDialog.ToString() == "OK")
                {
                    txtFile.Text = dlgOpenFile.FileName;
                }
            }
        }

        private void btnLog_Click(object sender, EventArgs e)
        {
            DialogResult resDialog = dlgSaveFile.ShowDialog();
            if (resDialog.ToString() == "OK")
            {
                FileInfo fi = new FileInfo(dlgSaveFile.FileName);
                StreamWriter sw = fi.CreateText();
                foreach (string sItem in lstNotification.Items)
                {
                    sw.WriteLine(sItem);
                }
                sw.Close();
            }
        }

        private void rdbFile_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbFile.Checked == true)
            {
                chkSubFolder.Enabled = false;
                chkSubFolder.Checked = false;
            }
        }

        private void rdbDir_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbDir.Checked == true)
            {
                chkSubFolder.Enabled = true;
            }
        }

        private void frmNotifier_Load(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}