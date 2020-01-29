﻿using OpenProtocolInterpreter.Messages;
using System.Collections.Generic;

namespace OpenProtocolInterpreter.MIDs.Tightening
{
    internal class TighteningMessages : IMessagesTemplate
    {
        private readonly IMID templates;

        public TighteningMessages()
        {
            this.templates = new MID_0061(
                                    new MID_0065(
                                        new MID_0062(
                                            new MID_0064(
                                                new MID_0063(
                                                    new MID_0060(null))))));
        }

        public TighteningMessages(bool onlyController)
        {
            this.templates = (onlyController) ? this.initControllerTemplates() : this.initIntegratorTemplates();
        }

        public TighteningMessages(IEnumerable<MID> selectedMids)
        {
            this.templates = MessageTemplateFactory.buildChainOfMids(selectedMids);
        }

        public MID processPackage(string package)
        {
            return this.templates.processPackage(package);
        }

        private IMID initIntegratorTemplates()
        {
            return new MID_0062(new MID_0064(new MID_0063(new MID_0060(null))));
        }

        private IMID initControllerTemplates()
        {
            return new MID_0061(new MID_0065(null));
        }
    }
}
