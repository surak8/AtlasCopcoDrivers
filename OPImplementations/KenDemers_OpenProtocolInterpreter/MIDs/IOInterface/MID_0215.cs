﻿using System.Collections.Generic;
using System.Linq;

namespace OpenProtocolInterpreter.MIDs.IOInterface
{
    /// <summary>
    /// MID: IO device status reply
    /// Description: 
    ///     This message is sent as an answer to the MID 0214 IO device status request.
    ///     MID 0215 revision 1 should only be used to get the status of IO devices with max 8 relays/digital
    ///     inputs.
    ///     For external I/O devices each list contain up to 8 relays/digital inputs. For the internal device the lists
    ///     contain up to 4 relays/digital inputs and the remaining 4 will be empty.
    ///     MID 0215 revision 2 can be used to get the status of all types of IO devices with up to 48 relays/digital
    ///     inputs.
    /// Message sent by: Controller
    /// Answer: None
    /// </summary>
    internal class MID_0215 : MID, IIOInterface
    {
        private const int length = 92;
        public const int MID = 215;
        private const int revision = 1;

        public int IODeviceID { get; set; }
        public List<Relay> RelayList { get; set; }
        public List<DigitalInput> DigitalInputList { get; set; }

        public MID_0215() : base(length, MID, revision)
        {
            this.RelayList = new List<Relay>();
            this.DigitalInputList = new List<DigitalInput>();
        }

        internal MID_0215(IMID nextTemplate) : base(length, MID, revision)
        {
            this.RelayList = new List<Relay>();
            this.DigitalInputList = new List<DigitalInput>();
            this.nextTemplate = nextTemplate;
        }

        public override string buildPackage()
        {
            string pkg = base.buildHeader();
            pkg += this.IODeviceID.ToString().PadLeft(base.RegisteredDataFields[(int)DataFields.IO_DEVICE_ID].Size, '0');
            RelayList.ForEach(x => pkg += x.buildPackage());
            DigitalInputList.ForEach(x => pkg += x.buildPackage());
            return pkg;
        }

        public override MID processPackage(string package)
        {
            if (base.isCorrectType(package))
            {
                base.processPackage(package);
                this.IODeviceID = base.RegisteredDataFields[(int)DataFields.IO_DEVICE_ID].ToInt32();
                this.RelayList = new Relay().getRelaysFromPackage(base.RegisteredDataFields[(int)DataFields.RELAY_LIST].Value.ToString()).ToList();
                this.DigitalInputList = new DigitalInput().getDigitalInputsFromPackage(base.RegisteredDataFields[(int)DataFields.DIGITAL_INPUT_LIST].Value.ToString()).ToList();
                return this;
            }

            return this.nextTemplate.processPackage(package);
        }

        protected override void registerDatafields()
        {
            this.RegisteredDataFields.AddRange(
                new DataField[]
                {
                    new DataField((int)DataFields.IO_DEVICE_ID, 20, 2),
                    new DataField((int)DataFields.RELAY_LIST, 24, 32),
                    new DataField((int)DataFields.DIGITAL_INPUT_LIST, 58, 32)
                });
        }

        public enum DataFields
        {
            IO_DEVICE_ID,
            RELAY_LIST,
            DIGITAL_INPUT_LIST
        }
    }
}
