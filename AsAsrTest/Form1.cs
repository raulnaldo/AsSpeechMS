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
        AsAsrMicrosoft.AsAsrMicrosoft Asr;
        public Form1()
        {
            InitializeComponent();
            Asr = new AsAsrMicrosoft.AsAsrMicrosoft(
                @"E:\ASDATA\Source\VOICE_MICROSOFT\Cognitive-Speech-STT-Windows\ASR_MICROSOFT\AsAsrTest\bin\Debug",
                @"C:\",
                "easyDoro",
                "3803cc91bec7474e8348ff9331206a02");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string Response="Hola Mundo";
            //Response=Asr.Recognice("Raul.wav", 5);
            Asr.PlayTTS(Response,"es-ES",@"C:\HolaMundo.wav");
        }

        private void buttonLUIS_Click(object sender, EventArgs e)
        {
            string OutPut = string.Empty;
            LuisOutputResponse Response = new LuisOutputResponse();
            Asr.GetLuisAnswer(textBox1.Text, ref Response);
        }
    }
}
