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
    class Extract
    {
        public string Extraction(Bitmap StegoImage, int stegoKey)
        {
            Bitmap image = new Bitmap(StegoImage);
            int p = stegoKey;

            Bitmap stegoImage = new Bitmap(image);
            Bitmap mask = new Bitmap(stegoImage);

            //mask 2LSB and find edges
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

            //pixels in eMax are maximum number of edge pixels for a given image
            Canny CannyDataMax = new Canny(mask, 0.1F, 0F, 0.1F);
            Bitmap eMax = new Bitmap(CannyDataMax.DisplayImage(CannyDataMax.EdgeMap));

            //shuffle e and stegoImage using stego key P to embed tH and WoG in non-edge pixels of stegoImage
            ALFG alfg = new ALFG(p);
            int key = alfg.PRNG(13, 11, 13);
            randomPermute permuteEMax = new randomPermute(key);
            randomPermute permuteStegoImage = new randomPermute(key);
            eMax = permuteEMax.Process(eMax, "Encrypt");
            stegoImage = permuteStegoImage.Process(stegoImage, "Encrypt");

            //extract threshold and width of Gaussian from the non-edge pixels of stegoImage
            int index = 0;
            bool ex = false;
            string binaryTh = "";
            string binaryWoG = "";
            for (int i = 0; i < eMax.Height; i++)
            {
                for (int j = 0; j < eMax.Width; j++)
                {
                    if (index < 32)
                    {
                        byte grey = (byte)((eMax.GetPixel(j, i).R + eMax.GetPixel(j, i).G + eMax.GetPixel(j, i).B) / 3);
                        if (grey == 0 && index < 16)
                        {
                            byte x = (byte)((stegoImage.GetPixel(j, i).R + stegoImage.GetPixel(j, i).G + stegoImage.GetPixel(j, i).B) / 3);
                            byte value = (byte)(x & 1);
                            binaryTh += (value % 2).ToString();
                            index++;
                        }
                        else if (grey == 0 && index >= 16 && index < 32)
                        {
                            byte x = (byte)((stegoImage.GetPixel(j, i).R + stegoImage.GetPixel(j, i).G + stegoImage.GetPixel(j, i).B) / 3);
                            byte value = (byte)(x & 1);
                            binaryWoG += (value % 2).ToString();
                            index++;
                        }
                    }
                    else if (index >= 32)
                    {
                        ex = true;
                        break;
                    }
                }
                if (ex == true)
                    break;
            }
            //end extract threshold and width of Gaussian

            //reshuffle back stegoImage
            stegoImage = permuteStegoImage.Process(stegoImage, "Decrypt");
            
            //convert threshold and width of Gaussian from the IEEE 754 floating point half precision format to float
            ushort halfTh = Convert.ToUInt16(binaryTh, 2);
            float tH = (float)Math.Round(Half.HalfToFloat(halfTh),0);
            float tL = 0.4F * tH;
            ushort halfWoG = Convert.ToUInt16(binaryWoG, 2);
            float WoG = (float)Math.Round(Half.HalfToFloat(halfWoG),1);

            //obtain e: e is edge map obtained by calling canny edge detection algorithm
            Canny CannyData = new Canny(mask, tH, tL, WoG);
            Bitmap e = CannyData.DisplayImage(CannyData.EdgeMap);

            //shuffle stegoImage to get order of embedding
            randomPermute permuteE = new randomPermute(p);
            randomPermute permuteStego = new randomPermute(p);
            e = permuteE.Process(e, "Encrypt");
            stegoImage = permuteStego.Process(stegoImage, "Encrypt");

            //extract message from the edge pixels of stegoImage
            string extractedMessage = "";
            for (int i = 0; i < e.Height; i++)
            {
                for (int j = 0; j < e.Width; j++)
                {
                    byte grey = (byte)((e.GetPixel(j, i).R + e.GetPixel(j, i).G + e.GetPixel(j, i).B) / 3);
                    if (grey == 255)
                    {
                        byte x = (byte)((stegoImage.GetPixel(j, i).R + stegoImage.GetPixel(j, i).G + stegoImage.GetPixel(j, i).B) / 3);
                        byte value = (byte)(x & 3);
                        extractedMessage += (value % 2).ToString() + (value / 2).ToString();
                    }
                }
            }
            //end extract message

            //compute the value of c by the number of edges in mask
            //edge - (edge * 0.1) because the number of the detected edge are in the range of the limit 0.1 and divided by 4 because about 1/4 of the edges are hold the length of the message in binary
            int edge = CannyData.CountEdges();
            string binaryC = Convert.ToString((int)((edge - (edge * 0.1F)) / 4F), 2); 
            int C = (int)Math.Ceiling(binaryC.Length / 8F);

            //extract first c bits to get length of the message
            int l = Convert.ToInt32(extractedMessage.Substring(0, (C * 8)), 2);
            //extract the main message in binary
            //l * 8 because 1 string represents 8 bits
            string binaryMessage = extractedMessage.Substring((C * 8), (l * 8));

            return Form1.BinaryToString(binaryMessage);
        }
    }
}