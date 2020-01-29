﻿using OpenProtocolInterpreter.Converters;
using System;
using System.Collections.Generic;

namespace OpenProtocolInterpreter.IOInterface
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
    public class Mid0220 : Mid, IIOInterface, IIntegrator
    {
        private readonly IValueConverter<int> _intConverter;
        private const int LAST_REVISION = 1;
        public const int MID = 220;

        public DigitalInputNumber DigitalInputNumber
        {
            get => (DigitalInputNumber)GetField(1,(int)DataFields.DIGITAL_INPUT_NUMBER).GetValue(_intConverter.Convert);
            set => GetField(1,(int)DataFields.DIGITAL_INPUT_NUMBER).SetValue(_intConverter.Convert, (int)value);
        }

        public Mid0220() : this(0)
        {

        }

        public Mid0220(int? noAckFlag = 0) : base(MID, LAST_REVISION, noAckFlag)
        {
            _intConverter = new Int32Converter();
        }

        public Mid0220(DigitalInputNumber digitalInputNumber, int? noAckFlag = 0) : this(noAckFlag)
        {
            DigitalInputNumber = digitalInputNumber;
        }

        protected override Dictionary<int, List<DataField>> RegisterDatafields()
        {
            return new Dictionary<int, List<DataField>>()
            {
                {
                    1, new List<DataField>()
                    {
                        new DataField((int)DataFields.DIGITAL_INPUT_NUMBER, 20, 3, '0', DataField.PaddingOrientations.LEFT_PADDED, false)
                    }
                }
            };
        }

        public enum DataFields
        {
            DIGITAL_INPUT_NUMBER
        }
    }
}
