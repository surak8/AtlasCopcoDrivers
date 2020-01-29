﻿using OpenProtocolInterpreter.Converters;
using System.Collections.Generic;

namespace OpenProtocolInterpreter.IOInterface
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
    public class Mid0219 : Mid, IIOInterface, IIntegrator
    {
        private readonly IValueConverter<int> _intConverter;
        private const int LAST_REVISION = 1;
        public const int MID = 219;

        public RelayNumber RelayNumber
        {
            get => (RelayNumber)GetField(1,(int)DataFields.RELAY_NUMBER).GetValue(_intConverter.Convert);
            set => GetField(1,(int)DataFields.RELAY_NUMBER).SetValue(_intConverter.Convert, (int)value);
        }

        public Mid0219() : base(MID, LAST_REVISION)
        {
            _intConverter = new Int32Converter();
        }

        /// <summary>
        /// Revision 1 Constructor
        /// </summary>
        /// <param name="relayNumber"></param>
        public Mid0219(RelayNumber relayNumber) : this()
        {
            RelayNumber = relayNumber;
        }

        protected override Dictionary<int, List<DataField>> RegisterDatafields()
        {
            return new Dictionary<int, List<DataField>>()
            {
                {
                    1, new List<DataField>()
                    {
                        new DataField((int)DataFields.RELAY_NUMBER, 20, 3, '0', DataField.PaddingOrientations.LEFT_PADDED, false)
                    }
                }
            };
        }

        public enum DataFields
        {
            RELAY_NUMBER
        }
    }
}
