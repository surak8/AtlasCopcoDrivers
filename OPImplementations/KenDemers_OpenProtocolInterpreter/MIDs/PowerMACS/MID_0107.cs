﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenProtocolInterpreter.MIDs.PowerMACS
{
    /// <summary>
    /// MID: Last Power MACS tightening result Bolt data
    /// Description: 
    ///    This message contains the cycle data for one Bolt, both Bolt data and step data. It is only sent if 
    ///    the acknowledgement of the message MID 0106 Last PowerMACS tightening result station data had the parameter 
    ///    Bolt Data set to TRUE. The next Bolt data is sent if the acknowledgement has the parameter Bolt Data set to TRUE.
    ///    This telegram is also used for Power MACS systems running a Press.The layout of the telegram is exactly the 
    ///    same but some of the fields have slightly different definitions. The fields for Torque are used for Force values 
    ///    and the fields for Angle are used for Stroke values. Press systems also use different identifiers for the optional 
    ///    data on bolt and step level. Press systems always use revision 4 or higher of the telegram.Values in the fixed part
    ///    that are undefined in the results will be sent as all spaces (ASCII 0x20). 
    ///    This can happen with the Customer Error Code if this function is not activated.
    ///    Note 2: The Bolt results and step results are only sent when the value exists in the result. This means,
    ///    for example, that if no high limit is programmed for Peak T, then the value Peak T + will not be sent
    ///    even if limits for Peak T are defined in the reporter.
    /// Message sent by: Controller
    /// Answer: MID 0108 Last PowerMACS tightening result data acknowledge
    /// </summary>
    public class MID_0107 : MID, IPowerMACS
    {
        public const int MID = 107;
        private const int length = 9999;
        private const int revision = 1;

        public int TotalNumberOfMessages { get; set; }
        public int MessageNumber { get; set; }
        public int DataNumberSystem { get; set; }
        public int StationNumber { get; set; }
        public DateTime Time { get; set; }
        public int BoltNumber { get; set; }
        public string BoltName { get; set; }
        public string ProgramName { get; set; }
        public PowerMacsStatuses PMStatus { get; set; }
        public string Errors { get; set; }
        public string CustomerErrorCode { get; set; }
        public List<BoltResult> BoltResults { get; set; }
        public bool AllStepDataSent { get; set; }
        public List<StepResult> StepResults { get; set; }
        public List<SpecialValue> SpecialValues { get; set; }

        public MID_0107() : base(length, MID, revision)
        {
            this.BoltResults = new List<BoltResult>();
            this.StepResults = new List<StepResult>();
            this.SpecialValues = new List<SpecialValue>();
        }

        internal MID_0107(IMID nextTemplate) : base(length, MID, revision)
        {
            this.BoltResults = new List<BoltResult>();
            this.StepResults = new List<StepResult>();
            this.SpecialValues = new List<SpecialValue>();
            this.nextTemplate = nextTemplate;
        }

        public override string buildPackage()
        {
            //TODO
            return base.buildPackage();
        }

        public override MID processPackage(string package)
        {
            if (base.isCorrectType(package))
            {
                base.HeaderData.Length = package.Length;
                base.processPackage(package);

                this.TotalNumberOfMessages = base.RegisteredDataFields[(int)DataFields.TOTAL_NUMBER_OF_MESSAGES].ToInt32();
                this.MessageNumber = base.RegisteredDataFields[(int)DataFields.MESSAGE_NUMBER].ToInt32();
                this.DataNumberSystem = base.RegisteredDataFields[(int)DataFields.DATA_NUMBER_SYSTEM].ToInt32();
                this.StationNumber = base.RegisteredDataFields[(int)DataFields.STATION_NUMBER].ToInt32();
                this.Time = base.RegisteredDataFields[(int)DataFields.TIME].ToDateTime();
                this.BoltNumber = base.RegisteredDataFields[(int)DataFields.BOLT_NUMBER].ToInt32();
                this.BoltName = base.RegisteredDataFields[(int)DataFields.BOLT_NAME].Value.ToString();
                this.ProgramName = base.RegisteredDataFields[(int)DataFields.PROGRAM_NAME].Value.ToString();
                this.PMStatus = (PowerMacsStatuses)base.RegisteredDataFields[(int)DataFields.PM_STATUS].ToInt32();
                this.Errors = base.RegisteredDataFields[(int)DataFields.ERRORS].Value.ToString();
                this.CustomerErrorCode = base.RegisteredDataFields[(int)DataFields.CUSTOMER_ERROR_CODE].Value.ToString();

                //BoltResults full size
                int totalBoltResults = base.RegisteredDataFields[(int)DataFields.NUMBER_OF_BOLT_RESULTS].ToInt32();
                base.RegisteredDataFields[(int)DataFields.NUMBER_OF_BOLT_RESULTS].Size = totalBoltResults * 29; 
                this.BoltResults = new BoltResult().getBoltResultsFromPackage(package.Substring(base.RegisteredDataFields[(int)DataFields.NUMBER_OF_BOLT_RESULTS].Index, base.RegisteredDataFields[(int)DataFields.NUMBER_OF_BOLT_RESULTS].Size)).ToList();

                //Step Results full size (index + size)
                this.AllStepDataSent = Convert.ToBoolean(Convert.ToInt32(package.Substring(base.RegisteredDataFields[(int)DataFields.NUMBER_OF_STEP_RESULTS].Index + 7, 1)));
                base.RegisteredDataFields[(int)DataFields.NUMBER_OF_STEP_RESULTS].Index = base.RegisteredDataFields[(int)DataFields.NUMBER_OF_BOLT_RESULTS].Index + base.RegisteredDataFields[(int)DataFields.NUMBER_OF_BOLT_RESULTS].Size;
                base.RegisteredDataFields[(int)DataFields.NUMBER_OF_STEP_RESULTS].Size = Convert.ToInt32(package.Substring(base.RegisteredDataFields[(int)DataFields.NUMBER_OF_STEP_RESULTS].Index, 3)) * 29;
                this.StepResults = new StepResult().getStepResultsFromPackage(package.Substring(base.RegisteredDataFields[(int)DataFields.NUMBER_OF_STEP_RESULTS].Index, base.RegisteredDataFields[(int)DataFields.NUMBER_OF_STEP_RESULTS].Size)).ToList();

                //Special Values full size (index + size)
                base.RegisteredDataFields[(int)DataFields.NUMBER_OF_SPECIAL_VALUES].Index = base.RegisteredDataFields[(int)DataFields.NUMBER_OF_STEP_RESULTS].Index + base.RegisteredDataFields[(int)DataFields.NUMBER_OF_STEP_RESULTS].Size;
                base.RegisteredDataFields[(int)DataFields.NUMBER_OF_STEP_RESULTS].Size = package.Length - Convert.ToInt32(package.Substring(base.RegisteredDataFields[(int)DataFields.NUMBER_OF_STEP_RESULTS].Index, 2));
                this.SpecialValues = new SpecialValue().getSpecialValuesFromPackage(package.Substring(base.RegisteredDataFields[(int)DataFields.NUMBER_OF_STEP_RESULTS].Index)).ToList();

                return this;
            }

            return this.nextTemplate.processPackage(package);
        }

        protected override void registerDatafields()
        {
            this.RegisteredDataFields.AddRange(
                new DataField[]
                {
                            new DataField((int)DataFields.TOTAL_NUMBER_OF_MESSAGES, 20, 2),
                            new DataField((int)DataFields.MESSAGE_NUMBER, 24, 2),
                            new DataField((int)DataFields.DATA_NUMBER_SYSTEM,28, 10),
                            new DataField((int)DataFields.STATION_NUMBER, 40, 2),
                            new DataField((int)DataFields.TIME, 44, 19),
                            new DataField((int)DataFields.BOLT_NUMBER, 55, 4),
                            new DataField((int)DataFields.BOLT_NAME, 61, 20),
                            new DataField((int)DataFields.PROGRAM_NAME, 83, 20),
                            new DataField((int)DataFields.PM_STATUS, 105, 1),
                            new DataField((int)DataFields.ERRORS, 108, 50),
                            new DataField((int)DataFields.CUSTOMER_ERROR_CODE, 160, 4),
                            new DataField((int)DataFields.NUMBER_OF_BOLT_RESULTS, 166, 2),
                            new DataField((int)DataFields.NUMBER_OF_STEP_RESULTS, 0, 0),
                            new DataField((int)DataFields.NUMBER_OF_SPECIAL_VALUES, 0, 0),
                });
        }

        public enum DataFields
        {
            TOTAL_NUMBER_OF_MESSAGES,
            MESSAGE_NUMBER,
            DATA_NUMBER_SYSTEM,
            STATION_NUMBER,
            TIME,
            BOLT_NUMBER,
            BOLT_NAME,
            PROGRAM_NAME,
            PM_STATUS,
            ERRORS,
            CUSTOMER_ERROR_CODE,
            NUMBER_OF_BOLT_RESULTS,
            NUMBER_OF_STEP_RESULTS,
            NUMBER_OF_SPECIAL_VALUES
        }

        public enum PowerMacsStatuses
        {
            OK = 0,
            OKR = 1,
            NOK = 2,
            TERMNOK = 3
        }

        public class BoltResult
        {
            private List<DataField> fields;
            public string VariableName { get; set; }
            public DataType Type { get; set; }
            public object Value { get; set; }

            public BoltResult()
            {
                this.fields = new List<DataField>();
                this.registerDatafields();
            }

            public IEnumerable<BoltResult> getBoltResultsFromPackage(string package)
            {
                List<BoltResult> obj = new List<BoltResult>();

                var totalBoltResults = Convert.ToInt32(package.Substring(2, 2));
                for (int i = 0; i < totalBoltResults; i++)
                    obj.Add(this.getBoltFromPackage(package.Substring(2 + (i * 29), 29)));

                return obj;
            }

            private BoltResult getBoltFromPackage(string package)
            {
                BoltResult result = new BoltResult();

                foreach (DataField field in this.fields)
                    field.Value = package.Substring(field.Index, field.Size);

                result.VariableName = this.fields[(int)DataFields.VARIABLE_NAME].Value.ToString();
                result.Type = DataType.DataTypes.SingleOrDefault(x => x.Type == this.fields[(int)DataFields.TYPE].Value.ToString().Trim());
                result.Value = (result.Type.Type == "I") ? this.fields[(int)DataFields.VALUE].ToInt32() : this.fields[(int)DataFields.VALUE].ToFloat();

                return result;
            }

            private void registerDatafields()
            {
                this.fields.AddRange(
                    new DataField[]
                    {
                            new DataField((int)DataFields.VARIABLE_NAME, 0, 20),
                            new DataField((int)DataFields.TYPE, 20, 2),
                            new DataField((int)DataFields.VALUE, 22, 7)
                    });
            }

            public enum DataFields
            {
                VARIABLE_NAME,
                TYPE,
                VALUE
            }
        }

        public class StepResult
        {
            private List<DataField> fields;
            public string VariableName { get; set; }
            public DataType Type { get; set; }
            public object Value { get; set; }
            public int StepNumber { get; set; }

            public StepResult()
            {
                this.fields = new List<DataField>();
                this.registerDatafields();
            }

            public IEnumerable<StepResult> getStepResultsFromPackage(string package)
            {
                List<StepResult> obj = new List<StepResult>();
                var totalStepResults = Convert.ToInt32(package.Substring(2, 3));
                for (int i = 0; i < totalStepResults; i++)
                    obj.Add(this.getStepResultFromPackage(package.Substring(8 + (i * 31), 31)));

                return obj;
            }

            private StepResult getStepResultFromPackage(string package)
            {
                StepResult result = new StepResult();

                foreach (DataField field in this.fields)
                    field.Value = package.Substring(field.Index, field.Size);

                result.VariableName = this.fields[(int)DataFields.VARIABLE_NAME].Value.ToString();
                result.Type = DataType.DataTypes.SingleOrDefault(x => x.Type == this.fields[(int)DataFields.TYPE].Value.ToString().Trim());
                result.Value = (result.Type.Type == "I") ? this.fields[(int)DataFields.VALUE].ToInt32() : this.fields[(int)DataFields.VALUE].ToFloat();
                result.StepNumber = this.fields[(int)DataFields.STEP_NUMBER].ToInt32();

                return result;
            }

            private void registerDatafields()
            {
                this.fields.AddRange(
                    new DataField[]
                    {
                            new DataField((int)DataFields.VARIABLE_NAME, 0, 20),
                            new DataField((int)DataFields.TYPE, 20, 2),
                            new DataField((int)DataFields.VALUE, 22, 7),
                            new DataField((int)DataFields.VALUE, 29, 2),
                    });
            }

            public enum DataFields
            {
                VARIABLE_NAME,
                TYPE,
                VALUE,
                STEP_NUMBER
            }
        }

        public class SpecialValue
        {
            private List<DataField> fields;
            public string VariableName { get; set; }
            public DataType Type { get; set; }
            public int Length { get; set; }
            public object Value { get; set; }
            public int StepNumber { get; set; }

            public SpecialValue()
            {
                this.fields = new List<DataField>();
                this.registerDatafields();
            }

            public IEnumerable<SpecialValue> getSpecialValuesFromPackage(string package)
            {
                List<SpecialValue> obj = new List<SpecialValue>();
                int numberOfSpecialValues = Convert.ToInt32(package.Substring(2, 2));

                int index = 4;
                for (int i = 0; i < numberOfSpecialValues; i++)
                {
                    int valueLength = Convert.ToInt32(package.Substring(index + this.fields[(int)DataFields.LENGTH].Index, this.fields[(int)DataFields.LENGTH].Size));
                    int totalSpecialValueLength = this.fields[(int)DataFields.LENGTH].Index + this.fields[(int)DataFields.LENGTH].Size + valueLength;

                    obj.Add(this.getSpecialValue(package.Substring(index, totalSpecialValueLength)));
                    index += totalSpecialValueLength;
                }

                return obj;
            }

            private SpecialValue getSpecialValue(string package)
            {
                SpecialValue val = new SpecialValue();

                val.VariableName = package.Substring(this.fields[(int)DataFields.VARIABLE_NAME].Index, this.fields[(int)DataFields.VARIABLE_NAME].Size);
                val.Type = DataType.DataTypes.SingleOrDefault(x => x.Type == package.Substring(this.fields[(int)DataFields.TYPE].Index, this.fields[(int)DataFields.TYPE].Size).Trim());
                val.Length = Convert.ToInt32(package.Substring(this.fields[(int)DataFields.LENGTH].Index, this.fields[(int)DataFields.LENGTH].Size));
                val.Value = package.Substring(this.fields[(int)DataFields.VALUE].Index, val.Length);
                val.StepNumber = Convert.ToInt32(package.Substring(this.fields[(int)DataFields.VALUE].Index + val.Length, this.fields[(int)DataFields.STEP_NUMBER].Size));

                return val;
            }

            private void registerDatafields()
            {
                this.fields.AddRange(
                    new DataField[]
                    {
                            new DataField((int)DataFields.VARIABLE_NAME, 0, 20),
                            new DataField((int)DataFields.TYPE, 20, 2),
                            new DataField((int)DataFields.LENGTH, 22, 2),
                            new DataField((int)DataFields.VALUE, 24, 0),
                            new DataField((int)DataFields.STEP_NUMBER, 0, 2),
                    });
            }

            public enum DataFields
            {
                VARIABLE_NAME,
                TYPE,
                LENGTH,
                VALUE,
                STEP_NUMBER
            }
        }
    }
}
