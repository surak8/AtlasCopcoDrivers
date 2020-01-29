﻿namespace OpenProtocolInterpreter.MIDs.PowerMACS
{
    /// <summary>
    /// MID: Last PowerMACS tightening result data subscribe
    /// Description: 
    ///    Set the subscription for the rundowns result. The result of this command will be the transmission of
    ///    the rundown result after the tightening is performed(push function).
    ///    This telegram is also used for a PowerMACS 4000 system running a press instead of a spindle.A
    ///    press system only supports revision 4 and higher of the telegram and will answer with MID 0004,
    ///    MID revision unsupported if a subscription is made with a lower revision.
    /// Message sent by: Integrator
    /// Answer: MID 0005 Command accepted or 
    ///         MID 0004 Command error, Subscription already exists or MID revision unsupported
    /// </summary>
    public class MID_0105 : MID, IPowerMACS
    {
        public const int MID = 105;
        private const int length = 20;
        private const int revision = 1;

        public MID_0105() : base(length, MID, revision) { }

        internal MID_0105(IMID nextTemplate) : base(length, MID, revision)
        {
            this.nextTemplate = nextTemplate;
        }

        public override MID processPackage(string package)
        {
            if (base.isCorrectType(package))
                return (MID_0105)base.processPackage(package);

            return this.nextTemplate.processPackage(package);
        }

        protected override void registerDatafields() { }
    }
}
