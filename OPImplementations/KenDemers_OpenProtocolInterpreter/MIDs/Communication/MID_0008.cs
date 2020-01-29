﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenProtocolInterpreter.MIDs.Communication
{
    /// <summary>
    /// MID: Application data message subscription
    /// Description: 
    ///     Start a subscription of data. This message is used for ALL subscription handling.
    ///     When used it substitutes the use of all MID special subscription messages.
    ///     NOTE! The Header Revision field is the revision of the MID 0008 itself NOT the revision of the data
    ///     MID that is wanted to be subscribed for.
    /// 
    /// Message sent by: Integrator
    /// Answer: MID 0005 Command accepted with the MID subscribed for or MID 0004 Command error, 
    ///         MID revision unsupported or Invalid data code and the MID subscribed for
    /// </summary>
    internal class MID_0008
    {
    }
}
