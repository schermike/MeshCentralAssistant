﻿/*
Copyright 2009-2020 Intel Corporation

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/

using System;
using System.Drawing;
using System.Windows.Forms;
using System.ServiceProcess;
using System.Security.Principal;

namespace MeshAssistant
{
    public partial class MainForm : Form
    {
        public int timerSlowDown = 0;
        public bool allowShowDisplay = false;
        public bool doclose = false;
        public bool helpRequested = false;
        public MeshAgent agent = null;
        public int queryNumber = 0;
        public SnapShotForm snapShotForm = null;
        public RequestHelpForm requestHelpForm = null;
        public SessionsForm sessionsForm = null;
        public MeInfoForm meInfoForm = null;
        public bool isAdministrator = false;

        public MainForm()
        {
            InitializeComponent();
            agent = new MeshAgent();
            agent.onStateChanged += Agent_onStateChanged;
            agent.onQueryResult += Agent_onQueryResult;
            agent.onSessionChanged += Agent_onSessionChanged;
            agent.onAmtState += Agent_onAmtState;
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(Screen.PrimaryScreen.WorkingArea.Width - this.Width, Screen.PrimaryScreen.WorkingArea.Height - this.Height);
            agent.ConnectPipe();
            UpdateServiceStatus();

            pictureBox1.Visible = false;
            pictureBox2.Visible = false;
            pictureBox3.Visible = true;

            isAdministrator = IsAdministrator();
            if (isAdministrator)
            {
                startAgentToolStripMenuItem.Visible = true;
                stopAgentToolStripMenuItem.Visible = true;
                toolStripMenuItem2.Visible = true;
            }
            else
            {
                startAgentToolStripMenuItem.Visible = false;
                stopAgentToolStripMenuItem.Visible = false;
                toolStripMenuItem2.Visible = false;
            }
        }

        private void Agent_onAmtState(System.Collections.Generic.Dictionary<string, object> state)
        {
            if (this.InvokeRequired) { this.Invoke(new MeshAgent.onAmtStateHandler(Agent_onAmtState), state); return; }
            if (meInfoForm != null) { meInfoForm.updateInfo(state); }
        }

        private void Agent_onSessionChanged()
        {
            if (this.InvokeRequired) { this.Invoke(new MeshAgent.onSessionChangedHandler(Agent_onSessionChanged)); return; }

            // Called when sessions on the agent have changed.
            int count = 0;
            if (agent.DesktopSessions != null) { count += agent.DesktopSessions.Count; }
            if (agent.TerminalSessions != null) { count += agent.TerminalSessions.Count; }
            if (agent.FilesSessions != null) { count += agent.FilesSessions.Count; }
            if (agent.TcpSessions != null) { count += agent.TcpSessions.Count; }
            if (agent.UdpSessions != null) { count += agent.UdpSessions.Count; }
            if (count > 1) { mainNotifyIcon.BalloonTipText = count + " remote sessions are active."; remoteSessionsLabel.Text = (count + " remote sessions"); }
            if (count == 1) { mainNotifyIcon.BalloonTipText = "1 remote session is active."; remoteSessionsLabel.Text = "1 remote session"; }
            if (count == 0) { mainNotifyIcon.BalloonTipText = "No active remote sessions."; remoteSessionsLabel.Text = "No remote sessions"; }
            //mainNotifyIcon.ShowBalloonTip(2000);
            if (sessionsForm != null) { sessionsForm.UpdateInfo(); }
        }

        public void UpdateServiceStatus()
        {
            try
            {
                ServiceControllerStatus status = MeshAgent.GetServiceStatus();
                startAgentToolStripMenuItem.Enabled = (status == ServiceControllerStatus.Stopped);
                stopAgentToolStripMenuItem.Enabled = (status != ServiceControllerStatus.Stopped);
                if (agent.State != 0) return;
                pictureBox1.Visible = false; // Green
                pictureBox2.Visible = true;  // Red
                pictureBox3.Visible = false; // Yellow
                pictureBox4.Visible = false; // Help
                switch (status)
                {
                    case ServiceControllerStatus.ContinuePending: { stateLabel.Text = "Agent is continue pending"; break; }
                    case ServiceControllerStatus.Paused: { stateLabel.Text = "Agent is paused"; break; }
                    case ServiceControllerStatus.PausePending: { stateLabel.Text = "Agent is pause pending"; break; }
                    case ServiceControllerStatus.Running: { stateLabel.Text = "Agent is running"; break; }
                    case ServiceControllerStatus.StartPending: { stateLabel.Text = "Agent is start pending"; break; }
                    case ServiceControllerStatus.Stopped: { stateLabel.Text = "Agent is stopped"; break; }
                    case ServiceControllerStatus.StopPending: { stateLabel.Text = "Agent is stopped pending"; break; }
                }
            }
            catch (Exception)
            {
                startAgentToolStripMenuItem.Enabled = false;
                stopAgentToolStripMenuItem.Enabled = false;
                stateLabel.Text = "Agent not installed";
            }
        }

        public static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(allowShowDisplay ? value : allowShowDisplay);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Visible = false;
            this.WindowState = FormWindowState.Minimized;
        }

        private void Agent_onStateChanged(int state, int serverState)
        {
            if (this.InvokeRequired) {
                this.Invoke(new MeshAgent.onStateChangedHandler(Agent_onStateChanged), state, serverState);
                return;
            }
            bool openUrlVisible = ((state == 1) && (agent.ServerUri != null));
            try { openSiteToolStripMenuItem.Visible = openUrlVisible; } catch (Exception) { return; }
            switch (state)
            {
                case 0:
                    {
                        pictureBox1.Visible = false; // Green
                        pictureBox2.Visible = true;  // Red
                        pictureBox3.Visible = false; // Yellow
                        pictureBox4.Visible = false; // Help
                        UpdateServiceStatus();
                        requestHelpToolStripMenuItem.Enabled = false;
                        requestHelpToolStripMenuItem.Visible = true;
                        cancelHelpRequestToolStripMenuItem.Visible = false;
                        intelMEStateToolStripMenuItem.Visible = false;
                        intelAMTStateToolStripMenuItem.Visible = false;
                        break;
                    }
                case 1:
                    {
                        if (serverState == 1) {
                            pictureBox1.Visible = true;
                            pictureBox2.Visible = false;
                            pictureBox3.Visible = false;
                            pictureBox4.Visible = false;
                            stateLabel.Text = "Connected to server";
                            requestHelpToolStripMenuItem.Enabled = true;
                            requestHelpToolStripMenuItem.Visible = true;
                            cancelHelpRequestToolStripMenuItem.Visible = false;
                        } else {
                            pictureBox1.Visible = false;
                            pictureBox2.Visible = false;
                            pictureBox3.Visible = true;
                            pictureBox4.Visible = false;
                            stateLabel.Text = "Agent is active";
                            requestHelpToolStripMenuItem.Enabled = false;
                            requestHelpToolStripMenuItem.Visible = true;
                            cancelHelpRequestToolStripMenuItem.Visible = false;
                        }
                        intelMEStateToolStripMenuItem.Visible = agent.IntelAmtSupport;
                        intelAMTStateToolStripMenuItem.Visible = agent.IntelAmtSupport;
                        break;
                    }
            }
            helpRequested = false;
            requestHelpButton.Text = "Request Help";
            requestHelpButton.Enabled = ((state == 1) && (serverState == 1));

            if (isAdministrator && agent.ServiceAgent)
            {
                startAgentToolStripMenuItem.Visible = true;
                stopAgentToolStripMenuItem.Visible = true;
                toolStripMenuItem2.Visible = true;
            }
            else
            {
                startAgentToolStripMenuItem.Visible = false;
                stopAgentToolStripMenuItem.Visible = false;
                toolStripMenuItem2.Visible = false;
            }

            if ((state != 1) || (serverState != 1))
            {
                if (snapShotForm != null) { snapShotForm.Close(); }
                if (requestHelpForm != null) { requestHelpForm.Close(); }
            }
        }

        private void Agent_onQueryResult(string value, string result)
        {
            if (this.InvokeRequired) { this.Invoke(new MeshAgent.onQueryResultHandler(Agent_onQueryResult), value, result); return; }
            if (snapShotForm != null) { snapShotForm.displaySnapShot(result); }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            doclose = true;
            Application.Exit();
        }

        private void connectionTimer_Tick(object sender, EventArgs e)
        {
            if (timerSlowDown > 0) { timerSlowDown--; if (timerSlowDown == 0) { connectionTimer.Interval = 10000; } }
            if (agent.State == 0) { agent.ConnectPipe(); }
            UpdateServiceStatus();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (doclose == false) { e.Cancel = true; this.Visible = false; }
        }

        private void openSiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(agent.ServerUri.ToString());
        }

        private void mainNotifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left) {
                this.WindowState = FormWindowState.Normal;
                this.allowShowDisplay = true;
                openToolStripMenuItem.Visible = this.Visible;
                closeToolStripMenuItem.Visible = !this.Visible;
                this.Visible = !this.Visible;
            }
        }

        private void startAgentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MeshAgent.StartService();
            connectionTimer.Enabled = false;
            connectionTimer.Interval = 500;
            timerSlowDown = 20;
            connectionTimer.Enabled = true;
        }

        private void stopAgentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MeshAgent.StopService();
            connectionTimer.Enabled = false;
            connectionTimer.Interval = 500;
            timerSlowDown = 20;
            connectionTimer.Enabled = true;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.allowShowDisplay = true;
            openToolStripMenuItem.Visible = this.Visible;
            closeToolStripMenuItem.Visible = !this.Visible;
            this.Visible = !this.Visible;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F10)
            {
                if (snapShotForm == null)
                {
                    snapShotForm = new SnapShotForm(this);
                    snapShotForm.Show(this);
                }
                else
                {
                    snapShotForm.Focus();
                }
            }
        }

        private void requestHelpButton_Click(object sender, EventArgs e)
        {
            if (helpRequested == true)
            {
                if (agent.CancelHelpRequest() == true)
                {
                    helpRequested = false;
                    requestHelpButton.Text = "Request Help";
                    stateLabel.Text = "Connected to server";
                    requestHelpToolStripMenuItem.Visible = true;
                    cancelHelpRequestToolStripMenuItem.Visible = false;
                    pictureBox1.Visible = true;
                    pictureBox2.Visible = false;
                    pictureBox3.Visible = false;
                    pictureBox4.Visible = false;
                }
            }
            else
            {
                if (requestHelpForm != null)
                {
                    requestHelpForm.Focus();
                }
                else
                {
                    requestHelpForm = new RequestHelpForm(this);
                    requestHelpForm.Show(this);
                }
            }
        }

        public void RequestHelp(string details)
        {
            if (agent.RequestHelp(details) == true)
            {
                helpRequested = true;
                requestHelpButton.Text = "Cancel Help Request";
                stateLabel.Text = "Help requested";
                requestHelpToolStripMenuItem.Visible = false;
                cancelHelpRequestToolStripMenuItem.Visible = true;
                pictureBox1.Visible = false;
                pictureBox2.Visible = false;
                pictureBox3.Visible = false;
                pictureBox4.Visible = true;
            }
        }

        private void remoteSessionsLabel_Click(object sender, EventArgs e)
        {
            if (sessionsForm != null)
            {
                sessionsForm.Focus();
            }
            else
            {
                sessionsForm = new SessionsForm(this);
                sessionsForm.Show(this);
            }
        }

        private void dialogContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            requestHelpToolStripMenuItem.Enabled = (agent.State != 0);
        }

        private void intelAMTStateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (meInfoForm != null)
            {
                meInfoForm.Focus();
            }
            else
            {
                meInfoForm = new MeInfoForm(this);
                meInfoForm.Show(this);
            }
        }
    }
}
