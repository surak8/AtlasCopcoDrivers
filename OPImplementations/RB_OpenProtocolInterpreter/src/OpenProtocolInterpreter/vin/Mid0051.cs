﻿namespace OpenProtocolInterpreter.Vin
{
    /// <summary>
    /// MID: Vehicle ID Number subscribe
    /// Description: 
    ///     This message is used by the integrator to set a subscription for the current identifiers of the tightening
    ///     result.
    ///     The tightening result can be stamped with up to four identifiers:
    ///         - VIN number
    ///         - Identifier result part 2
    ///         - Identifier result part 3
    ///         - Identifier result part 4
    ///     The identifiers are received by the controller from several input sources, for example serial, Ethernet,
    ///     or field bus.
    ///     In revision 1 of the MID 0052 Vehicle ID Number, only the VIN number is transmitted.In revision 2, all
    ///     four possible identifiers are transmitted.
    /// Message sent by: Integrator
    /// Answer: MID 0005 Command accepted or MID 0004 Command error, VIN subscription already exists
    /// </summary>
    public class Mid0051 : Mid, IVin, IIntegrator
    {
        private const int LAST_REVISION = 2;
        public const int MID = 51;

        public Mid0051() : this(LAST_REVISION)
        {

        }

        public Mid0051(int revision = LAST_REVISION, int ? noAckFlag = 0) : base(MID, revision, noAckFlag)
        {

        }
    }
}
