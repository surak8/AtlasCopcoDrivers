﻿namespace OpenProtocolInterpreter.MotorTuning
{
    /// <summary>
    /// MID: Motor tuning request
    /// Description: 
    ///     Request the start of the motor tuning.
    ///     
    ///     Warning !: This command must be implemented during hard restrictions and 
    ///     customer dependent requirements.
    /// Message sent by: Integrator
    /// Answer: MID 0005 Command accepted or 
    ///         MID 0004 Command error, Tool motor tuning failed
    /// </summary>
    public class Mid0504 : Mid, IMotorTuning, IIntegrator
    {
        private const int LAST_REVISION = 1;
        public const int MID = 504;

        public Mid0504() : base(MID, LAST_REVISION) { }
    }
}
