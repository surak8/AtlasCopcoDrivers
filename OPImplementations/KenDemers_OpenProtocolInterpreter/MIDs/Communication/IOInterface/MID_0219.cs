﻿using System;

namespace OpenProtocolInterpreter.MIDs.IOInterface
{
    /// <summary>
    /// MID: Relay function unsubscribe
    /// Description: 
    ///     Unsubscribe for a single relay function. The data field consists of three ASCII digits,
    ///     the relay number, which corresponds to the specific relay function. The relay numbers can be 
    ///     found in Table 101.
    /// Message sent by: Integrator
    /// Answer: MID 0005 Command accepted or
    ///         MID 0004 Command error, The relay function subscription does not exist
    /// </summary>
    public class MID_0219 : MID, IIOInterface
    {
        public const int MID = 219;
        private const int length = 23;
        private const int revision = 1;

        public Relay.RelayNumbers RelayNumber { get; set; }

        public MID_0219() : base(length, MID, revision) { }

        internal MID_0219(IMID nextTemplate) : base(length, MID, revision)
        {
            this.nextTemplate = nextTemplate;
        }

        public override string buildPackage()
        {
            return base.buildHeader() + ((int)this.RelayNumber).ToString().PadLeft(base.RegisteredDataFields[(int)DataFields.RELAY_NUMBER].Size, '0');
        }

        public override MID processPackage(string package)
        {
            if (base.isCorrectType(package))
            {
                base.processHeader(package);
                var dataField = base.RegisteredDataFields[(int)DataFields.RELAY_NUMBER];
                this.RelayNumber = (Relay.RelayNumbers)Convert.ToInt32(package.Substring(dataField.Index, dataField.Size));
                return this;
            }


            return this.nextTemplate.processPackage(package);
        }

        protected override void registerDatafields()
        {
            this.RegisteredDataFields.Add(new DataField((int)DataFields.RELAY_NUMBER, 20, 3));
        }

        public enum DataFields
        {
            RELAY_NUMBER
        }
    }
}
