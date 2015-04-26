﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace APLIKASI_STEGO_EDGE_COVER_CITRA_DIGITAL
{
    class Canny
    {
        public int Width, Height;
        public Bitmap Obj;
        public int[,] GreyImage;

        //Gaussian Kernel Data
        int[,] GaussianKernel;
        int KernelWeight, KernelSize = 5;
        float Sigma = 1;   // for N=2 Sigma =0.85  N=5 Sigma =1, N=9 Sigma = 2    2*Sigma = (int)N/2

        //Canny Edge Detection Parameters
        float MaxHysteresisThresh, MinHysteresisThresh;
        public float[,] DerivativeX, DerivativeY, Gradient, NonMax, GNH, GNL;
        public int[,] FilteredImage, PostHysteresis, EdgeMap, VisitedMap;
        int[,] EdgePoints;
        
        public Canny(Bitmap Input, float Th, float Tl)
        {
            // Gaussian and Canny Parameters
            MaxHysteresisThresh = Th;
            MinHysteresisThresh = Tl;
            Obj = Input;
            Width = Obj.Width;
            Height = Obj.Height;
            EdgeMap = new int[Width, Height];
            VisitedMap = new int[Width, Height];

            ReadImage();
            DetectCannyEdges();
            return;
        }

        public Canny(Bitmap Input, float Th, float Tl, int GaussianMaskSize, float SigmaforGaussianKernel)
        {
            // Gaussian and Canny Parameters
            MaxHysteresisThresh = Th;
            MinHysteresisThresh = Tl;
            KernelSize = GaussianMaskSize;
            Sigma = SigmaforGaussianKernel;
            Obj = Input;
            Width = Obj.Width;
            Height = Obj.Height;
            EdgeMap = new int[Width, Height];
            VisitedMap = new int[Width, Height];

            ReadImage();
            DetectCannyEdges();
            return;
        }
        
        public Bitmap DisplayImage(int[,] GreyImage)
        {
            int i, j;
            int W, H;
            W = GreyImage.GetLength(0);
            H = GreyImage.GetLength(1);
            Bitmap image = new Bitmap(W, H);
            BitmapData bitmapData1 = image.LockBits(new Rectangle(0, 0, W, H),
                                     ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                byte* imagePointer1 = (byte*)bitmapData1.Scan0;

                for (i = 0; i < bitmapData1.Height; i++)
                {
                    for (j = 0; j < bitmapData1.Width; j++)
                    {
                        imagePointer1[0] = (byte)GreyImage[j, i];
                        imagePointer1[1] = (byte)GreyImage[j, i];
                        imagePointer1[2] = (byte)GreyImage[j, i];
                        imagePointer1[3] = (byte)255;
                        //4 bytes per pixel
                        imagePointer1 += 4;
                    }
                    //4 bytes per pixel
                    imagePointer1 += (bitmapData1.Stride - (bitmapData1.Width * 4));
                }
            }
            image.UnlockBits(bitmapData1);
            return image;
        }      // Display Grey Image

        private void ReadImage()
        {
            int i, j;
            GreyImage = new int[Obj.Width, Obj.Height];  //[Row,Column]
            Bitmap image = Obj;
            BitmapData bitmapData1 = image.LockBits(new Rectangle(0, 0, image.Width, image.Height),
                                     ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                byte* imagePointer1 = (byte*)bitmapData1.Scan0;

                for (i = 0; i < bitmapData1.Height; i++)
                {
                    for (j = 0; j < bitmapData1.Width; j++)
                    {
                        GreyImage[j, i] = (int)((imagePointer1[0] + imagePointer1[1] + imagePointer1[2]) / 3.0);
                        //4 bytes per pixel
                        imagePointer1 += 4;
                    }
                    //4 bytes per pixel
                    imagePointer1 += bitmapData1.Stride - (bitmapData1.Width * 4);
                }
            }
            image.UnlockBits(bitmapData1);
            return;
        }

        private void GenerateGaussianKernel(int N, float S, out int Weight)
        {
            int SizeofKernel = N;
            float Sigma = S;
            float pi = (float)Math.PI;

            float[,] Kernel = new float[N, N];
            GaussianKernel = new int[N, N];
            float[,] OP = new float[N, N];

            float D1 = 1 / (2 * pi * Sigma * Sigma);
            float D2 = 2 * Sigma * Sigma;

            float min = 1000;
            for (int i = -SizeofKernel / 2; i <= SizeofKernel / 2; i++)
            {
                for (int j = -SizeofKernel / 2; j <= SizeofKernel / 2; j++)
                {
                    Kernel[SizeofKernel / 2 + i, SizeofKernel / 2 + j] = ((1 / D1) * (float)Math.Exp(-(i * i + j * j) / D2));
                    if (Kernel[SizeofKernel / 2 + i, SizeofKernel / 2 + j] < min)
                        min = Kernel[SizeofKernel / 2 + i, SizeofKernel / 2 + j];
                }
            }

            int mult = (int)(1 / min);
            int sum = 0;
            if ((min > 0) && (min < 1))
            {
                for (int i = -SizeofKernel / 2; i <= SizeofKernel / 2; i++)
                {
                    for (int j = -SizeofKernel / 2; j <= SizeofKernel / 2; j++)
                    {
                        Kernel[SizeofKernel / 2 + i, SizeofKernel / 2 + j] = (float)Math.Round(Kernel[SizeofKernel / 2 + i, SizeofKernel / 2 + j] * mult, 0);
                        GaussianKernel[SizeofKernel / 2 + i, SizeofKernel / 2 + j] = (int)Kernel[SizeofKernel / 2 + i, SizeofKernel / 2 + j];
                        sum = sum + GaussianKernel[SizeofKernel / 2 + i, SizeofKernel / 2 + j];
                    }
                }
            }
            else
            {
                sum = 0;
                for (int i = -SizeofKernel / 2; i <= SizeofKernel / 2; i++)
                {
                    for (int j = -SizeofKernel / 2; j <= SizeofKernel / 2; j++)
                    {
                        Kernel[SizeofKernel / 2 + i, SizeofKernel / 2 + j] = (float)Math.Round(Kernel[SizeofKernel / 2 + i, SizeofKernel / 2 + j], 0);
                        GaussianKernel[SizeofKernel / 2 + i, SizeofKernel / 2 + j] = (int)Kernel[SizeofKernel / 2 + i, SizeofKernel / 2 + j];
                        sum = sum + GaussianKernel[SizeofKernel / 2 + i, SizeofKernel / 2 + j];
                    }
                }
            }
            //Normalizing kernel Weight
            Weight = sum;

            return;
        }

        private int[,] GaussianFilter(int[,] Data)
        {
            GenerateGaussianKernel(KernelSize, Sigma, out KernelWeight);

            int[,] Output = new int[Width, Height];
            int Limit = KernelSize / 2;

            float Sum = 0;
            Output = Data; // Removes Unwanted Data Omission due to kernel bias while convolution
            for (int i = Limit; i <= ((Width - 1) - Limit); i++)
            {
                for (int j = Limit; j <= ((Height - 1) - Limit); j++)
                {
                    Sum = 0;
                    for (int k = -Limit; k <= Limit; k++)
                    {
                        for (int l = -Limit; l <= Limit; l++)
                        {
                            Sum = Sum + ((float)Data[i + k, j + l] * GaussianKernel[Limit + k, Limit + l]);
                        }
                    }
                    Output[i, j] = (int)(Math.Round(Sum / (float)KernelWeight));
                }
            }
            return Output;
        }

        private float[,] Differentiate(int[,] Data, int[,] Filter)
        {
            int Fw = Filter.GetLength(0);
            int Fh = Filter.GetLength(1);
            float sum = 0;
            float[,] Output = new float[Width, Height];

            for (int i = (Fw / 2); i <= ((Width - (Fw / 2)) - 1); i++)
            {
                for (int j = (Fh / 2); j <= ((Height - (Fh / 2)) - 1); j++)
                {
                    sum = 0;
                    for (int k = (-Fw / 2); k <= (Fw / 2); k++)
                    {
                        for (int l = (-Fh / 2); l <= (Fh / 2); l++)
                        {
                            sum = sum + Data[i + k, j + l] * Filter[(Fw / 2) + k, (Fh / 2) + l];
                        }
                    }
                    Output[i, j] = sum;
                }
            }
            return Output;
        }

        private void DetectCannyEdges()
        {
            Gradient = new float[Width, Height];
            NonMax = new float[Width, Height];
            PostHysteresis = new int[Width, Height];
            DerivativeX = new float[Width, Height];
            DerivativeY = new float[Width, Height];

            //Gaussian Filter Input Image 
            FilteredImage = GaussianFilter(GreyImage);

            //Sobel Masks
            int[,] Dx = {{1,0,-1},
                         {1,0,-1},
                         {1,0,-1}};

            int[,] Dy = {{1,1,1},
                         {0,0,0},
                         {-1,-1,-1}};
            
            DerivativeX = Differentiate(FilteredImage, Dx);
            DerivativeY = Differentiate(FilteredImage, Dy);

            //Compute the gradient magnitude based on derivatives in x and y:
            for (int i = 0; i <= (Width - 1); i++)
            {
                for (int j = 0; j <= (Height - 1); j++)
                {
                    Gradient[i, j] = (float)Math.Sqrt((DerivativeX[i, j] * DerivativeX[i, j]) + (DerivativeY[i, j] * DerivativeY[i, j]));
                }
            }

            // Perform Non maximum suppression:
            // NonMax = Gradient;
            for (int i = 0; i <= (Width - 1); i++)
            {
                for (int j = 0; j <= (Height - 1); j++)
                {
                    NonMax[i, j] = Gradient[i, j];
                }
            }

            int Limit = KernelSize / 2;
            float Tangent;
            for (int i = Limit; i <= (Width - Limit) - 1; i++)
            {
                for (int j = Limit; j <= (Height - Limit) - 1; j++)
                {
                    if (DerivativeX[i, j] == 0)
                        Tangent = 90F;
                    else
                        Tangent = (float)(Math.Atan(DerivativeY[i, j] / DerivativeX[i, j]) * 180 / Math.PI); //rad to degree

                    //Horizontal Edge
                    if (((-22.5 < Tangent) && (Tangent <= 22.5)) || ((157.5 < Tangent) && (Tangent <= -157.5)))
                    {
                        if ((Gradient[i, j] < Gradient[i, j + 1]) || (Gradient[i, j] < Gradient[i, j - 1]))
                            NonMax[i, j] = 0;
                    }

                    //Vertical Edge
                    if (((-112.5 < Tangent) && (Tangent <= -67.5)) || ((67.5 < Tangent) && (Tangent <= 112.5)))
                    {
                        if ((Gradient[i, j] < Gradient[i + 1, j]) || (Gradient[i, j] < Gradient[i - 1, j]))
                            NonMax[i, j] = 0;
                    }

                    //+45 Degree Edge
                    if (((-67.5 < Tangent) && (Tangent <= -22.5)) || ((112.5 < Tangent) && (Tangent <= 157.5)))
                    {
                        if ((Gradient[i, j] < Gradient[i + 1, j - 1]) || (Gradient[i, j] < Gradient[i - 1, j + 1]))
                            NonMax[i, j] = 0;
                    }

                    //-45 Degree Edge
                    if (((-157.5 < Tangent) && (Tangent <= -112.5)) || ((67.5 < Tangent) && (Tangent <= 22.5)))
                    {
                        if ((Gradient[i, j] < Gradient[i + 1, j + 1]) || (Gradient[i, j] < Gradient[i - 1, j - 1]))
                            NonMax[i, j] = 0;
                    }
                }
            }

            //PostHysteresis = NonMax;
            for (int r = Limit; r <= (Width - Limit) - 1; r++)
            {
                for (int c = Limit; c <= (Height - Limit) - 1; c++)
                {
                    PostHysteresis[r, c] = (int)NonMax[r, c];
                }
            }

            //Find Max and Min in Post Hysterisis
            float min = 100;
            float max = 0;
            for (int r = Limit; r <= (Width - Limit) - 1; r++)
            {
                for (int c = Limit; c <= (Height - Limit) - 1; c++)
                {
                    if (PostHysteresis[r, c] > max)
                    {
                        max = PostHysteresis[r, c];
                    }

                    if ((PostHysteresis[r, c] < min) && (PostHysteresis[r, c] > 0))
                    {
                        min = PostHysteresis[r, c];
                    }
                }
            }

            GNH = new float[Width, Height];
            GNL = new float[Width, Height]; ;
            EdgePoints = new int[Width, Height];

            for (int r = Limit; r <= (Width - Limit) - 1; r++)
            {
                for (int c = Limit; c <= (Height - Limit) - 1; c++)
                {
                    if (PostHysteresis[r, c] >= MaxHysteresisThresh)
                    {
                        EdgePoints[r, c] = 1;
                        GNH[r, c] = 255;
                    }

                    if ((PostHysteresis[r, c] < MaxHysteresisThresh) && (PostHysteresis[r, c] >= MinHysteresisThresh))
                    {
                        EdgePoints[r, c] = 2;
                        GNL[r, c] = 255;
                    }
                }
            }

            HysterisisThresholding(EdgePoints);

            for (int i = 0; i <= (Width - 1); i++)
            {
                for (int j = 0; j <= (Height - 1); j++)
                {
                    EdgeMap[i, j] = EdgeMap[i, j] * 255;
                }
            }

            return;
        }

        private void HysterisisThresholding(int[,] Edges)
        {
            int Limit = KernelSize / 2;
            for (int i = Limit; i <= (Width - 1) - Limit; i++)
            {
                for (int j = Limit; j <= (Height - 1) - Limit; j++)
                {
                    if (Edges[i, j] == 1)
                    {
                        EdgeMap[i, j] = 1;
                    }
                }
            }

            for (int i = Limit; i <= (Width - 1) - Limit; i++)
            {
                for (int j = Limit; j <= (Height - 1) - Limit; j++)
                {
                    if (Edges[i, j] == 1)
                    {
                        EdgeMap[i, j] = 1;
                        Travers(i, j);
                        VisitedMap[i, j] = 1;
                    }
                }
            }

            return;
        }

        private void Travers(int X, int Y)
        {
            if (VisitedMap[X, Y] == 1)
                return;

            //1
            if (EdgePoints[X + 1, Y] == 2)
            {
                EdgeMap[X + 1, Y] = 1;
                VisitedMap[X + 1, Y] = 1;
                Travers(X + 1, Y);
                return;
            }

            //2
            if (EdgePoints[X + 1, Y - 1] == 2)
            {
                EdgeMap[X + 1, Y - 1] = 1;
                VisitedMap[X + 1, Y - 1] = 1;
                Travers(X + 1, Y - 1);
                return;
            }

            //3
            if (EdgePoints[X, Y - 1] == 2)
            {
                EdgeMap[X, Y - 1] = 1;
                VisitedMap[X, Y - 1] = 1;
                Travers(X, Y - 1);
                return;
            }

            //4
            if (EdgePoints[X - 1, Y - 1] == 2)
            {
                EdgeMap[X - 1, Y - 1] = 1;
                VisitedMap[X - 1, Y - 1] = 1;
                Travers(X - 1, Y - 1);
                return;
            }

            //5
            if (EdgePoints[X - 1, Y] == 2)
            {
                EdgeMap[X - 1, Y] = 1;
                VisitedMap[X - 1, Y] = 1;
                Travers(X - 1, Y);
                return;
            }

            //6
            if (EdgePoints[X - 1, Y + 1] == 2)
            {
                EdgeMap[X - 1, Y + 1] = 1;
                VisitedMap[X - 1, Y + 1] = 1;
                Travers(X - 1, Y + 1);
                return;
            }

            //7
            if (EdgePoints[X, Y + 1] == 2)
            {
                EdgeMap[X, Y + 1] = 1;
                VisitedMap[X, Y + 1] = 1;
                Travers(X, Y + 1);
                return;
            }

            //8
            if (EdgePoints[X + 1, Y + 1] == 2)
            {
                EdgeMap[X + 1, Y + 1] = 1;
                VisitedMap[X + 1, Y + 1] = 1;
                Travers(X + 1, Y + 1);
                return;
            }
            //VisitedMap[X, Y] = 1;
            return;
        }

        public int CountEdges()
        {
            int count = 0;
            for (int i = 0; i < EdgeMap.GetLength(0); i++)
            {
                for (int j = 0; j < EdgeMap.GetLength(1); j++)
                {
                    if (EdgeMap[i, j] == 255)
                    {
                        count++;
                    }
                }
            }

            return count;
        }//Canny Class Ends
    }
}