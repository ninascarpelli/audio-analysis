﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using TowseyLib;


namespace AudioAnalysisTools
{


    public enum TrackType { none, deciBels, waveEnvelope, segmentation, syllables, scoreArray, scoreMatrix, zeroCrossings, hits, timeTics }


    public sealed class Image_Track
    {
        #region Constants
        public const int DefaultHeight = 30;   //pixel height of a track
        public const int timeScaleHt = 10;   //pixel height of the top and bottom time scales
        public const int syllablesTrackHeight = 10;   //pixel height of a track
        public const int envelopeTrackHeight  = 40;   //pixel height of a track
        public const int scoreTrackHeight = 20;   //pixel height of a track
        #endregion


        #region static var - colors for tracks
        public static Color[] TrackColors = {Color.White, Color.Red, Color.Orange, Color.Cyan, Color.OrangeRed, Color.Pink, Color.Salmon, Color.Tomato, Color.DarkRed, Color.Purple, 
                                          /*Color.AliceBlue,*/ /*Color.Aqua, Color.Aquamarine, Color.Azure, Color.Bisque,*/
                             Color.Blue, Color.BlueViolet, /*Color.Brown, Color.BurlyWood,*/ Color.CadetBlue, /*Color.Chartreuse,*/ 
                             Color.Chocolate, /*Color.Coral,*/ /*Color.CornflowerBlue,*/ /*Color.Cornsilk,*/ Color.Crimson, Color.DarkBlue, 
                             Color.DarkCyan, Color.DarkGoldenrod, Color.DarkGray, Color.DarkGreen, Color.DarkKhaki, Color.DarkMagenta, 
                             Color.DarkOliveGreen, Color.DarkOrange, Color.DarkOrchid, Color.DarkSalmon, Color.DarkSeaGreen, 
                             Color.DarkSlateBlue, Color.DarkSlateGray, Color.DarkTurquoise, Color.DarkViolet, Color.DeepPink, Color.DeepSkyBlue, 
                             Color.DimGray, Color.DodgerBlue, Color.Firebrick, Color.ForestGreen, Color.Fuchsia, 
                             Color.Gainsboro, Color.Gold, Color.Goldenrod, /*Color.Gray,*/ Color.Green, /*Color.GreenYellow,*/ 
                             Color.Honeydew, Color.HotPink, Color.IndianRed, Color.Indigo, /*Color.Khaki,*/ Color.Lavender, 
                             /*Color.LavenderBlush,*/ Color.LawnGreen, /*Color.LemonChiffon,*/ Color.Lime, 
                             Color.LimeGreen, /*Color.Linen,*/ Color.Magenta, Color.Maroon, Color.MediumAquamarine, Color.MediumBlue, 
                             /*Color.MediumOrchid,*/ Color.MediumPurple, /*Color.MediumSeaGreen,*/ Color.MediumSlateBlue, Color.MediumSpringGreen, 
                             Color.MediumTurquoise, Color.MediumVioletRed, Color.MidnightBlue, /*Color.MistyRose,*/ /*Color.Moccasin,*/ 
                             Color.Navy, /*Color.OldLace,*/ Color.Olive, /*Color.OliveDrab,*/ 
                             /*Color.Orchid, Color.PaleVioletRed, Color.PapayaWhip, */
                             /*Color.PeachPuff,*/ /*Color.Peru,*/ Color.Plum, /*Color.PowderBlue,*/ Color.RosyBrown, 
                             Color.RoyalBlue, Color.SaddleBrown, /*Color.SandyBrown,*/ Color.SeaGreen, /*Color.Sienna,*/ 
                             /*Color.Silver,*/ Color.SkyBlue, Color.SlateBlue, /*Color.SlateGray,*/ Color.SpringGreen, Color.SteelBlue, 
                             /*Color.Tan,*/ Color.Teal, Color.Thistle, Color.Turquoise, Color.Violet, /*Color.Wheat,*/ 
                             /*Color.Yellow,*/ Color.YellowGreen,  Color.Black};
        #endregion


        #region Properties
        public TrackType TrackType { get; set; }

        public double scoreMin = 0.0; //default min score displayed in score track of image
        public double ScoreMin { get { return scoreMin; } set { scoreMin = value; } }
        public double scoreMax = 10.0; //default max score displayed in score track of image
        public double ScoreMax { get { return scoreMax; } set { scoreMax = value; } }
        public double ScoreThreshold { set; get; }

        public int topOffset { get; set; }    //set to track's TOP    pixel row in final image
        public int bottomOffset { get; set; } //set to track's BOTTOM pixel row in final image
        private int height = DefaultHeight;
        public int Height { get { return height; } set { height = value; } }
        private int[] intData = null;
        private double[] doubleData = null;
        private double[,] doubleMatrix = null;

        TimeSpan timeSpan { set; get; }
        double timeScale { set; get; } //pixels per second

        private double[] doubleData1 = null;
        private double[] doubleData2 = null;

        //these params used for segmentation track
        public double SegmentationThreshold_k1 { set; get; }
        public double SegmentationThreshold_k2 { set; get; }

        private int garbageID = 0;
        public int GarbageID { get { return garbageID; } set { garbageID = value; } }
        #endregion



        #region Constructors
        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        /// <param name="type"></param>
        /// <param name="data"></param>
        public Image_Track(TrackType type, int[] data)
        {
            this.TrackType = type;
            this.intData = data;
            this.height = SetTrackHeight();
            //if(SonoImage.Verbose)Console.WriteLine("\tTrack CONSTRUCTOR: trackType = " + type + "  Data = " + data.ToString());
        }
        public Image_Track(TrackType type, double[] data)
        {
            this.TrackType = type;
            this.doubleData = data;
            this.height = SetTrackHeight();
            //if (SonoImage.Verbose) Console.WriteLine("\tTrack CONSTRUCTOR: trackType = " + type + "  Data = " + data.ToString());
        }
        /// <summary>
        /// used for showing the singal envelope track
        /// </summary>
        /// <param name="type"></param>
        /// <param name="data1"></param>
        /// <param name="data2"></param>
        public Image_Track(TrackType type, double[] data1, double[] data2)
        {
            this.TrackType = type;
            this.doubleData1 = data1;
            this.doubleData2 = data2;
            this.height = SetTrackHeight();
        }
        public Image_Track(TrackType type, double[,] data)
        {
            this.TrackType = type;
            this.doubleMatrix = data;
            this.height = SetTrackHeight();
        }
        public Image_Track(TrackType type, TimeSpan t, double pixelsPerSecond)
        {
            this.TrackType = type;
            this.timeSpan = t;
            this.timeScale = pixelsPerSecond;
            this.height = SetTrackHeight();
        }
        public Image_Track(TrackType type)
        {
            this.TrackType = type;
        }

        #endregion


        public void SetIntArray(int[] data)
        {
            this.intData = data;
        }

        private int SetTrackHeight()
        {
            switch (TrackType)
            {
                case TrackType.timeTics:
                    return timeScaleHt;
                case TrackType.syllables:
                    return syllablesTrackHeight;
                case TrackType.scoreArray:
                    return scoreTrackHeight;
                case TrackType.deciBels:
                    return DefaultHeight;
                case TrackType.waveEnvelope:
                    return envelopeTrackHeight;
                case TrackType.segmentation:
                    return DefaultHeight;
                case TrackType.scoreMatrix:
                    return DefaultHeight;
                default:
                    return DefaultHeight;
            }
        }

        public void DrawTrack(Bitmap bmp)
        {
            //Log.WriteIfVerbose("\tDrawing track type =" + TrackType);
            switch (TrackType)
            {
                case TrackType.timeTics:
                    DrawTimeTrack(bmp);    //time scale track
                    break;
                case TrackType.deciBels:
                    DrawDecibelTrack(bmp); //frame energy track
                    break;
                case TrackType.waveEnvelope:
                    DrawWaveEnvelopeTrack(bmp); //signal envelope track
                    break;
                case TrackType.segmentation:
                    DrawSegmentationTrack(bmp); //segmentation track
                    break;
                case TrackType.syllables:
                    DrawSyllablesTrack(bmp);
                    break;
                case TrackType.scoreArray:
                    DrawScoreArrayTrack(bmp);  //add a score track
                    break;
                case TrackType.scoreMatrix:
                    DrawScoreMatrixTrack(bmp);  //add a score track
                    break;
                default:
                    Log.WriteLine("WARNING******** !!!! Image_Track.DrawTrack():- TRACKTYPE NOT DEFINED");
                    break;

            }
            // none, energy, syllables, scoreArray, scoreMatrix, zeroCrossings, hits 
        }

        /// <summary>
        /// paints a track of symbol colours derived from symbol ID
        /// </summary>
        public Bitmap DrawSyllablesTrack(Bitmap bmp)
        {
            int bmpWidth = bmp.Width;
            //int bmpHt = bmp.Height;
            Color gray = Color.LightGray;
            Color white = Color.White;
            //Color red = Color.Red;
            if ((intData == null) || (intData.Length == 0))
            {
                Console.WriteLine("#####WARNING!! AddScoreArrayTrack(Bitmap bmp):- Integer data does not exists!");
                return bmp;
            }

            //Console.WriteLine("offset=" + this.offset);
            int bottom = topOffset + this.height - 1;
            for (int x = 0; x < Math.Min(bmp.Width, intData.Length); x++)
            {
                Color col = TrackColors[intData[x]];
                if (intData[x] == 0) col = white;
                if (intData[x] == this.garbageID) col = gray;
                for (int z = 0; z < this.height; z++) bmp.SetPixel(x, topOffset + z, col);  //add in hits
                bmp.SetPixel(x, bottom, Color.Black);
            }
            return bmp;
        }


        public Bitmap DrawTimeTrack(Bitmap bmp)
        {
            int width = bmp.Width;
            int height = bmp.Height;

            Graphics g = Graphics.FromImage(bmp);
            DateTime start = new DateTime(0); 

            byte[] hScale = CreateXaxis(width, this.timeSpan);
            Color black = Color.Black;
            Color gray = Color.Gray;
            Color white = Color.White;
            Color c = white;
            topOffset += (timeScaleHt - 1);//shift offset to bottom of scale

            for (int x = 0; x < width; x++)
            {
                c = white;
                if (hScale[x] == 50) c = gray;
                else
                    if (hScale[x] == 0) c = black;

                //top axis
                int secs = (int)Math.Round(x / timeScale);
                TimeSpan span = new TimeSpan(0, 0, secs);

                //bottom axis tick marks
                for (int h = 0; h < timeScaleHt; h++) bmp.SetPixel(x, topOffset - h, c);
                bmp.SetPixel(x, topOffset, black);                    // top line of scale
                bmp.SetPixel(x, topOffset - timeScaleHt + 1, black);  // bottom line of scale
                if (hScale[x] == 0) g.DrawString(span.ToReadableString(), new Font("Tahoma", 8), Brushes.Black, new PointF(x, topOffset - 21));
            } //end of adding time grid
            return bmp;
        }

        public byte[] CreateXaxis(int width, TimeSpan timeSpan)
        {
            byte[] ba = new byte[width]; //byte array
            double duration = timeSpan.TotalSeconds;
            double period = duration / (double)width;

            for (int x = 0; x < width; x++) ba[x] = (byte)230; //set background pale gray colour
            for (int x = 0; x < width - 1; x++)
            {
                double elapsedTime = x * period; // / (double)width;
                double mod1sec = elapsedTime % 1.0000;
                double mod10sec = elapsedTime % 10.0000;
                if (mod10sec < period)//double black line
                {
                    ba[x] = (byte)0;
                    ba[x + 1] = (byte)0;
                }
                else
                    if (mod1sec <= period)
                    {
                        ba[x] = (byte)50;
                    }
            }
            return ba; //byte array
        }

        /// <summary>
        /// Displays a score track, normalised to min and max of the data. max=approx 8-16.
        /// </summary>
        public Bitmap DrawScoreArrayTrack(Bitmap bmp)
        {
            //Console.WriteLine("DRAW SCORE TRACK: image ht=" + bmp.Height + "  topOffset = " + topOffset + "   botOffset =" + bottomOffset);
            if (doubleData == null) return bmp;
            int dataLength = this.doubleData.Length;
            double range = this.scoreMax - this.scoreMin;

            int bmpWidth = bmp.Width;
            int subSample = (int)Math.Round((double)(dataLength / bmp.Width));
            if (subSample < 1) subSample = 1;

            Color gray = Color.LightGray;
            Color white = Color.White;

            for (int w = 0; w < bmpWidth; w++)
            {
                int start = w * subSample;
                int end = (w + 1) * subSample;
                double max = -Double.MaxValue;
                int location = 0;
                for (int x = start; x < end; x++)
                {
                    if (max < doubleData[x]) max = doubleData[x];
                    location = x;
                }
                double fraction = (doubleData[w] - this.scoreMin) / range;
                int id = this.Height - 1 - (int)(this.Height * fraction);
                if (id < 0) id = 0;
                else if (id > this.Height) id = this.Height;
                //paint white and leave a black vertical histogram bar
                for (int z = 0; z < id; z++) bmp.SetPixel(w, topOffset + z, white);
            }

            //add in horizontal threshold significance line
          //  double thold = 2 * this.ScoreThreshold;
           // if (thold < this.ScoreMax) thold = this.ScoreMax;
          //  int lineID = (int)(this.Height * (1 - (this.ScoreThreshold / thold)));
            double f = (this.ScoreThreshold - this.scoreMin) / range;
            int lineID = this.Height - 1 - (int)(this.Height * f);
            if (lineID < 0) lineID = 0;
            else if (lineID > this.Height) lineID = this.Height;
            for (int x = 0; x < bmpWidth; x++) bmp.SetPixel(x, topOffset + lineID, gray);

            return bmp;
        }

        /// <summary>
        /// This method assumes that the passed data array is of values, min=0.0, max = approx 8-16.
        /// </summary>
        public Bitmap DrawScoreMatrixTrack(Bitmap bmp)
        {
            int bmpWidth = bmp.Width;
            int dataLength = this.intData.Length;
            int subSample = (int)Math.Round((double)(dataLength / bmp.Width));
            if (subSample < 1) subSample = 1;

            Color gray = Color.LightGray;
            Color white = Color.White;
            Color red = Color.Red;

            int numberOfScoreTracks = this.doubleMatrix.GetLength(0);
            double[] scores = new double[numberOfScoreTracks];
            for (int x = 0; x < bmpWidth; x++)
            {
                //transfer scores
                for (int y = 0; y < numberOfScoreTracks; y++) scores[y] = this.doubleMatrix[y, x];
                int maxIndex = DataTools.GetMaxIndex(scores);

                int id = this.Height - 1 - (int)(this.Height * scores[maxIndex] / this.ScoreMax);
                if (id < 0) id = 0;
                else if (id > this.Height) id = this.Height;
                //paint white and leave a black vertical histogram bar
                for (int z = 0; z < id; z++) bmp.SetPixel(x, topOffset + z, white);
                for (int z = id; z < this.Height; z++) bmp.SetPixel(x, topOffset + z, TrackColors[maxIndex + 15]);
            }

            //add in horizontal threshold significance line
            double max = 2 * this.ScoreThreshold;
            if (max < this.ScoreMax) max = this.ScoreMax;
            int lineID = (int)(this.Height * (1 - (this.ScoreThreshold / max)));
            for (int x = 0; x < bmpWidth; x++) bmp.SetPixel(x, topOffset + lineID, gray);

            return bmp;
        }

        /// <summary>
        /// This method assumes that the passed decibel array has been normalised
        /// </summary>
        public Bitmap DrawDecibelTrack(Bitmap bmp)
        {
            int dataLength = doubleData.Length;
            int subSample = (int)Math.Round((double)(dataLength / bmp.Width));
            if (subSample < 1) subSample = 1;

            for (int w = 0; w < bmp.Width; w++)
            {
                int start = w * subSample;
                int end = (w+1) * subSample;
                double max = -Double.MaxValue;
                int location = 0;
                for (int x = start; x < end; x++)
                {
                    if (max < doubleData[x]) max = doubleData[x];
                    location = x;
                }

                double norm = doubleData[location];
                int id = Image_Track.DefaultHeight - 1 - (int)(Image_Track.DefaultHeight * norm);
                if (id < 0) id = 0;
                else if (id > Image_Track.DefaultHeight) id = Image_Track.DefaultHeight;
                //paint white and leave a black vertical bar
                for (int z = 0; z < id; z++) bmp.SetPixel(w, topOffset + z, Color.White);
            }

            return bmp;
        }

        /// <summary>
        /// assumes that max signal value = 1.0 and min sig value = -1.0 i.e. wav file values 
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns></returns>
        public Bitmap DrawWaveEnvelopeTrack(Bitmap bmp)
        {
            int halfHeight = this.height / 2;
            Color c = Color.FromArgb(10, 200, 255);

            for (int w = 0; w < bmp.Width; w++)
            {
                int minID = halfHeight + (int)Math.Round(doubleMatrix[0, w] * halfHeight);
                //minID = halfHeight + (int)Math.Round(-1.0 * halfHeight);
                int maxID = halfHeight + (int)Math.Round(doubleMatrix[1, w] * halfHeight) -1;
                for (int z = minID; z <= maxID; z++) bmp.SetPixel(w, this.bottomOffset - z, c);
                bmp.SetPixel(w, this.topOffset + halfHeight, c); //set zero line in case it was missed
                if (doubleMatrix[0, w] < -0.99)
                {
                    bmp.SetPixel(w, this.bottomOffset - 1, Color.OrangeRed);
                    bmp.SetPixel(w, this.bottomOffset - 2, Color.OrangeRed);
                    bmp.SetPixel(w, this.bottomOffset - 3, Color.OrangeRed);
                }else 
                if (doubleMatrix[1, w] > 0.99)
                {
                    bmp.SetPixel(w, this.topOffset, Color.OrangeRed);
                    bmp.SetPixel(w, this.topOffset+1, Color.OrangeRed);
                    bmp.SetPixel(w, this.topOffset+2, Color.OrangeRed);
                }
                //bmp.SetPixel(w, this.topOffset, Color.OrangeRed);
                //bmp.SetPixel(w, this.bottomOffset - 1, Color.OrangeRed);
            }

            return bmp;
        }


        /// <summary>
        /// This method assumes that the passed decibel array has been normalised
        /// Also requires values to be set for SegmentationThreshold_k1 and SegmentationThreshold_k2

        /// </summary>
        public Bitmap DrawSegmentationTrack(Bitmap bmp)
        {
            bmp = DrawDecibelTrack(bmp);

            if (this.intData == null) return bmp;     //cannot show becuase no state info

            int dataLength = this.intData.Length;
            int subSample = (int)Math.Round((double)(dataLength / bmp.Width));
            if (subSample < 1) subSample = 1;

            //display vocalisation state and thresholds used to determine endpoints
            Color[] stateColors = { Color.White, Color.Green, Color.Red };
            double v1 = this.SegmentationThreshold_k1;
            int k1 = this.Height - (int)(this.Height * v1);
            double v2 = this.SegmentationThreshold_k2;
            int k2 = this.Height - (int)(this.Height * v2);
            if ((v1 < 0.0) || (v1 > 1.0)) return bmp; //thresholds are illegal so stop now.
            if ((v2 < 0.0) || (v2 > 1.0)) return bmp;

            //calculate location of the segmentation threshold lines
            int y1 = topOffset + k1;
            if (y1 >= bmp.Height) y1 = bmp.Height - 1;
            int y2 = topOffset + k2;
            if (y2 >= bmp.Height) y2 = bmp.Height - 1;

            for (int x = 0; x < bmp.Width; x++)
            {
                bmp.SetPixel(x, y1, Color.Orange);//threshold lines
                bmp.SetPixel(x, y2, Color.Lime);

                //put state as top four pixels
                int location = x * subSample;
                if (location > dataLength - 1) continue;
                Color col = stateColors[this.intData[location]];
                bmp.SetPixel(x, topOffset, col);
                bmp.SetPixel(x, topOffset + 1, col);
                bmp.SetPixel(x, topOffset + 2, col);
                bmp.SetPixel(x, topOffset + 3, col);
            }
            return bmp;
        }

        /// <summary>
        /// ASSUME that passed decibel array has been normalised
        /// </summary>
        /// <param name="sg"></param>
        /// <returns></returns>
        public static Image_Track GetDecibelTrack(BaseSonogram sg)
        {
            var track = new Image_Track(TrackType.deciBels, sg.DecibelsNormalised);
            return track;
        }

        /// <summary>
        /// ASSUME that passed decibel array has been normalised
        /// </summary>
        /// <param name="sg"></param>
        /// <returns></returns>
        public static Image_Track GetSegmentationTrack(BaseSonogram sg)
        {
            var track = new Image_Track(TrackType.segmentation, sg.DecibelsNormalised);
            track.intData = sg.SigState;
            track.SegmentationThreshold_k1 = EndpointDetectionConfiguration.K1Threshold / sg.Max_dBReference;
            track.SegmentationThreshold_k2 = EndpointDetectionConfiguration.K2Threshold / sg.Max_dBReference;
            return track;
        }

        public static Image_Track GetWavEnvelopeTrack(AudioRecording ar, int imageWidth)
        {
            double[,] envelope = ar.GetWaveForm(imageWidth);

            return GetWavEnvelopeTrack(envelope, imageWidth);
        }

        public static Image_Track GetWavEnvelopeTrack(double[,] envelope, int imageWidth)
        {            
            var track = new Image_Track(TrackType.waveEnvelope, envelope);
            return track;
        }

        public static Image_Track GetTimeTrack(TimeSpan t, double pixelsPerSecond)
        {
            var track = new Image_Track(TrackType.timeTics, t, pixelsPerSecond);
            //int height = track.Height;
            return track;
        }
        public static Image_Track GetSyllablesTrack(int[] SyllableIDs, int garbageID)
        {
            var track = new Image_Track(TrackType.syllables, SyllableIDs);
            track.GarbageID = garbageID;
            return track;
        }
        public static Image_Track GetScoreTrack(double[] scores, double? scoreMin, double? scoreMax, double? scoreThreshold)
        {
            var track = new Image_Track(TrackType.scoreArray, scores);
            track.ScoreThreshold = scoreThreshold ?? 0;
            track.ScoreMin = scoreMin ?? 0;
            track.ScoreMax = scoreMax ?? 0;
            return track;
        }
        public static Image_Track GetScoreTrack(int[] scores, int? scoreMin, int? scoreMax, int? scoreThreshold)
        {
            //convert integers to doubles
            var dscores = new double[scores.Length];
            for (int x = 0; x < scores.Length; x++) dscores[x] = scores[x];
            return GetScoreTrack(dscores, (double)scoreMin, (double)scoreMax, (double)scoreThreshold);
        }

        /// <summary>
        /// used to draw richness indices 
        /// </summary>
        /// <param name="scores"></param>
        /// <param name="scoreMin"></param>
        /// <param name="scoreMax"></param>
        /// <param name="scoreThreshold"></param>
        /// <returns></returns>
        public static void DrawScoreTrack(Bitmap bmp, double[] array, int yOffset, int trackHeight, double minVal, double maxVal, double threshold, string title)
        {
            Color[] grayScale = ImageTools.GrayScale();
            int imageWidth = array.Length;
            double range = maxVal - minVal;
            for (int x = 0; x < imageWidth; x++) //for pixels in the line
            {
                // normalise and bound the value - use min bound, max and 255 image intensity range
                double value = (array[x] - minVal) / range;
                int c = 255 - (int)Math.Floor(255.0 * value); //original version
                if (c < threshold) c = 0;
                else
                if (c >= 256) c = 255;

                Color col = grayScale[c];
                for (int y = 0; y < trackHeight; y++) bmp.SetPixel(x, yOffset + y, col);
                bmp.SetPixel(x, yOffset, grayScale[0]); //draw upper boundary
            }//end over all pixels
            Graphics g = Graphics.FromImage(bmp);
            g.DrawString(title, new Font("Tahoma", 8), Brushes.White, new PointF(imageWidth+5, yOffset));
        }

        /// <summary>
        /// used to draw score track or any array of values 
        /// </summary>
        /// <param name="scores"></param>
        /// <param name="scoreMin"></param>
        /// <param name="scoreMax"></param>
        /// <param name="scoreThreshold"></param>
        /// <returns></returns>
        public static Bitmap DrawGrayScaleScoreTrack(double[] array, int trackHeight, double minVal, double maxVal, double threshold, string title)
        {

            Color[] grayScale = ImageTools.GrayScale();
            int imageWidth = array.Length;
            Bitmap bmp = new Bitmap(imageWidth, trackHeight);


            double range = maxVal - minVal;
            for (int x = 0; x < imageWidth; x++) //for pixels in the line
            {
                // normalise and bound the value - use min bound, max and 255 image intensity range
                double value = (array[x] - minVal) / range;
                int c = 255 - (int)Math.Floor(255.0 * value); //original version
                if (c < threshold) c = 0;
                else
                    if (c >= 256) c = 255;

                Color col = grayScale[c];
                for (int y = 0; y < trackHeight; y++) bmp.SetPixel(x, y, col);
                bmp.SetPixel(x, 0, grayScale[0]); //draw upper boundary
            }//end over all pixels
            Graphics g = Graphics.FromImage(bmp);
            g.DrawString(title, new Font("Tahoma", 8), Brushes.White, new PointF(imageWidth + 5, 0));
            return bmp;
        }

        /// <summary>
        /// used to draw score track or any array of values 
        /// </summary>
        /// <param name="scores"></param>
        /// <param name="scoreMin"></param>
        /// <param name="scoreMax"></param>
        /// <param name="scoreThreshold"></param>
        /// <returns></returns>
        public static Bitmap DrawBarScoreTrack(double[] array, int trackHeight, double minVal, double maxVal, double threshold, string title)
        {

            Color[] grayScale = ImageTools.GrayScale();
            int imageWidth = array.Length;
            Bitmap bmp = new Bitmap(imageWidth, trackHeight);


            double range = maxVal - minVal;
            for (int x = 0; x < imageWidth; x++) //for pixels in the line
            {
                // normalise and bound the value - use min bound, max and 255 image intensity range
                double value = (array[x] - minVal) / range;
                int barHeight = (int)Math.Round(value * trackHeight);
                //int c = 255 - (int)Math.Floor(255.0 * value); //original version
                //if (c < threshold) c = 0;
                //else
                //    if (c >= 256) c = 255;

                //Color col = grayScale[c];
                for (int y = 0; y < barHeight; y++) bmp.SetPixel(x, trackHeight - y - 1, Color.Black);
                //bmp.SetPixel(x, 0, grayScale[0]); //draw upper boundary
            }//end over all pixels
            Graphics g = Graphics.FromImage(bmp);
            g.DrawString(title, new Font("Tahoma", 8), Brushes.White, new PointF(imageWidth + 5, 0));
            return bmp;
        }


        public static void DrawScoreTrack(Bitmap bmp, double[] array, int yOffset, int trackHeight, double threshold, string title)
        {
            double minVal;
            double maxVal;
            DataTools.MinMax(array, out minVal, out maxVal);
            DrawScoreTrack(bmp, array, yOffset, trackHeight, minVal, maxVal, threshold, title);
        }


        public static Bitmap DrawBarScoreTrack(double[] array, int trackHeight, double threshold, string title)
        {
            double minVal;
            double maxVal;
            DataTools.MinMax(array, out minVal, out maxVal);
            Bitmap bitmap = DrawBarScoreTrack(array, trackHeight, minVal, maxVal, threshold, title);
            return bitmap;
        }

        public static Bitmap DrawGrayScaleScoreTrack(double[] array, int trackHeight, double threshold, string title)
        {
            double minVal;
            double maxVal;
            DataTools.MinMax(array, out minVal, out maxVal);
            Bitmap bitmap = DrawGrayScaleScoreTrack(array, trackHeight, minVal, maxVal, threshold, title);
            return bitmap;
        }


        // mark of time scale according to scale.
        public static Bitmap DrawTimeTrack(int duration, int scale, int trackWidth, int trackHeight, string title)
        {
            Bitmap bmp = new Bitmap(trackWidth, trackHeight);
            Graphics g = Graphics.FromImage(bmp);
            g.Clear(Color.Black);
            
            int hour = 0;

            for (int x = 0; x < duration; x++) //for pixels in the line
            {
                bmp.SetPixel(x, 0, Color.White); //draw upper boundary
                if (x % scale != 0) continue;
                for (int y = 0; y < trackHeight; y++) bmp.SetPixel(x, y, Color.White);
                g.DrawString(hour.ToString(), new Font("Tahoma", 8), Brushes.White, new PointF(x + 2, 0)); //draw time
                hour++;
            }//end over all pixels

            g.DrawString(title, new Font("Tahoma", 8), Brushes.White, new PointF(duration + 5, 0));
            return bmp;
        }


        // mark of time scale according to scale.
        public static void DrawTimeTrack(Bitmap bmp, int duration, int scale, int yOffset, int trackHeight, string title)
        {
            Bitmap timeBmp = Image_Track.DrawTimeTrack(duration, scale, bmp.Width, trackHeight, title);
            Graphics gr = Graphics.FromImage(bmp);
            gr.DrawImage(timeBmp, 0, yOffset);
        }

    }// end  class Image_Track
}
