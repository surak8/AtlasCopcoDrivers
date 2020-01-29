﻿namespace OpenProtocolInterpreter.MIDs.ParameterSet
{
    /// <summary>
    /// MID: Lock at batch done unsubscribe
    /// Description: 
    ///     Reset the subscription for Lock at batch done.
    /// Message sent by: Integrator
    /// Answer: MID 0005 Command accepted or MID 0004 Command error
    /// </summary>
    public class MID_0024 : MID, IParameterSet
    {
        private const int length = 20;
        public const int MID = 24;
        private const int revision = 1;

        public MID_0024() : base(length, MID, revision) { }

        internal MID_0024(IMID nextTemplate) : base(length, MID, revision)
        {
            this.nextTemplate = nextTemplate;
        }

        public override MID processPackage(string package)
        {
            if (base.isCorrectType(package))
                return (MID_0024)base.processPackage(package);

            return this.nextTemplate.processPackage(package);
        }

        protected override void registerDatafields() { }
    }
}
