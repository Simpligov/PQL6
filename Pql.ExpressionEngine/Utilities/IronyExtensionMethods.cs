﻿using System.Reflection;

using Irony.Parsing;

namespace Pql.ExpressionEngine.Utilities
{
    public static class IronyExtensionMethods
    {
        private static readonly MethodInfo s_method;

        static IronyExtensionMethods()
        {
            var t = typeof(Parser);
            var method = t.GetMethod("Reset", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method is null)
            {
                throw new Exception("Could not resolve internal method Reset on " + t.FullName);
            }

            s_method = method;
        }

        public static void Reset(this Parser parser) => s_method.Invoke(parser, null);
    }
}