﻿namespace OpenProtocolInterpreter.MIDs.Statistic
{
    /// <summary>
    /// MID: Histogram upload request
    /// Description: 
    ///    Request to upload a histogram from the controller for a certain parameter set.
    ///    The histogram is calculated with all the tightening results currently present in 
    ///    the controller’s memory and within the statistic acceptance window(statistic min and max limits) 
    ///    for the requested parameter set.
    /// Message sent by: Integrator
    /// Answer: MID 0301, Histogram upload reply, or 
    ///         MID 0004 Command error, No histogram available or Invalid data
    /// </summary>
    public class MID_0300 : MID, IStatistic
    {
        private const int length = 30;
        public const int MID = 300;
        private const int revision = 1;

        public int ParameterSetID { get; set; }
        public HistogramTypes HistogramType { get; set; }

        public MID_0300() : base(length, MID, revision) { }

        internal MID_0300(IMID nextTemplate) : base(length, MID, revision)
        {
            this.nextTemplate = nextTemplate;
        }

        public override string buildPackage()
        {
            base.RegisteredDataFields[(int)DataFields.PARAMETER_SET_ID].Value = this.ParameterSetID.ToString().PadLeft(base.RegisteredDataFields[(int)DataFields.PARAMETER_SET_ID].Size);
            base.RegisteredDataFields[(int)DataFields.HISTOGRAM_TYPE].Value = ((int)this.HistogramType).ToString().PadLeft(base.RegisteredDataFields[(int)DataFields.HISTOGRAM_TYPE].Size);
            return base.buildPackage();
        }

        public override MID processPackage(string package)
        {
            if (base.isCorrectType(package))
            {
                base.processPackage(package);
                this.ParameterSetID = base.RegisteredDataFields[(int)DataFields.PARAMETER_SET_ID].ToInt32();
                this.HistogramType = (HistogramTypes)base.RegisteredDataFields[(int)DataFields.HISTOGRAM_TYPE].ToInt32();
                return this;
            }

            return this.nextTemplate.processPackage(package);
        }

        protected override void registerDatafields()
        {
            this.RegisteredDataFields.AddRange(new DataField[] {
                new DataField((int)DataFields.PARAMETER_SET_ID, 20, 3),
                new DataField((int)DataFields.HISTOGRAM_TYPE, 25, 2)
            });
        }

        public enum HistogramTypes
        {
            TORQUE = 0,
            ANGLE = 1,
            CURRENT = 2,
            PREVAIL_TORQUE = 3,
            SELF_TAP = 4,
            RUNDOWN_ANGLE = 5
        }

        public enum DataFields
        {
            PARAMETER_SET_ID,
            HISTOGRAM_TYPE
        }
    }
}
