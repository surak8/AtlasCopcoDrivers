﻿using System;

namespace OpenProtocolInterpreter.Converters
{
    internal class DateConverter : AsciiConverter<DateTime>
    {
        public override DateTime Convert(string value)
        {
            DateTime convertedValue = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(value.ToString()))
            {
                var date = value.ToString();
                DateTime.TryParse(date.Substring(0, 10) + " " + date.Substring(11, 8), out convertedValue);
            }

            return convertedValue;
        }

        public override string Convert(DateTime value) => value.ToString("yyyy-MM-dd:HH:mm:ss");

        public override string Convert(char paddingChar, int size, DataField.PaddingOrientations orientation, DateTime value) => Convert(value);
    }
}
