using System.Diagnostics;
using System.Reflection;

namespace NSTcp_listener {
    static class Logger {
        //internal static void log(MethodBase mb) {
        //    log(mb,null);
        //}

        internal static void log(MethodBase mb, string msg = null) {
            string displayMsg = makeSignature(mb) +
               (string.IsNullOrEmpty(msg) ? string.Empty :
                (":" + msg.Replace('\0', '*')));
            //":"+string.IsInterned
            Trace.WriteLine(displayMsg);
        }

        static string makeSignature(MethodBase mb) {
            if (mb != null)
                return mb.ReflectedType.Name + "." + mb.Name;
            return string.Empty;
        }
        //trace
    }
}