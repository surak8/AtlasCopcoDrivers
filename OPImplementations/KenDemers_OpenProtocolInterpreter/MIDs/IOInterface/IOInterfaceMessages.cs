﻿using System.Collections.Generic;
using OpenProtocolInterpreter.Messages;

namespace OpenProtocolInterpreter.MIDs.IOInterface
{
    internal class IOInterfaceMessages : IMessagesTemplate
    {
        private readonly IMID templates;

        public IOInterfaceMessages()
        {
            this.templates = new MID_0200(new MID_0210(new MID_0211(new MID_0212(new MID_0213( new MID_0214(
                             new MID_0215(new MID_0216(new MID_0217(new MID_0218( new MID_0219(new MID_0220(
                             new MID_0221(new MID_0222(new MID_0223(new MID_0224(new MID_0225(null)))))))))))))))));
        }

        public IOInterfaceMessages(IEnumerable<MID> selectedMids)
        {
            this.templates = MessageTemplateFactory.buildChainOfMids(selectedMids);
        }

        public MID processPackage(string package)
        {
            return this.templates.processPackage(package);
        }
    }
}
