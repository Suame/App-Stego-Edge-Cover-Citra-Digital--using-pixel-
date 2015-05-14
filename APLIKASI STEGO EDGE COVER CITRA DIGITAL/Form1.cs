using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.IO;

namespace APLIKASI_STEGO_EDGE_COVER_CITRA_DIGITAL
{
    public partial class Form1 : Form
    {
        public OpenFileDialog ofd;
        public SaveFileDialog sfd;
        public Form1()
        {
            InitializeComponent();
            ofd = new OpenFileDialog();
            sfd = new SaveFileDialog();
            pcBoxSave.Enabled = false;
            btnEmbed.Enabled = false;
            btnExtract.Enabled = false;
        }
                
        public static string StringToBinary(string data)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char c in data.ToCharArray())
            {
                sb.Append(Convert.ToString(c, 2).PadLeft(8, '0'));
            }
            return sb.ToString();
        }

        public static string BinaryToString(string data)
        {
            List<Byte> byteList = new List<byte>();

            for (int i = 0; i < data.Length; i += 8)
            {
                byteList.Add(Convert.ToByte(data.Substring(i, 8), 2));
            }
            return Encoding.ASCII.GetString(byteList.ToArray());
        }

        private void txtBoxEmbedMessage_TextChanged(object sender, EventArgs e)
        {
            if (txtBoxEmbedMessage.Text != "" && pcboxCover.Image != null)
                btnEmbed.Enabled = true;

            if (txtBoxEmbedMessage.Text == "")
                btnEmbed.Enabled = false;
        }           

        private void pcBoxOpen_Click(object sender, EventArgs e)
        {
            try
            {
                ofd.FileName = "";
                ofd.Filter = "File Gambar(*.pgm;*.png;*.bmp;*.jpg;*.jpeg)|*.pgm;*.png;*.bmp;*.jpg;*.jpeg";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string extension = Path.GetExtension(ofd.FileName);
                    extension = extension.ToLower();

                    if (extension == ".pgm")
                    {
                        PGMImageIO pgmImageIO = new PGMImageIO(ofd.FileName);
                        pgmImageIO.LoadImage();
                        Bitmap _image = new Bitmap(pgmImageIO.Image);
                        pcboxCover.Image = _image;
                    }
                    else
                        pcboxCover.Image = new Bitmap(ofd.FileName);

                    if (txtBoxEmbedMessage.Text != "" && pcboxCover.Image != null)
                        btnEmbed.Enabled = true;
                }
            }
            catch (Exception er)
            {
                MessageBox.Show("Failed to load image, " + er.Message);
            }
        }

        private void btnEmbed_Click(object sender, EventArgs e)
        {
            try
            {
                string message = txtBoxEmbedMessage.Text;
                float widthOfGaussian = (float)numericUpDownGaussian.Value;
                Bitmap image = new Bitmap(pcboxCover.Image);
                ALFG prng = new ALFG(11);
                int p = prng.PRNG(7, 3, 23);
                Embed embed = new Embed();
                Bitmap stegoImage = embed.Embedding(image, message, p, widthOfGaussian);

                txtBoxStegoKey.Text = p.ToString();
                pcboxEmbedStego.Image = stegoImage;

                pcBoxSave.Enabled = true;
            }
            catch (Exception er)
            {
                MessageBox.Show("Failed to embed secret message in cover image : \n" + er.Message);
            }
        }

        private void pcBoxSave_Click(object sender, EventArgs e)
        {
            try
            {
                Bitmap stegoImage = new Bitmap(pcboxEmbedStego.Image);
                string p = txtBoxStegoKey.Text;

                sfd.FileName = "Stego Image " + p;
                sfd.Filter = "PGM(*.pgm)|*.pgm|PNG(*.png)|*.png|BMP(*.bmp)|*.bmp|JPEG(*.jpg,*.jpeg)|*.jpeg";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    string extension = Path.GetExtension(sfd.FileName);
                    extension = extension.ToLower();

                    if (extension == ".pgm")
                    {
                        PGMImageIO pgmImageIO = new PGMImageIO(sfd.FileName, stegoImage);
                        pgmImageIO.SaveImage();
                    }
                    else if (extension == ".png")
                        stegoImage.Save(sfd.FileName, ImageFormat.Png);
                    else if (extension == ".bmp")
                        stegoImage.Save(sfd.FileName, ImageFormat.Bmp);
                    else if (extension == ".jpeg")
                        stegoImage.Save(sfd.FileName, ImageFormat.Jpeg);
                    else
                        MessageBox.Show("Unsupported image format");
                }
            }
            catch (Exception er)
            {
                MessageBox.Show("Failed to save image : \n" + er.Message);
            }
        }

        private void btnResetEmbed_Click(object sender, EventArgs e)
        {
            txtBoxEmbedMessage.Text = "";
            numericUpDownGaussian.Value = (decimal)0.1;
            txtBoxStegoKey.Text = "";
            pcboxCover.Image = null;
            pcboxEmbedStego.Image = null;
            pcBoxSave.Enabled = false;
            btnEmbed.Enabled = false;
        }

        private void txtBoxKey_TextChanged(object sender, EventArgs e)
        {
            if (txtBoxKey.Text != "" && pcBoxExtractStegoImage.Image != null)
                btnExtract.Enabled = true;

            if (txtBoxKey.Text == "")
                btnExtract.Enabled = false;
        }

        private void txtBoxKey_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void pcBoxOpenStego_Click(object sender, EventArgs e)
        {
            try
            {
                ofd.FileName = "";
                ofd.Filter = "File Gambar(*.pgm;*.png;*.bmp;*.jpg;*.jpeg)|*.pgm;*.png;*.bmp;*.jpg;*.jpeg";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string extension = Path.GetExtension(ofd.FileName);
                    extension = extension.ToLower();

                    if (extension == ".pgm")
                    {
                        PGMImageIO pgmImageIO = new PGMImageIO(ofd.FileName);
                        pgmImageIO.LoadImage();
                        Bitmap _image = new Bitmap(pgmImageIO.Image);
                        pcBoxExtractStegoImage.Image = _image;
                    }
                    else
                        pcBoxExtractStegoImage.Image = new Bitmap(ofd.FileName);

                    if (txtBoxKey.Text != "" && pcBoxExtractStegoImage.Image != null)
                        btnExtract.Enabled = true;
                }
            }
            catch (Exception er)
            {
                MessageBox.Show("Failed to load image : \n" + er.Message);
            }
        }

        private void btnExtract_Click(object sender, EventArgs e)
        {
            try
            {
                Bitmap stegoImage = new Bitmap(pcBoxExtractStegoImage.Image);
                int p = Convert.ToInt32(txtBoxKey.Text);
                Extract extract = new Extract();
                string extractedMessage = extract.Extraction(stegoImage, p);
                txtBoxExtractMessage.Text = extractedMessage;
            }
            catch (Exception er)
            {
                MessageBox.Show("Failed to extract secret message from stego image : \n" + er.Message);
            }
        }

        private void btnResetExtract_Click(object sender, EventArgs e)
        {
            txtBoxKey.Text = "";
            txtBoxExtractMessage.Text = "";
            pcBoxExtractStegoImage.Image = null;
            btnExtract.Enabled = false;
        }
    }
}