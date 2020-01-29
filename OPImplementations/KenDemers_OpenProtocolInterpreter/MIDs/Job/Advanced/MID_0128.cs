﻿namespace OpenProtocolInterpreter.MIDs.Job.Advanced
{
    /// <summary>
    /// MID: Job batch increment
    /// Description: Increment the Job batch if there is a current running Job.
    /// Message sent by: Integrator
    /// Answer: MID 0005 Command accepted
    /// </summary>
    public class MID_0128 : MID, IAdvancedJob
    {
        private const int length = 20;
        public const int MID = 128;
        private const int revision = 1;

        public MID_0128() : base(length, MID, revision) { }

        internal MID_0128(IMID nextTemplate) : base(length, MID, revision)
        {
            this.nextTemplate = nextTemplate;
        }

        public override MID processPackage(string package)
        {
            if (base.isCorrectType(package))
                return (MID_0128)base.processPackage(package);

            return this.nextTemplate.processPackage(package);
        }

        protected override void registerDatafields() { }
    }
}
