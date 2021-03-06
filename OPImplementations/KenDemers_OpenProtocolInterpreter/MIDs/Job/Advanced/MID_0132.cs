﻿namespace OpenProtocolInterpreter.MIDs.Job.Advanced
{
    /// <summary>
    /// MID: Set Job line alert 1
    /// Description: The integrator can set the line control alert 1 in the controller with this message.
    /// Message sent by: Integrator
    /// Answer: MID 0005 Command accepted
    /// </summary>
    public class MID_0132 : MID, IAdvancedJob
    {
        private const int length = 20;
        public const int MID = 132;
        private const int revision = 1;

        public MID_0132() : base(length, MID, revision) { }

        internal MID_0132(IMID nextTemplate) : base(length, MID, revision)
        {
            this.nextTemplate = nextTemplate;
        }

        public override MID processPackage(string package)
        {
            if (base.isCorrectType(package))
                return (MID_0132)base.processPackage(package);

            return this.nextTemplate.processPackage(package);
        }

        protected override void registerDatafields() { }
    }
}
