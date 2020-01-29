﻿using OpenProtocolInterpreter.Converters;
using System;
using System.Collections.Generic;

namespace OpenProtocolInterpreter.Tool
{
    /// <summary>
    /// MID: Set calibration value request
    /// Description: 
    ///     This message is sent by the integrator in order to set the calibration value of the tool.
    /// Message sent by: Integrator
    /// Answer: MID 0005 Command accepted or MID 0004 Command error, Calibration failed
    /// </summary>
    public class Mid0045 : Mid, ITool, IIntegrator
    {
        private readonly IValueConverter<decimal> _decimalConverter;
        private readonly IValueConverter<int> _intConverter;
        private const int LAST_REVISION = 1;
        public const int MID = 45;

        public CalibrationUnit CalibrationValueUnit
        {
            get => (CalibrationUnit)GetField(1,(int)DataFields.CALIBRATION_VALUE_UNIT).GetValue(_intConverter.Convert);
            set => GetField(1,(int)DataFields.CALIBRATION_VALUE_UNIT).SetValue(_intConverter.Convert, (int)value);
        }
        public decimal CalibrationValue
        {
            get => GetField(1,(int)DataFields.CALIBRATION_VALUE).GetValue(_decimalConverter.Convert);
            set => GetField(1,(int)DataFields.CALIBRATION_VALUE).SetValue(_decimalConverter.Convert, value);
        }

        public Mid0045() : base(MID, LAST_REVISION)
        {
            _decimalConverter = new DecimalTrucatedConverter(2);
            _intConverter = new Int32Converter();
        }

        /// <summary>
        /// Revision 1 Constructor
        /// </summary>
        /// <param name="calibrationValueUnit">The unit in which the calibration value is sent. The calibration value unit is one byte long and specified by one ASCII digit.</param>
        /// <param name="calibrationValue">The calibration value is multiplied by 100 and sent as an integer (2 decimals truncated). The calibration value is six bytes long and is specified by six ASCII digits.</param>
        public Mid0045(CalibrationUnit calibrationValueUnit, decimal calibrationValue) : this()
        {
            CalibrationValueUnit = calibrationValueUnit;
            CalibrationValue = calibrationValue;
        }

        protected override Dictionary<int, List<DataField>> RegisterDatafields()
        {
            return new Dictionary<int, List<DataField>>()
            {
                {
                    1, new List<DataField>()
                            {
                                new DataField((int)DataFields.CALIBRATION_VALUE_UNIT, 20, 1),
                                new DataField((int)DataFields.CALIBRATION_VALUE, 23, 6, '0', DataField.PaddingOrientations.LEFT_PADDED)
                            }
                }
            };
        }

        public enum DataFields
        {
            CALIBRATION_VALUE_UNIT,
            CALIBRATION_VALUE
        }
    }
}
