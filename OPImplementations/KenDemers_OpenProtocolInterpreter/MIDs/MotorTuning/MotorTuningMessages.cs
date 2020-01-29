﻿using OpenProtocolInterpreter.Messages;

namespace OpenProtocolInterpreter.MIDs.MotorTuning
{
    internal class MotorTuningMessages : IMessagesTemplate
    {
        private readonly IMID templates;

        public MotorTuningMessages()
        {
            this.templates = new MID_0500(new MID_0501(new MID_0502(new MID_0503(new MID_0504(null)))));
        }

        public MotorTuningMessages(System.Collections.Generic.IEnumerable<MID> selectedMids)
        {
            this.templates = MessageTemplateFactory.buildChainOfMids(selectedMids);
        }

        public MID processPackage(string package)
        {
            return this.templates.processPackage(package);
        }
    }
}
