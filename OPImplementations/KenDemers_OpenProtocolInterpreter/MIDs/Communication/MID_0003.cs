﻿namespace OpenProtocolInterpreter.MIDs.Communication
{
    /// <summary>
    /// MID: Application Communication stop
    /// Description:
    ///     This message disables the communication. The controller will stop to respond to any commands
    ///     except for MID 0001 Communication start after receiving this command.
    /// Message sent by: Controller
    /// Answer: MID 0005 Command accepted
    /// </summary>
    public class MID_0003 : MID, ICommunication
    {
        private const int length = 20;
        public const int MID = 3;
        private const int revision = 1;

        public MID_0003() : base(length, MID, revision) { }

        internal MID_0003(IMID nextTemplate) : base(length, MID, revision)
        {
            this.nextTemplate = nextTemplate;
        }

        public override MID processPackage(string package)
        {
            if (base.isCorrectType(package))
                return (MID_0003)base.processPackage(package);

            return this.nextTemplate.processPackage(package);
        }

        protected override void registerDatafields() { }
    }
}
