﻿using OpenProtocolInterpreter.Messages;

namespace OpenProtocolInterpreter.MIDs.PLCUserData
{
    internal class PLCUserDataMessages : IMessagesTemplate
    {
        private readonly IMID templates;

        public PLCUserDataMessages()
        {
            this.templates = new MID_0240(new MID_0241( new MID_0242( new MID_0243(new MID_0244(new MID_0245(null))))));
        }

        public PLCUserDataMessages(System.Collections.Generic.IEnumerable<MID> selectedMids)
        {
            this.templates = MessageTemplateFactory.buildChainOfMids(selectedMids);
        }

        public MID processPackage(string package)
        {
            return this.templates.processPackage(package);
        }
    }
}
