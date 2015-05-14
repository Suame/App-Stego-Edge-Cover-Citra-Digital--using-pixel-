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
    class Embed
    {
        private float GetThreshold(Bitmap coverImage, int augmentedLength, float widthOfGaussian)
        {
            Bitmap image = new Bitmap(coverImage);
            int n = augmentedLength;
            float sigma = widthOfGaussian;
            float tMax = 255F, tMin = 0F, limit = 0.1F * n, tH, tL;
            int nE, diff;
            bool set = false;

            do
            {
                tH = (float)Math.Floor((tMax + tMin) / 2F);
                tL = 0.4F * tH;
                Canny CannyData = new Canny(image, tH, tL, sigma);
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

        public Bitmap Embedding(Bitmap coverImage, string secretMessage, int stegoKey, float widthOfGaussian)
        {
            Bitmap image = new Bitmap(coverImage);
            string message = secretMessage;
            int p = stegoKey;
            float WoG = widthOfGaussian;

            Bitmap stegoImage = new Bitmap(image);

            //mask 2LSB an find edges
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

            //add length of the message for C bits before the message
            int l = message.Length;
            string binaryL = Convert.ToString(l, 2);
            int C = (int)Math.Ceiling(binaryL.Length / 8F);

            string messageSize = Convert.ToString(l, 2).PadLeft(C * 8, '0');
            string binaryMessage = Form1.StringToBinary(message);
            string augmentedMessage = messageSize + binaryMessage;
            l = augmentedMessage.Length;   //length of the augmented message in binary

            //l is divided by 2 because each edge pixel carries 2 bits of the augmented message
            float tH = GetThreshold(image, (int)(l / 2), WoG);
            float tL = 0.4F * tH;

            //obtain e: e is edge map obtained by calling canny edge detection algorithm
            Canny CannyData = new Canny(image, tH, tL, WoG);
            Bitmap e = CannyData.DisplayImage(CannyData.EdgeMap);

            //shuffle e and stegoImage using stego key P to embed message in edge pixels of stegoImage
            randomPermute permuteE = new randomPermute(p);
            randomPermute permuteStego = new randomPermute(p);
            e = permuteE.Process(e, "Encrypt");
            stegoImage = permuteStego.Process(stegoImage, "Encrypt");

            //embed message in edge pixels of stegoImage
            int index = 0;
            for (int i = 0; i < e.Height; i++)
            {
                for (int j = 0; j < e.Width; j++)
                {
                    byte grey = (byte)((e.GetPixel(j, i).R + e.GetPixel(j, i).G + e.GetPixel(j, i).B) / 3);
                    if (grey == 255 && index < l)
                    {
                        byte x = (byte)((stegoImage.GetPixel(j, i).R + stegoImage.GetPixel(j, i).G + stegoImage.GetPixel(j, i).B) / 3); ;
                        x = (byte)(x & 252);

                        x = (byte)(x + (2 * Convert.ToByte(augmentedMessage[index + 1] + "")) + Convert.ToByte(augmentedMessage[index] + ""));

                        Color newPixel = Color.FromArgb(x, x, x);
                        stegoImage.SetPixel(j, i, newPixel);
                        index += 2;
                    }
                }
            }
            //end embed message

            //reshuffle stegoImage to get stego image
            stegoImage = permuteStego.Process(stegoImage, "Decrypt");

            //convert threshold and width of Gaussian to IEEE 754 floating point half precision format
            ushort halfTh = Half.FloatToHalf(tH);
            string binaryTh = Convert.ToString(halfTh, 2).PadLeft(16, '0');
            ushort halfWoG = Half.FloatToHalf(WoG);
            string binaryWoG = Convert.ToString(halfWoG, 2).PadLeft(16, '0');
            
            //pixels in eMax are maximum number of edge pixels for a given image
            Canny CannyDataMax = new Canny(image, 0.1F, 0F, 0.1F);
            Bitmap eMax = new Bitmap(CannyDataMax.DisplayImage(CannyDataMax.EdgeMap));

            //shuffle e and stegoImage using stego key P to embed tH and WoG in non-edge pixels of stegoImage
            ALFG alfg = new ALFG(p);
            int key = alfg.PRNG(13, 11, 13);
            randomPermute permuteEMax = new randomPermute(key);
            randomPermute permuteStegoImage = new randomPermute(key);
            eMax = permuteEMax.Process(eMax, "Encrypt");
            stegoImage = permuteStegoImage.Process(stegoImage, "Encrypt");

            //embed threshold and width of Gaussian in non-edge pixels of stegoImage
            index = 0;
            bool ex = false;
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
                            x = (byte)(x & 254);

                            x = (byte)(x + Convert.ToByte(binaryTh[index] + ""));

                            Color newPixel = Color.FromArgb(x, x, x);
                            stegoImage.SetPixel(j, i, newPixel);
                            index++;
                        }
                        else if (grey == 0 && index >= 16 && index < 32)
                        {
                            byte x = (byte)((stegoImage.GetPixel(j, i).R + stegoImage.GetPixel(j, i).G + stegoImage.GetPixel(j, i).B) / 3);
                            x = (byte)(x & 254);

                            x = (byte)(x + Convert.ToByte(binaryWoG[index - 16] + ""));

                            Color newPixel = Color.FromArgb(x, x, x);
                            stegoImage.SetPixel(j, i, newPixel);
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

            //reshuffle stegoImage to get stego image
            stegoImage = permuteStegoImage.Process(stegoImage, "Decrypt");

            return stegoImage;
        }
    }
}