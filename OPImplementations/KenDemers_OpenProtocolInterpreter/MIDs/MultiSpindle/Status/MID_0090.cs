﻿namespace OpenProtocolInterpreter.MIDs.MultiSpindle.Status
{
    /// <summary>
    /// MID: Multi-spindle status subscribe
    /// Description:
    ///     A subscription for the multi-spindle status. For Power Focus, the subscription must be addressed to the sync Master.
    /// Message sent by: Integrator
    /// Answer: MID 0005 Command accepted or MID 0004 Command error, 
    ///         Controller is not a sync master/station controller, 
    ///         or Multi-spindle status subscription already exists
    /// </summary>
    public class MID_0090 : MID, IMultiSpindle
    {   
        public const int MID = 90;
        private const int length = 20;
        private const int revision = 1;

        public MID_0090() : base(length, MID, revision) { }

        internal MID_0090(IMID nextTemplate) : base(length, MID, revision)
        {
            this.nextTemplate = nextTemplate;
        }

        public override MID processPackage(string package)
        {
            if (base.isCorrectType(package))
                return (MID_0090)base.processPackage(package);

            return this.nextTemplate.processPackage(package);
        }

        protected override void registerDatafields() { }
    }
}
