using System;
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
        public Bitmap image;
        float MaxHysteresisThreshold, MinHysteresisThreshold, Sigma;
        public int[,] EdgeMap, VisitedMap, EdgePoints;

        public Canny(Bitmap Input, float Th, float Tl, float SigmaforGaussianKernel)
        {
            image = Input;
            MaxHysteresisThreshold = Th;
            MinHysteresisThreshold = Tl;
            Sigma = SigmaforGaussianKernel;

            DetectCannyEdges();
            return;
        }

        private int[,] ReadImage(Bitmap Obj)
        {
            Bitmap Image = Obj;
            int[,] GreyImage = new int[Image.Width, Image.Height]; 

            BitmapData bitmapData1 = Image.LockBits(new Rectangle(0, 0, Image.Width, Image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                byte* imagePointer1 = (byte*)bitmapData1.Scan0;

                for (int i = 0; i < bitmapData1.Height; i++)
                {
                    for (int j = 0; j < bitmapData1.Width; j++)
                    {
                        GreyImage[j, i] = (int)((imagePointer1[0] + imagePointer1[1] + imagePointer1[2]) / 3.0);

                        imagePointer1 += 4;
                    }
                    imagePointer1 += bitmapData1.Stride - (bitmapData1.Width * 4);
                }
            }
            Image.UnlockBits(bitmapData1);

            return GreyImage;
        }

        public Bitmap DisplayImage(int[,] GreyImage)
        {
            int W = GreyImage.GetLength(0);
            int H = GreyImage.GetLength(1);
            Bitmap Image = new Bitmap(W, H);

            BitmapData bitmapData1 = Image.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                byte* imagePointer1 = (byte*)bitmapData1.Scan0;

                for (int i = 0; i < bitmapData1.Height; i++)
                {
                    for (int j = 0; j < bitmapData1.Width; j++)
                    {
                        imagePointer1[0] = (byte)GreyImage[j, i];
                        imagePointer1[1] = (byte)GreyImage[j, i];
                        imagePointer1[2] = (byte)GreyImage[j, i];
                        imagePointer1[3] = (byte)255;

                        imagePointer1 += 4;
                    }
                    imagePointer1 += (bitmapData1.Stride - (bitmapData1.Width * 4));
                }
            }
            Image.UnlockBits(bitmapData1);

            return Image;
        }

        private void DetectCannyEdges()
        {
            int Width = image.Width;
            int Height = image.Height;
            int[,] GreyImage = ReadImage(image);

            float[,] DerivativeX = new float[Width, Height];
            float[,] DerivativeY = new float[Width, Height];
            float[,] Gradient = new float[Width, Height];
            float[,] NonMax = new float[Width, Height];
            int[,] PostHysteresis = new int[Width, Height];
            EdgeMap = new int[Width, Height];
            VisitedMap = new int[Width, Height];
            EdgePoints = new int[Width, Height];

            int KernelLength = 5;//(int)Math.Ceiling(Sigma * 3) * 2 + 1;
            int KernelSize = KernelLength / 2;

            //Gaussian Filter
            int[,] FilteredImage = GaussianFilter(GreyImage, KernelLength);

            //Sobel Masks
            int[,] Dx = {{-1,0,1},
                         {-2,0,2},
                         {-1,0,1}};

            int[,] Dy = {{1,2,1},
                         {0,0,0},
                         {-1,-2,-1}};

            DerivativeX = Differentiate(FilteredImage, Dx);
            DerivativeY = Differentiate(FilteredImage, Dy);

            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    //Compute the gradient magnitude based on derivatives in x and y
                    Gradient[i, j] = (float)Math.Sqrt((DerivativeX[i, j] * DerivativeX[i, j]) + (DerivativeY[i, j] * DerivativeY[i, j]));
                    // Perform Non maximum suppression
                    NonMax[i, j] = Gradient[i, j];
                }
            }

            float Tangent;
            for (int i = KernelSize; i < (Width - KernelSize); i++)
            {
                for (int j = KernelSize; j < (Height - KernelSize); j++)
                {
                    if (DerivativeX[i, j] == 0)
                        Tangent = 90F;
                    else
                        Tangent = (float)(Math.Atan(DerivativeY[i, j] / DerivativeX[i, j]) * 180 / Math.PI); //rad to degree

                    //0 degrees edge (in the horizontal direction)
                    if (((-22.5 < Tangent) && (Tangent <= 22.5)) || ((157.5 < Tangent) && (Tangent <= -157.5)))
                    {
                        if ((Gradient[i, j] < Gradient[i, j + 1]) || (Gradient[i, j] < Gradient[i, j - 1]))
                            NonMax[i, j] = 0;
                    }

                    //45 degrees edge (along the positive diagonal)
                    if (((-157.5 < Tangent) && (Tangent <= -112.5)) || ((22.5 < Tangent) && (Tangent <= 67.5)))
                    {
                        if ((Gradient[i, j] < Gradient[i + 1, j - 1]) || (Gradient[i, j] < Gradient[i - 1, j + 1]))
                            NonMax[i, j] = 0;
                    }

                    //90 degrees edge (in the vertical direction)
                    if (((-112.5 < Tangent) && (Tangent <= -67.5)) || ((67.5 < Tangent) && (Tangent <= 112.5)))
                    {
                        if ((Gradient[i, j] < Gradient[i + 1, j]) || (Gradient[i, j] < Gradient[i - 1, j]))
                            NonMax[i, j] = 0;
                    }

                    //135 degrees edge (along the negative diagonal)
                    if (((-67.5 < Tangent) && (Tangent <= -22.5)) || ((112.5 < Tangent) && (Tangent <= 157.5)))
                    {
                        if ((Gradient[i, j] < Gradient[i + 1, j + 1]) || (Gradient[i, j] < Gradient[i - 1, j - 1]))
                            NonMax[i, j] = 0;
                    }
                }
            }

            for (int r = KernelSize; r < (Width - KernelSize); r++)
            {
                for (int c = KernelSize; c < (Height - KernelSize); c++)
                {
                    //PostHysteresis = NonMax;
                    PostHysteresis[r, c] = (int)NonMax[r, c];

                    if (PostHysteresis[r, c] >= MaxHysteresisThreshold)
                    {
                        EdgePoints[r, c] = 1;
                    }

                    if ((PostHysteresis[r, c] < MaxHysteresisThreshold) && (PostHysteresis[r, c] >= MinHysteresisThreshold))
                    {
                        EdgePoints[r, c] = 2;
                    }
                }
            }

            HysterisisThresholding(EdgePoints, KernelLength);

            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    EdgeMap[i, j] = EdgeMap[i, j] * 255;
                }
            }

            return;
        }

        private int[,] GaussianFilter(int[,] Data, int KernelLength)
        {
            int Width = Data.GetLength(0);
            int Height = Data.GetLength(1);
            int[,] Output = new int[Width, Height];

            int KernelWeight;
            int KernelSize = KernelLength / 2;
            int[,] GaussianKernel = GenerateGaussianKernel(KernelLength, Sigma, out KernelWeight);

            float Sum = 0;
            // Removes Unwanted Data Omission due to kernel bias while convolution
            for (int i = KernelSize; i < (Width - KernelSize); i++)
            {
                for (int j = KernelSize; j < (Height - KernelSize); j++)
                {
                    Sum = 0;
                    for (int k = -KernelSize; k <= KernelSize; k++)
                    {
                        for (int l = -KernelSize; l <= KernelSize; l++)
                        {
                            Sum = Sum + ((float)Data[i + k, j + l] * GaussianKernel[KernelSize + k, KernelSize + l]);
                        }
                    }
                    Output[i, j] = (int)(Math.Round(Sum / (float)KernelWeight));
                }
            }
            return Output;
        }

        private int[,] GenerateGaussianKernel(int N, float S, out int Weight)
        {
            int KernelLength = N;
            float Sigma = S;
            int KernelSize = KernelLength / 2;

            float[,] Kernel = new float[KernelLength, KernelLength];
            int[,] GaussianKernel = new int[KernelLength, KernelLength];

            float D1 = (2 * (float)Math.PI * Sigma * Sigma);
            float D2 = 2 * Sigma * Sigma;

            float min = 1000;
            for (int i = -KernelSize; i <= KernelSize; i++)
            {
                for (int j = -KernelSize; j <= KernelSize; j++)
                {
                    Kernel[KernelSize + i, KernelSize + j] = ((1 / D1) * (float)Math.Exp(-(i * i + j * j) / D2));
                    if (Kernel[KernelSize + i, KernelSize + j] < min)
                        min = Kernel[KernelSize + i, KernelSize + j];
                }
            }

            int mult = 1;
            int sum = 0;
            if ((min > 0) && (min < 1))
                mult = (int)(1 / min);

            for (int i = -KernelSize; i <= KernelSize; i++)
            {
                for (int j = -KernelSize; j <= KernelSize; j++)
                {
                    Kernel[KernelSize + i, KernelSize + j] = (float)Math.Round(Kernel[KernelSize + i, KernelSize + j] * mult, 0);
                    GaussianKernel[KernelSize + i, KernelSize + j] = (int)Kernel[KernelSize + i, KernelSize + j];
                    sum = sum + GaussianKernel[KernelSize + i, KernelSize + j];
                }
            }

            //Normalizing kernel Weight
            Weight = sum;

            return GaussianKernel;
        }

        private float[,] Differentiate(int[,] Data, int[,] Filter)
        {
            int Width = Data.GetLength(0);
            int Height = Data.GetLength(1);
            int Fw = Filter.GetLength(0);
            int Fh = Filter.GetLength(1);
            float[,] Output = new float[Width, Height];

            for (int i = (Fw / 2); i < (Width - (Fw / 2)); i++)
            {
                for (int j = (Fh / 2); j < (Height - (Fh / 2)); j++)
                {
                    float sum = 0F;
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

        private void HysterisisThresholding(int[,] Edges, int KernelLength)
        {
            int KernelSize = KernelLength / 2;
            for (int i = KernelSize; i < Edges.GetLength(0) - KernelSize; i++)
            {
                for (int j = KernelSize; j < Edges.GetLength(1) - KernelSize; j++)
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
        }
    }//Canny Class Ends
}