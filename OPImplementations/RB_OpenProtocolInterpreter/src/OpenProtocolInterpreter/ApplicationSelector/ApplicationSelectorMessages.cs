﻿using System;
using System.Collections.Generic;
using OpenProtocolInterpreter.Messages;

namespace OpenProtocolInterpreter.ApplicationSelector
{
    internal class ApplicationSelectorMessages : MessagesTemplate
    {
        public ApplicationSelectorMessages() : base()
        {
            _templates = new Dictionary<int, MidCompiledInstance>()
            {
                { Mid0250.MID, new MidCompiledInstance(typeof(Mid0250)) },
                { Mid0251.MID, new MidCompiledInstance(typeof(Mid0251)) },
                { Mid0252.MID, new MidCompiledInstance(typeof(Mid0252)) },
                { Mid0253.MID, new MidCompiledInstance(typeof(Mid0253)) },
                { Mid0254.MID, new MidCompiledInstance(typeof(Mid0254)) },
                { Mid0255.MID, new MidCompiledInstance(typeof(Mid0255)) }
            };
        }

        public ApplicationSelectorMessages(IEnumerable<Type> selectedMids) : this()
        {
            FilterSelectedMids(selectedMids);
        }

        public ApplicationSelectorMessages(InterpreterMode mode) : this()
        {
            FilterSelectedMids(mode);
        }

        public override bool IsAssignableTo(int mid) => mid > 249 && mid < 256;
    }
}
