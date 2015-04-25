﻿using System;
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
        float threshold;
        public Form1()
        {
            InitializeComponent();
            ofd = new OpenFileDialog();
            sfd = new SaveFileDialog();
        }

        private float GetThreshold(Bitmap image, int n)
        {
            float tMax = 255F, tMin = 0F, limit = 0.1F * n, tH, tL;
            int nE, diff;
            bool set = false;

            do
            {
                tH = (float)Math.Floor((tMax + tMin) / 2F);
                tL = 0.4F * tH;
                Canny CannyData = new Canny(image, tH, tL);
                nE = CannyData.CountEdges();
                diff = nE - n;

                if (diff > limit)
                {
                    tMin = tH;
                }
                else if (diff < 0)
                {
                    tMax = tH;
                }
                else
                {
                    set = true;
                }
            } while (set == false);

            return tH;
        }

        private Bitmap Embed(Bitmap image, string message, int p)
        {
            int l = message.Length;
            Bitmap e;
            float tH, tL;
            Canny CannyData;
            Bitmap stegoImage = (Bitmap)image.Clone();
            string binaryMessage = StringToBinary(message);

            //bitand
            for (int i = 0; i < image.Height; i++)
            {
                for (int j = 0; j < image.Width; j++)
                {
                    byte x = (byte)((image.GetPixel(j, i).R + image.GetPixel(j, i).G + image.GetPixel(j, i).B) / 3);
                    x = (byte)(x & 252);
                    Color newPixel = Color.FromArgb(x, x, x);
                    image.SetPixel(j, i, newPixel);
                }
            }            
            //end bitand

            //add length of the message for C bits before the message
            string binaryL = Convert.ToString(l, 2);
            float C = (float)Math.Ceiling(binaryL.Length / 8F);
            
            string lengthMessage = Convert.ToString(l, 2).PadLeft((int)C * 8, '0');
            binaryMessage = lengthMessage + binaryMessage;
            l = binaryMessage.Length;   //length of the augmented message

            //l is the length of the binary digit from the message
            //l is divided by 2 because 1 pixel holds 2 bits of the message
            tH = GetThreshold(image, (int)Math.Ceiling(l / 2F));
            threshold = tH;
            tL = 0.4F * tH;

            //obtain e: e is edge map obtained by calling canny edge detection algorithm
            CannyData = new Canny(image, tH, tL);
            e = CannyData.DisplayImage(CannyData.EdgeMap);

            //shuffle e and stegoImage using stego key P
            randomPermute permute = new randomPermute(p);
            randomPermute permute1 = new randomPermute(p);
            e = permute.Encrypt(e);
            stegoImage = permute1.Encrypt(stegoImage);

            //embed message
            int index = 0;
            for (int i = 0; i < e.Height; i++)
            {
                for (int j = 0; j < e.Width; j++)
                {
                    int grey = (int)((e.GetPixel(j, i).R + e.GetPixel(j, i).G + e.GetPixel(j, i).B) / 3);
                    if (grey == 255 && index < l)
                    {
                        byte color = (byte)((stegoImage.GetPixel(j,i).R + stegoImage.GetPixel(j,i).G + stegoImage.GetPixel(j,i).B) / 3); ;
                        color = (byte)(color & 252);

                        if (index == l)
                        {
                            color = (byte)(color + int.Parse(binaryMessage[index] + ""));
                        }
                        else
                        {
                            color = (byte)(color + (2 * int.Parse(binaryMessage[index + 1] + "")) + int.Parse(binaryMessage[index] + ""));
                        }

                        Color newPixel = Color.FromArgb(color, color, color);
                        stegoImage.SetPixel(j, i, newPixel);
                        index += 2;
                    }
                }
            }
            //end embed message

            //reshuffle stegoImage to get stego image
            stegoImage = permute1.Decrypt(stegoImage);

            return stegoImage;
        }
        
        private string Extract(Bitmap image, float t, int p)
        {
            Canny CannyData;
            int edge;
            Bitmap e;
            Bitmap stegoImage = (Bitmap)image.Clone();
            Bitmap mask = (Bitmap)stegoImage.Clone();

            //bitand
            for (int i = 0; i < mask.Height; i++)
            {
                for (int j = 0; j < mask.Width; j++)
                {
                    byte x = (byte)((mask.GetPixel(j, i).R + mask.GetPixel(j, i).G + mask.GetPixel(j, i).B) / 3);
                    x = (byte)(x & 252);
                    Color newPixel = Color.FromArgb(x, x, x);
                    mask.SetPixel(j, i, newPixel);
                }
            }
            //end bitand
            
            float tH = t, tL = 0.4F * tH;
            CannyData = new Canny(mask, tH, tL);
            e = CannyData.DisplayImage(CannyData.EdgeMap);
            edge = CannyData.CountEdges();

            //shuffle stegoImage to get order of embedding
            randomPermute permute = new randomPermute(p);
            randomPermute permute1 = new randomPermute(p);
            e = permute.Encrypt(e);
            stegoImage = permute1.Encrypt(stegoImage);

            //extract message
            string extractedMessage = "";
            for (int i = 0; i < e.Height; i++)
            {
                for (int j = 0; j < e.Width; j++)
                {
                    int grey = (int)((e.GetPixel(j, i).R + e.GetPixel(j, i).G + e.GetPixel(j, i).B) / 3);
                    if (grey == 255)
                    {
                        Color pixel = stegoImage.GetPixel(j, i);
                        int x = (int)((pixel.R + pixel.G + pixel.B) / 3);
                        byte value = (byte)(x & 3);
                        extractedMessage += (value % 2).ToString() + (value / 2).ToString();
                    }
                }
            }
            //end extract message
            string binaryC = Convert.ToString((int)((edge - (edge * 0.1F)) / 4F), 2);
            int c = (int)Math.Ceiling(binaryC.Length/8F);

            //extract first 16 bits to get length of the message
            int l = Convert.ToInt32(extractedMessage.Substring(0, (int)c * 8), 2);
            //extract the main message in binary
            //l * 8 because 1 string represents 8 bits
            string binaryMessage = extractedMessage.Substring((int)c * 8, (l * 8));

            return BinaryToString(binaryMessage);
        }

        private string StringToBinary(string data)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char c in data.ToCharArray())
            {
                sb.Append(Convert.ToString(c, 2).PadLeft(8, '0'));
            }
            return sb.ToString();
        }

        private string BinaryToString(string data)
        {
            List<Byte> byteList = new List<byte>();

            for (int i = 0; i < data.Length; i += 8)
            {
                byteList.Add(Convert.ToByte(data.Substring(i, 8), 2));
            }
            return Encoding.ASCII.GetString(byteList.ToArray());
        }
                            
        private void pcBoxOpen_Click(object sender, EventArgs e)
        {
            ofd.FileName = "";
            ofd.Filter = "File Gambar(*.png,*.bmp,*.pgm)|*.png;*.bmp;*.pgm";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string extension = Path.GetExtension(ofd.FileName);
                extension = extension.ToLower();

                if (extension == ".pgm")
                {
                    PGMImageIO pgmImageIO = new PGMImageIO(ofd.FileName);
                    pgmImageIO.LoadImage();
                    Bitmap _image = (Bitmap)pgmImageIO.Image.Clone();
                    pcboxCover.Image = _image;
                }
                else
                pcboxCover.Image = new Bitmap(ofd.FileName);
            }            
        }

        private void btnEmbed_Click(object sender, EventArgs e)
        {
            string message = txtBoxEmbedMessage.Text;
            Bitmap image = (Bitmap)pcboxCover.Image;
            ALFG prng = new ALFG(11);
            int p = prng.PRNG(5, 7, 2);

            Bitmap stegoImage = Embed(image, message,p);

            txtBoxStegoKey.Text = p.ToString();
            pcboxEmbedStego.Image = stegoImage;
        }

        private void pcBoxSave_Click(object sender, EventArgs e)
        {
            Bitmap stegoImage = (Bitmap)pcboxEmbedStego.Image;
            string p = txtBoxStegoKey.Text;

            sfd.FileName = "Stego Image " + p + ".png";
            sfd.Filter = "File Gambar(*.png)|*.png";
            if (sfd.ShowDialog() == DialogResult.OK)
            {                
                stegoImage.Save(sfd.FileName, ImageFormat.Png);
            }
        }

        private void pcBoxOpenStego_Click(object sender, EventArgs e)
        {
            ofd.FileName = "";
            ofd.Filter = "File Gambar(*.jpg;*.jpeg;*.png,*.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                pcBoxExtractStegoImage.Image = new Bitmap(ofd.FileName);
            } 
        }

        private void btnExtract_Click(object sender, EventArgs e)
        {            
            Bitmap stegoImage = (Bitmap)pcBoxExtractStegoImage.Image;            
            int p = Convert.ToInt32(txtBoxKey.Text);
            float tH = threshold;

            string extractedMessage = Extract(stegoImage, tH, p);
            txtBoxExtractMessage.Text = extractedMessage;
        }
    }
}