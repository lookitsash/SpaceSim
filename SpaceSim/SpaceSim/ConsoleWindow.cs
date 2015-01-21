using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SpaceSim
{
    public class ConsoleWindow
    {
        private System.Windows.Forms.Form debugForm;
        private System.Windows.Forms.TextBox outputTextBox, inputTextBox;
        
        private int maxEntries = 100;
        private int currentEntries = 0;

        public delegate void ConsoleInputEventHandler(string str);

        public event ConsoleInputEventHandler OnInput;

        public ConsoleWindow()
        {
            debugForm = new System.Windows.Forms.Form();
            debugForm.Text = "Console";
            debugForm.Width = 500;

            inputTextBox = new System.Windows.Forms.TextBox();
            inputTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Bottom;
            inputTextBox.Location = new System.Drawing.Point(0, debugForm.ClientSize.Height - inputTextBox.Height);
            inputTextBox.Width = debugForm.ClientSize.Width;
            inputTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(KeyEventHandler);

            outputTextBox = new System.Windows.Forms.TextBox();
            outputTextBox.Multiline = true;
            outputTextBox.Width = debugForm.ClientSize.Width;
            outputTextBox.Height = debugForm.ClientSize.Height - inputTextBox.Height;
            outputTextBox.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right | System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom;
            outputTextBox.ReadOnly = true;
            outputTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;

            debugForm.Controls.Add(outputTextBox);
            debugForm.Controls.Add(inputTextBox);
        }

        private void KeyEventHandler(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Enter)
            {
                if (OnInput != null) OnInput(inputTextBox.Text);
                inputTextBox.Text = "";
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private DateTime Timer_Start, Timer_Stop;

        public TimeSpan Timer_Elapsed { get { return Timer_Stop - Timer_Start; } }

        public void TimerStart()
        {
            Timer_Start = DateTime.Now;
        }

        public TimeSpan TimerStop()
        {
            Timer_Stop = DateTime.Now;
            return Timer_Elapsed;
        }

        public void Log(string str)
        {
            try
            {
                if (currentEntries == maxEntries)
                {
                    currentEntries = 0;
                    outputTextBox.Clear();
                }
                outputTextBox.AppendText(DateTime.Now.ToLongTimeString() + " - " + str + "\r\n");
                currentEntries++;
            }
            catch { }
        }

        public void Show()
        {
            debugForm.Show();
        }

        public void Hide()
        {
            debugForm.Hide();
        }
    }
}
