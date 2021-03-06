﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace APLIKASI_STEGO_EDGE_COVER_CITRA_DIGITAL
{
    public class randomPermute
    {
        int key;

        public randomPermute(int key)
        {
            this.key = key;
        }

        public Bitmap Process(Bitmap image, string mode)
        {
            Bitmap Image = new Bitmap(image);
            int height = Image.Height;
            int width = Image.Width;
            int piksel = width * height;
            int index = 0;
            int[] temp = new int[piksel];

            // buat matriks gambar menjadi array 1 dimensi
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    temp[index] = (int)((Image.GetPixel(j, i).R + Image.GetPixel(j, i).G + Image.GetPixel(j, i).B) / 3);
                    index++;
                }
            }

            List<int>[] rail = new List<int>[key];
            for (int i = 0; i < key; i++)
            {
                rail[i] = new List<int>();
            }

            index = 0;
            int number = 0;
            int increment = 1;
            int[] buffer = new int[temp.Length];
            if (mode == "Encrypt")
            {
                for (int i = 0; i < temp.Length; i++)
                {
                    if (number + increment == key)
                    {
                        increment = -1;
                    }
                    else if (number + increment == -1)
                    {
                        increment = 1;
                    }
                    rail[number].Add(temp[i]);
                    number += increment;
                }

                for (int i = 0; i < rail.Length; i++)
                {
                    foreach (int x in rail[i])
                    {
                        buffer[index] = x;
                        index++;
                    }
                }
            }
            else if (mode == "Decrypt")
            {
                for (int i = 0; i < temp.Length; i++)
                {
                    if (number + increment == key)
                    {
                        increment = -1;
                    }
                    else if (number + increment == -1)
                    {
                        increment = 1;
                    }
                    rail[number].Add(i);
                    number += increment;
                }

                for (int i = 0; i < key; i++)
                {
                    for (int j = 0; j < rail[i].Count; j++)
                    {
                        buffer[rail[i][j]] = temp[index];
                        index++;
                    }
                }
            }

            // buat gambar berdasarkan array
            Bitmap randomPermute = new Bitmap(width, height);
            index = 0;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    randomPermute.SetPixel(j, i, Color.FromArgb(buffer[index], buffer[index], buffer[index]));
                    index++;
                }
            }

            return randomPermute;
        }
    }
}