using System;
using Cinegy.TsDecoder.TransportStream;
using Cinegy.TsDecoder.Tables;

namespace TtxFromTS.DVB
{
    /// <summary>
    /// Wraps the <see cref="T:Cinegy.TsDecoder.Tables.ProgramAssociationTableFactory"/> class,
    /// changing the return of PAT tables from an event to a method return.
    /// </summary>
    public class PATFactory : ProgramAssociationTableFactory
    {
        private bool _changed = false;

        public PATFactory() => TableChangeDetected += PATChanged;

        private void PATChanged(object sender, TransportStreamEventArgs args) => _changed = true;

        public new ProgramAssociationTable? AddPacket(TsPacket packet)
        {
            base.AddPacket(packet);
            if (_changed)
            {
                return ProgramAssociationTable;
            }
            else
            {
                return null;
            }
        }
    }
}
