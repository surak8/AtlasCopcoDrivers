﻿using System;

namespace OpenProtocolInterpreter.MIDs.ParameterSet
{
    /// <summary>
    /// MID: Parameter set selected
    /// Description: 
    ///     A new parameter set is selected in the controller. 
    ///     The message includes the ID of the parameter set selected as well as the date and time of the 
    ///     last change in the parameter set settings. This message is also sent as an immediate response to MID 0014 
    ///     Parameter set selected subscribe.
    /// Message sent by: Controller
    /// Answer: MID 0016 New parameter set selected acknowledge
    /// </summary>
    public class MID_0015 : MID, IParameterSet
    {
        private const int length = 42;
        public const int MID = 15;
        private const int revision = 1;

        public int ParameterSetID { get; set; }
        public DateTime LastChangeInParameterSet { get; set; }

        public MID_0015() : base(length, MID, revision) { }

        public MID_0015(int parameterSetId, DateTime lastChangeInParameterSet) : base(length, MID, revision)
        {
            this.ParameterSetID = parameterSetId;
            this.LastChangeInParameterSet = lastChangeInParameterSet;
        }

        internal MID_0015(IMID nextTemplate) : base(length, MID, revision)
        {
            this.nextTemplate = nextTemplate;
        }

        public override string buildPackage()
        {
            string package = base.buildPackage();
            package += this.ParameterSetID.ToString().PadLeft(this.RegisteredDataFields[(int)DataFields.PARAMETER_SET_ID].Size, '0');
            package += this.LastChangeInParameterSet.ToString("yyyy-MM-dd:HH:mm:ss");
            return package;
        }

        public override MID processPackage(string package)
        {
            if (base.isCorrectType(package))
            {
                this.HeaderData = base.processHeader(package);

                this.ParameterSetID = Convert.ToInt32(package.Substring(this.RegisteredDataFields[(int)DataFields.PARAMETER_SET_ID].Index,
                                                                        this.RegisteredDataFields[(int)DataFields.PARAMETER_SET_ID].Size));
                this.RegisteredDataFields[(int)DataFields.LAST_CHANGE_IN_PARAMETER_SET].Value = package.Substring(this.RegisteredDataFields[(int)DataFields.LAST_CHANGE_IN_PARAMETER_SET].Index, 
                                                                                                      this.RegisteredDataFields[(int)DataFields.LAST_CHANGE_IN_PARAMETER_SET].Size);
                this.LastChangeInParameterSet = this.RegisteredDataFields[(int)DataFields.LAST_CHANGE_IN_PARAMETER_SET].ToDateTime();

                return this;
            }

            return this.nextTemplate.processPackage(package);
        }

        protected override void registerDatafields()
        {
            this.RegisteredDataFields.Add(new DataField((int)DataFields.PARAMETER_SET_ID, 20, 3));
            this.RegisteredDataFields.Add(new DataField((int)DataFields.LAST_CHANGE_IN_PARAMETER_SET, 23, 19));
        }

        public enum DataFields
        {
            PARAMETER_SET_ID,
            LAST_CHANGE_IN_PARAMETER_SET
        }
    }
}
