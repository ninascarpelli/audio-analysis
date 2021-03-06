// <copyright file="AedEvent.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace AudioAnalysisTools.Events
{
    using System.Collections.Generic;
    using AudioAnalysisTools.Events.Drawing;
    using SixLabors.ImageSharp.Processing;

    public class AedEvent : SpectralEvent, IPointData
    {

        public ISet<ISpectralPoint> Points { get; } = new HashSet<ISpectralPoint>();

        public override void Draw(IImageProcessingContext graphics, EventRenderingOptions options)
        {
            ((IPointData)this).DrawPointsAsFill(graphics, options);

            //  base drawing (border)
            base.Draw(graphics, options);
        }
    }
}