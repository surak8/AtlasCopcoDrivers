﻿using System;

namespace OpenProtocolInterpreter.MIDs.ParameterSet
{
    /// <summary>
    /// MID: Parameter set data upload request
    /// Description: 
    ///     Request to upload parameter set data from the controller.
    /// Message sent by: Integrator
    /// Answer: MID 0013 Parameter set data upload reply, or MID 0004 Command error, Parameter set not present
    /// </summary>
    public class MID_0012 : MID, IParameterSet
    {
        private const int length = 23;
        public const int MID = 12;
        private const int revision = 1;

        public int ParameterSetID { get; set; }

        public MID_0012() : base(length, MID, revision) { }

        public MID_0012(int parameterSetId) : base(length, MID, revision)
        {
            this.ParameterSetID = parameterSetId;
        }

        internal MID_0012(IMID nextTemplate) : base(length, MID, revision)
        {
            this.nextTemplate = nextTemplate;
        }

        public override string buildPackage()
        {
            return base.buildHeader() + this.ParameterSetID.ToString().PadLeft(this.RegisteredDataFields[(int)DataFields.PARAMETER_SET_ID].Size, '0') ;
        }

        public override MID processPackage(string package)
        {
            if (base.isCorrectType(package))
            {
                this.HeaderData = base.processHeader(package);

                var datafield = this.RegisteredDataFields[(int)DataFields.PARAMETER_SET_ID];
                this.ParameterSetID = Convert.ToInt32(package.Substring(datafield.Index, datafield.Size));

                return this;
            }
                
            return this.nextTemplate.processPackage(package);
        }

        protected override void registerDatafields() 
        {
            this.RegisteredDataFields.Add(new DataField((int)DataFields.PARAMETER_SET_ID, 20, 3));
        }

        public enum DataFields
        {
            PARAMETER_SET_ID
        }
    }
}
