﻿using System.Collections.Generic;

namespace OpenProtocolInterpreter.Alarm
{

    /// <summary>
    /// MID: Alarm acknowledged on controller
    /// Description: 
    ///      The message is sent by the controller to inform the integrator that the current alarm has been
    ///      acknowledged.
    /// Message sent by: Controller
    /// Answer: MID 0075 Alarm acknowledged on controller acknowledge
    /// </summary>
    public class Mid0074 : Mid, IAlarm, IController
    {
        private const int LAST_REVISION = 2;
        public const int MID = 74;

        public string ErrorCode
        {
            get => GetField(1,(int)DataFields.ERROR_CODE).Value;
            set => GetField(1,(int)DataFields.ERROR_CODE).SetValue(value);
        }

        public Mid0074() : this(LAST_REVISION)
        {

        }

        public Mid0074(int revision = LAST_REVISION) : base(MID, revision) { }

        public Mid0074(string errorCode, int revision = LAST_REVISION) : this(revision)
        {
            ErrorCode = errorCode;
        }

        protected override Dictionary<int, List<DataField>> RegisterDatafields()
        {
            return new Dictionary<int, List<DataField>>()
            {
                {
                    1, new List<DataField>()
                            {
                                new DataField((int)DataFields.ERROR_CODE, 20, 4, ' ', DataField.PaddingOrientations.LEFT_PADDED, false)
                            }
                },
                {
                    2, new List<DataField>()
                    {
                        //None??
                    }
                }
            };
        }

        public enum DataFields
        {
            ERROR_CODE
        }
    }
}
