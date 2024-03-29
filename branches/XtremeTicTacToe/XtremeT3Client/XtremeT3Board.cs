﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Remoting;
using XtremeT3Library;

namespace XtremeT3Client
{
    public partial class XtremeT3Board : Form
    {
        private Guid callbackId;
        private const int CELL_MARGIN = 16;     // Used for aligning grid cells 
        private const int CELL_WIDTH = 80;      // Used for aligning grid cells 
        private Label[] cells = new Label[9];   // Tic-Tac-Toe cell controls
        private IXT3GameState gameState;
        private string server = "";
        private string name = "";

        public XtremeT3Board()
        {
            InitializeComponent();
            Greeting greetings = new Greeting();
            greetings.parentForm = this;
            if (greetings.ShowDialog() == DialogResult.OK)
            {
                if (greetings.UserName != "")
                {
                    name = greetings.UserName;
                    server = greetings.Server;
                }
            }
            try
            {
                // Load the remoting config file 
                RemotingConfiguration.Configure("XtremeT3Client.exe.config", false);

                // Activate a TicTacToeLibrary.Locations object 
                gameState = (IXT3GameState)Activator.GetObject(typeof(IXT3GameState), "http://" + server + ":10001/xt3gamestate.soap");

                // Register this client instance for server callbacks 
                callbackId = gameState.RegisterCallback(new XServerUpdates(this), name);
                if (callbackId.Equals(Guid.Empty))
                {
                    throw new Exception("There are already two players. You cannot be added at this time.");
                }

                // Display client's GUID in form caption 
                this.Text = name;

                // Setup label controls for grid cells 
                createCells();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Tic-Tac-Toe Error");
            }
        }

        public void createCells()
        {
            int x, y; // horizontal & vertical positions 
            int xIdx, yIdx; // horizontal & vertical indexes 
            for (int i = 0; i < cells.Length; ++i)
            {
                // Get horizontal and vertical offsets 
                xIdx = i % 3;
                yIdx = i / 3;

                // Get positional coordinates for each cell 
                x = CELL_MARGIN + xIdx * CELL_WIDTH;
                y = CELL_MARGIN + yIdx * CELL_WIDTH;

                // Initialize label that represents this card 
                cells[i] = new Label();
                cells[i].BackColor = System.Drawing.SystemColors.ControlLightLight;
                cells[i].Location = new System.Drawing.Point(x, y);
                cells[i].TabIndex = i;  // Use to identify cell index 
                cells[i].BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
                cells[i].Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
                cells[i].Size = new System.Drawing.Size(CELL_WIDTH, CELL_WIDTH);
                cells[i].TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
                cells[i].Text = "";
                cells[i].Click += new System.EventHandler(cell_Click);
                Controls.Add(cells[i]);
            }
        }

        private void cell_Click(object sender, System.EventArgs e)
        {
            try
            {
                if (!btnNew.Enabled)
                {
                    Label cell = (Label)sender;

                    // Label's TabIndex property indicates position in grid 
                    int cellIdx = ((Label)sender).TabIndex;

                    // Process the requested move 
                    gameState.userSelection(cellIdx, callbackId);
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Tic-Tac-Toe Error");
            }
        }

        private delegate void FormUpdatePlayers(string[] names);
        public void UpdatePlayers(string[] names)
        {
            if (names != null && names.Length != 0)
            {
                try
                {
                    if (lblOpposition.InvokeRequired)
                        lblOpposition.BeginInvoke(new FormUpdatePlayers(UpdatePlayers), new Object[] { names });
                    else
                    {
                        foreach (string n in names)
                        {
                            if (name != n.ToString())
                            {
                                lblOpposition.Text = n;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private delegate void FormUpdateBoard(char[] board);
        public void UpdateBoard(char[] board)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    // This will happen if current thread isn't the UI's own thread
                    this.BeginInvoke(new FormUpdateBoard(UpdateBoard), new Object[] { board });
                }
                else
                {
                    for (int i = 0; i < cells.Length; i++)
                    {
                        cells[i].Text = board[i].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message, "Tic-Tac-Toe Error");
            }
        }

        private delegate void FormReportWinner();
        public void ReportWinner()
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new FormReportWinner(ReportWinner), null);
            else
            {
                if (gameState.getWinner() == XtremeWho.Who.USER)
                    MessageBox.Show("Game Over! " + (char)XtremeWho.Symbol.USER + " is the winner.");
                else if (gameState.getWinner() == XtremeWho.Who.USER2)
                    MessageBox.Show("Game Over! " + (char)XtremeWho.Symbol.USER2 + " is the winner.");
                else
                    MessageBox.Show("Game Over! It's a draw!");
                gameState.GameDone();
            }
        }

        private delegate void FormEnableButtons();
        public void EnableButtons()
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new FormEnableButtons(EnableButtons), null);
            else
                btnNew.Enabled = true;
        }

        private delegate void FormDisableButtons();
        public void DisableButtons()
        {
            if (this.InvokeRequired)
                this.BeginInvoke(new FormDisableButtons(DisableButtons), null);
            else
                btnNew.Enabled = false;
        }

        private void XtremeT3Board_Load(object sender, EventArgs e)
        {

        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            gameState.Reset();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
            try
            {
                gameState.UnregisterCallback(callbackId, name);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
