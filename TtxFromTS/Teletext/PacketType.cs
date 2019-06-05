using System;

namespace TtxFromTS.Teletext
{
    /// <summary>
    /// The teletext packet types available.
    /// </summary>
    public enum PacketType
    {
        Header,
        PageBody,
        Fastext,
        TOPCommentary,
        PageReplacements,
        LinkedPages,
        PageEnhancements,
        MagazineEnhancements,
        BroadcastServiceData,
        Unspecified
    }
}