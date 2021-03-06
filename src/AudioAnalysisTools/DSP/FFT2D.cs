// <copyright file="FFT2D.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace AudioAnalysisTools.DSP
{
    using System;
    using System.Linq;
    using System.Numerics;

    // this is needed for the class ComplexExtensions which does the calculation of the magnitude of a complex number.
    using MathNet.Numerics.IntegralTransforms;
    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.PixelFormats;

    //using MathNet.Numerics.ComplexExtensions;
    using TowseyLibrary;

    /// <summary>
    /// Performs two dimensional FFT on a matrix of values.
    /// IMPORTANT: The matrix passed to this class for performing of 2D FFT need not necessarily have width equal to height
    /// but both width and height MUST be a power of two.
    /// </summary>
    public class FFT2D
    {
        /// <summary>
        /// Performs a 2D-Fourier transform on data in the passed Matrix/image.
        /// </summary>
        public static double[,] FFT2Dimensional(double[,] M)
        {
            // Step 1: convert matrix to complex array
            //double[] sampleData = Matrix2PaddedVector(M);

            // AT 20180202: updated call to support newer MathNet API
            var sampleData = Matrix2ComplexVector(M);

            int rowCount = M.GetLength(0);
            int colCount = M.GetLength(1);

            // Step 2: do 2d fft
            Fourier.Forward2D(sampleData, rowCount, colCount, FourierOptions.Matlab);

            // Step 3: Convert complex output array to array of real magnitude values
            var magnitudeMatrix = Fft2DOutputToMatrixOfMagnitude(rowCount, colCount, sampleData);

            // Step 3: do the shift for array of magnitude values.
            // var outputData = magnitudeMatrix; // no shifting
            var outputData = fftShift(magnitudeMatrix);

            return outputData;
        }

        /// <summary>
        /// Concatenates the columns of the passed matrix and inserts zeros in every second position.
        /// The matrix is assumed to be an image and therefore read it using image coordinates.
        /// The output vector is now assumed to be a vector of Complex numbers,
        /// with the real values in the even positions and the imaginary numbers in the odd positions.
        /// </summary>
        public static double[] Matrix2PaddedVector(double[,] M)
        {
            int rowCount = M.GetLength(0);
            int colCount = M.GetLength(1);

            // set up sampleData with additional space for padding zeroes.
            var sampleData = new double[rowCount * colCount * 2];
            for (var r = 0; r < rowCount; r++)
            {
                for (var c = 0; c < colCount; c++)
                {
                    sampleData[((r * rowCount) + c) * 2] = M[r, c];
                    sampleData[(((r * rowCount) + c) * 2) + 1] = 0.0;
                }
            }

            return sampleData;
        }

        /// <summary>
        /// Concatenates the columns of the passed matrix and inserts zeros in every second position.
        /// The matrix is assumed to be an image and therefore read it using image coordinates.
        /// The output vector is now a vector of Complex numbers, with the imaginary part set to 0.
        /// </summary>
        /// <remarks>
        /// This method was created to replicate the functionality of <see cref="Matrix2PaddedVector(double[,])"/>
        /// to support a changed MathNet API.
        /// </remarks>
        /// <param name="M">The input matrix.</param>
        /// <returns>A flattened <paramref name="M" /> as a vactor.</returns>
        public static Complex[] Matrix2ComplexVector(double[,] M)
        {
            int rowCount = M.GetLength(0);
            int colCount = M.GetLength(1);

            // set up sampleData with additional space for padding zeroes.
            var sampleData = new Complex[rowCount * colCount];
            for (var r = 0; r < rowCount; r++)
            {
                for (var c = 0; c < colCount; c++)
                {
                    sampleData[(r * rowCount) + c] = new Complex(real: M[r, c], imaginary: 0.0);
                }
            }

            return sampleData;
        }

        /// <summary>
        /// First construct complex sampleData, then calculate the magnitude of sampleData.
        /// </summary>
        public static double[,] FFT2DOutput2MatrixOfMagnitude(double[] sampleData, int[] dims)
        {
            // After 2D-FFT transformation, the sampleData array now has alternating real and imaginary values.
            // Create an array of class Complex.
            Complex[] sampleComplexPairs = new Complex[sampleData.Length / 2];
            for (int i = 0; i < sampleData.Length; i++)
            {
                // even number save real values for complex
                if (i % 2 == 0)
                {
                    var item = new Complex(
                        real: sampleData[i],
                        imaginary: sampleData[i + 1]);
                    sampleComplexPairs[i / 2] = item;
                }
            }

            //int[] dims = { rowCount, colCount };
            int rowCount = dims[0];
            int colCount = dims[1];

            return Fft2DOutputToMatrixOfMagnitude(rowCount, colCount, sampleComplexPairs);
        }

        private static double[,] Fft2DOutputToMatrixOfMagnitude(int rowCount, int colCount, Complex[] sampleComplexPairs)
        {
            var outputData = new double[rowCount, colCount];
            for (var r = 0; r < rowCount; r++)
            {
                for (var c = 0; c < colCount; c++)
                {
                    outputData[r, c] = Math.Sqrt(Math.Pow(sampleComplexPairs[(r * rowCount) + c].Real, 2.0)
                                                 + Math.Pow(sampleComplexPairs[(r * rowCount) + c].Imaginary, 2.0));

                    //var magnitude = ComplexExtensions.SquareRoot(sampleComplexPairs[r * matrixRowCount + c]);
                }
            }

            return outputData;
        }

        /// <summary>
        /// This method "shifts" (that is, "rearranges") the quadrants of the magnitude matrix generated by the 2DFourierTransform
        /// such that the Top Left  quadrant is swapped with the Bottom-Right quadrant
        ///       and the Top-Right quadrant is swapped with the Bottom-Left.
        /// This has the effect of shifting the low frequency coefficients into the centre of the matrix and the high frequency
        /// coefficients are shifted to the edge of the image.
        /// </summary>
        public static double[,] fftShift(double[,] matrix)
        {
            var rowCount = matrix.GetLength(0);
            var colCount = matrix.GetLength(1);

            var shiftedMatrix = new double[rowCount, colCount];
            var quadrantLength = rowCount / 2;

            for (int r = 0; r < rowCount; r++)
            {
                for (int c = 0; c < colCount; c++)
                {
                    if (r < quadrantLength)
                    {
                        if (c < quadrantLength)
                        {
                            // the first quadrant.
                            shiftedMatrix[r + quadrantLength, c + quadrantLength] = matrix[r, c];
                        }
                        else
                        {
                            // the second quadrant.
                            shiftedMatrix[r + quadrantLength, c - quadrantLength] = matrix[r, c];
                        }
                    }
                    else
                    {
                        // the third quadrant.
                        if (c < quadrantLength)
                        {
                            shiftedMatrix[r - quadrantLength, c + quadrantLength] = matrix[r, c];
                        }
                        else
                        {
                            // the fourth quadrant
                            shiftedMatrix[r - quadrantLength, c - quadrantLength] = matrix[r, c];
                        }
                    }
                }
            }

            return shiftedMatrix;
        }

        /// <summary>
        /// reads an image into a matrix.
        /// Takes weighted average of the RGB colours in each pixel.
        /// </summary>
        public static double[,] GetImageDataAsGrayIntensity(string imageFilePath, bool reversed)
        {
            Image<Rgb24> image = Image.Load<Rgb24>(imageFilePath);
            var rowCount = image.Height;
            var colCount = image.Width;
            var result = new double[rowCount, colCount];
            for (int r = 0; r < rowCount; r++)
            {
                for (int c = 0; c < colCount; c++)
                {
                    result[r, c] = (0.299 * image[c, r].R)
                                 + (0.587 * image[c, r].G)
                                 + (0.114 * image[c, r].B);
                    if (reversed) // reverse the image
                    {
                        result[r, c] = 255 - result[r, c];
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// METHOD to TEST the FFT2D.
        /// </summary>
        public static void TestFFT2D()
        {
            string imageFilePath = @"C:\SensorNetworks\Output\FFT2D\test5.png";
            bool reversed = false;
            double[,] matrix = GetImageDataAsGrayIntensity(imageFilePath, reversed);

            //matrix = MatrixTools.NormaliseMatrixValues(matrix);
            double[,] output = FFT2Dimensional(matrix);
            Console.WriteLine("Sum={0}", MatrixTools.Matrix2Array(output).Sum());

            //draws matrix after normalisation with white=low and black=high
            ImageTools.DrawReversedMatrix(output, @"C:\SensorNetworks\Output\FFT2D\test5_2DFFT.png");
        }
    }
}