﻿namespace OpenProtocolInterpreter.MIDs.MultipleIdentifiers
{
    /// <summary>
    /// MID: Multiple identifier and result parts unsubscribe
    /// Description: 
    ///    Reset the subscription for the multiple identifiers and result parts.
    /// Message sent by: Integrator
    /// Answer: MID 0005 Command accepted or MID 0004 Command error,
    /// Multiple identifiers and result parts subscription does not exist
    /// </summary>
    public class MID_0154 : MID, IMultipleIdentifier
    {
        public const int MID = 154;
        private const int length = 20;
        private const int revision = 1;

        public MID_0154() : base(length, MID, revision) { }

        internal MID_0154(IMID nextTemplate) : base(length, MID, revision)
        {
            this.nextTemplate = nextTemplate;
        }

        public override MID processPackage(string package)
        {
            if (base.isCorrectType(package))
                return (MID_0154)base.processPackage(package);

            return this.nextTemplate.processPackage(package);
        }

        protected override void registerDatafields() { }
    }
}
