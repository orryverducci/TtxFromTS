using System;
using Cinegy.TsDecoder.Tables;
using Cinegy.TsDecoder.TransportStream;

namespace TtxFromTS.DVB
{
    /// <summary>
    /// Wraps the <see cref="T:Cinegy.TsDecoder.Tables.ProgramMapTableFactory"/> class,
    /// changing the return of PMT tables from an event to a method return.
    /// </summary>
    public class PMTFactory : ProgramMapTableFactory
    {
        private bool _changed = false;

        public PMTFactory() => TableChangeDetected += PMTChanged;

        private void PMTChanged(object sender, TransportStreamEventArgs args) => _changed = ProgramMapTable.CurrentNextIndicator;

        public new ProgramMapTable? AddPacket(TsPacket packet)
        {
            base.AddPacket(packet);
            if (_changed)
            {
                return ProgramMapTable;
            }
            else
            {
                return null;
            }
        }
    }
}
