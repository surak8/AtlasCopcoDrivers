﻿using System;

namespace OpenProtocolInterpreter.MIDs.IOInterface
{
    /// <summary>
    /// MID: Digital input function subscribe
    /// Description: 
    ///     Subscribe for one single digital input function. The data field consists of three ASCII digits, 
    ///     the digital input function number. The digital input function numbers can be found in Table 80 above.
    ///     At a subscription of a tracking event, MID 0221 Digital input function upload immediately returns the 
    ///     current digital input function status to the subscriber.
    ///     MID 0220 can only subscribe for one single digital input function at a time, 
    ///     but still, Open Protocol supports keeping several digital input function subscriptions simultaneously.
    /// Message sent by: Integrator
    /// Answer: MID 0005 Command accepted or
    ///         MID 0004 Command error, The digital input function subscription already exists
    /// </summary>
    public class MID_0220 : MID, IIOInterface
    {
        public const int MID = 220;
        private const int length = 23;
        private const int revision = 1;

        public DigitalInput.DigitalInputNumbers DigitalInputNumber { get; set; }

        public MID_0220() : base(length, MID, revision) { }

        internal MID_0220(IMID nextTemplate) : base(length, MID, revision)
        {
            this.nextTemplate = nextTemplate;
        }

        public override string buildPackage()
        {
            return base.buildHeader() + ((int)this.DigitalInputNumber).ToString().PadLeft(base.RegisteredDataFields[(int)DataFields.DIGITAL_INPUT_NUMBER].Size, '0');
        }

        public override MID processPackage(string package)
        {
            if (base.isCorrectType(package))
            {
                base.processHeader(package);
                var dataField = base.RegisteredDataFields[(int)DataFields.DIGITAL_INPUT_NUMBER];
                this.DigitalInputNumber = (DigitalInput.DigitalInputNumbers)Convert.ToInt32(package.Substring(dataField.Index, dataField.Size));
                return this;
            }


            return this.nextTemplate.processPackage(package);
        }

        protected override void registerDatafields()
        {
            this.RegisteredDataFields.Add(new DataField((int)DataFields.DIGITAL_INPUT_NUMBER, 20, 3));
        }

        public enum DataFields
        {
            DIGITAL_INPUT_NUMBER
        }
    }
}
