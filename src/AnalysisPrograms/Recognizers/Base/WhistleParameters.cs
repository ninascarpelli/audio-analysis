// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WhistleParameters.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>
// <summary>

namespace AnalysisPrograms.Recognizers.Base
{
    using Acoustics.Shared;

    /// <summary>
    /// Parameters needed from a config file to detect whistle components.
    /// </summary>
    [YamlTypeTag(typeof(WhistleParameters))]
    public class WhistleParameters : CommonParameters
    {
    }
}
