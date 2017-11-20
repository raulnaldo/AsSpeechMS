using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AsAsrMicrosoft;

namespace AsAsrTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            AsAsrMicrosoft.AsAsrMicrosoft Asr = new AsAsrMicrosoft.AsAsrMicrosoft(
                @"E:\ASDATA\Source\VOICE_MICROSOFT\Cognitive-Speech-STT-Windows\ASR_MICROSOFT\AsAsrTest\bin\Debug",
                @"C:\",
                "easyDoro");
            MessageBox.Show(Asr.Recognice("Raul.wav", 1));
            
        }
    }
}
