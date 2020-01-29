﻿using System;

namespace OpenProtocolInterpreter.MIDs.IOInterface
{
    /// <summary>
    /// MID: Set digital input function
    /// Description: 
    ///     Set the digital input function with the digital input number. 
    ///     The digital input function numbers are defined in Table 80.
    /// Message sent by: Integrator
    /// Answer: MID 0005 Command accepted or
    ///         MID 0004 Command error, Invalid data
    /// </summary>
    public class MID_0224 : MID, IIOInterface
    {
        public const int MID = 224;
        private const int length = 23;
        private const int revision = 1;

        public DigitalInput.DigitalInputNumbers DigitalInputNumber { get; set; }

        public MID_0224() : base(length, MID, revision) { }

        internal MID_0224(IMID nextTemplate) : base(length, MID, revision)
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
