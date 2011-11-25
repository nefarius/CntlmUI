using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using CntlmUI.Properties;
using System.Net;
using System.Diagnostics;
using Microsoft.Win32;
using System.Reflection;

namespace CntlmUI
{
    public partial class CntlmUI : Form
    {
        private bool mAllowVisible;     // ContextMenu's Show command used
        private bool mAllowClose;       // ContextMenu's Exit command used
        private bool mLoadFired;        // Form was shown once
        private Settings config = Settings.Default;
        private int cntlmPid = 0;
        private const string RUN_LOCATION = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string VALUE_NAME = "CntlmUI";
        private string CntlmBinary
        {
            get { return Path.Combine(Path.GetDirectoryName(CurrentApplicationLocation), "cntlm.exe"); }
        }

        public CntlmUI()
        {
            InitializeComponent();

            mAllowVisible = config.FirstRun;
            if (config.Autoconnect && !config.FirstRun)
            {
                Connect();
            }
        }

        /// <summary>
        /// Full name of the current running assembly instance.
        /// </summary>
        public static string CurrentApplicationLocation
        {
            get { return Assembly.GetExecutingAssembly().Location; }
        }

        /// <summary>
        /// Add to windows autostart.
        /// </summary>
        public void SetAutoStart()
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(RUN_LOCATION);
            key.SetValue(VALUE_NAME, CurrentApplicationLocation);
        }

        /// <summary>
        /// Remove from windows autostart.
        /// </summary>
        public void UnSetAutoStart()
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(RUN_LOCATION);
            key.DeleteValue(VALUE_NAME);
        }

        /// <summary>
        /// Checks if application is in autostart list.
        /// </summary>
        public bool IsAutoStartEnabled
        {
            get
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_LOCATION);
                if (key == null)
                    return false;

                string value = (string)key.GetValue(VALUE_NAME);
                if (value == null)
                    return false;
                return (value == CurrentApplicationLocation);
            }
        }

        private void CntlmUI_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(e.CloseReason == CloseReason.UserClosing)
            {
                this.Hide();
                e.Cancel = true;
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            if (!mAllowVisible) value = false;
            base.SetVisibleCore(value);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!mAllowClose)
            {
                this.Hide();
                e.Cancel = true;
            }
            base.OnFormClosing(e);
        }

        private void notifyIconSysTray_DoubleClick(object sender, EventArgs e)
        {
            mAllowVisible = true;
            mLoadFired = true;
            Show();
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Disconnect();
            mAllowClose = mAllowVisible = true;
            if (!mLoadFired) Show();
            Application.Exit();
        }

        private void connectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (cntlmPid == 0)
            {
                Connect();
            }
            else
            {
                Disconnect();
            }
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mAllowVisible = true;
            mLoadFired = true;
            Show();
        }

        private void CntlmUI_Shown(object sender, EventArgs e)
        {
            if (config.FirstRun)
            {
                IWebProxy iProxy = WebRequest.DefaultWebProxy;
                Uri website = new Uri("http://www.microsoft.com");
                WebProxy proxy = new WebProxy(iProxy.GetProxy(website));
                if (proxy.Address != website)
                {
                    config.Proxy = proxy.Address.AbsoluteUri;
                }

                config.Username = Environment.UserName;
                config.Domain = Environment.UserDomainName;

                if (radioButtonNTLM.Checked)
                    config.AuthMode = radioButtonNTLM.Text;
                if (radioButtonLM.Checked)
                    config.AuthMode = radioButtonLM.Text;
                if (radioButtonNT.Checked)
                    config.AuthMode = radioButtonNT.Text;

                config.FirstRun = false;
            }

            textBoxUser.Text = config.Username;
            textBoxDomain.Text = config.Domain;
            textBoxProxy.Text = config.Proxy;
            switch (config.AuthMode)
            {
                case "NTLM":
                    radioButtonNTLM.Checked = true;
                    break;
                case "LM":
                    radioButtonLM.Checked = true;
                    break;
                case "NT":
                    radioButtonNT.Checked = true;
                    break;
            }
            textBoxListen.Text = config.Listen;
            checkBoxAutostart.Checked = IsAutoStartEnabled;
            checkBoxAutoconnect.Checked = config.Autoconnect;
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxPass.Text))
            {
                config.Password = GetPasswordHash(textBoxPass.Text);
            }
            config.Username = textBoxUser.Text;
            config.Domain = textBoxDomain.Text;
            config.Proxy = textBoxProxy.Text;
            if (radioButtonNTLM.Checked)
                config.AuthMode = radioButtonNTLM.Text;
            if (radioButtonLM.Checked)
                config.AuthMode = radioButtonLM.Text;
            if (radioButtonNT.Checked)
                config.AuthMode = radioButtonNT.Text;
            config.Listen = textBoxListen.Text;
            config.Save();
            this.Hide();
        }

        private string GetPasswordHash(string plain)
        {
            Regex PassNTLMv2 = new Regex(@"^PassNTLMv2\s*(\S*)\s*", RegexOptions.Multiline);

            Process cntlm = new Process();
            cntlm.StartInfo.FileName = CntlmBinary;
            cntlm.StartInfo.Arguments = string.Format("-u {0} -d {1} -H -p {2}", 
                config.Username, config.Domain, plain);
            cntlm.StartInfo.UseShellExecute = false;
            cntlm.StartInfo.CreateNoWindow = true;
            cntlm.StartInfo.RedirectStandardOutput = true;
            cntlm.Start();

            cntlm.WaitForExit();
            string output = cntlm.StandardOutput.ReadToEnd();
            Match hashes = PassNTLMv2.Match(output);

            return hashes.Groups[1].Value;
        }

        private void Connect()
        {
            Uri proxy = new Uri(config.Proxy);
            Process cntlm = new Process();
            cntlm.StartInfo.FileName = CntlmBinary;
            cntlm.StartInfo.Arguments = string.Format("-v -a {6} -u {0} -d {1} -p {2} -l {3} {4}:{5}",
                config.Username,
                config.Domain,
                config.Password,
                config.Listen,
                proxy.Host,
                proxy.Port,
                config.AuthMode.ToLower());
            cntlm.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            cntlm.Start();

            cntlmPid = cntlm.Id;
            connectToolStripMenuItem.Text = "Disconnect";
            notifyIconSysTray.ShowBalloonTip(500,
                "Connected",
                string.Format("Listening on {0}", config.Listen),
                ToolTipIcon.Info);
        }

        private void Disconnect()
        {
            if (cntlmPid != 0)
            {
                Process cntlmProc = Process.GetProcessById(cntlmPid);
                cntlmProc.Kill();
                cntlmPid = 0;
                connectToolStripMenuItem.Text = "Connect";
                notifyIconSysTray.ShowBalloonTip(500,
                    "Disconnected",
                    "Proxy process terminated.",
                    ToolTipIcon.Info);
            }
        }

        private void checkBoxAutostart_CheckedChanged(object sender, EventArgs e)
        {
            if ((sender as CheckBox).Checked)
            {
                SetAutoStart();
            }
            else
            {
                UnSetAutoStart();
            }
        }

        private void checkBoxAutoconnect_CheckedChanged(object sender, EventArgs e)
        {
            config.Autoconnect = (sender as CheckBox).Checked;
        }
    }
}
