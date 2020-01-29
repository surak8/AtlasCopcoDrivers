﻿using OpenProtocolInterpreter.Messages;

namespace OpenProtocolInterpreter.MIDs.Tool
{
    internal class ToolMessages : IMessagesTemplate
    {
        private readonly IMID templates;

        public ToolMessages()
        {
            this.templates = new MID_0040(new MID_0041(new MID_0042(new MID_0043(new MID_0044(new MID_0045(new MID_0046(
                             new MID_0047(new MID_0048(null)))))))));
        }

        public ToolMessages(System.Collections.Generic.IEnumerable<MID> selectedMids)
        {
            this.templates = MessageTemplateFactory.buildChainOfMids(selectedMids);
        }

        public MID processPackage(string package)
        {
            return this.templates.processPackage(package);
        }
    }
}
