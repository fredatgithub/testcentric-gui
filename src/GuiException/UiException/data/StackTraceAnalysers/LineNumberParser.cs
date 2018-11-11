﻿// ***********************************************************************
// Copyright (c) 2018 Charlie Poole
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using NUnit.UiException.StackTraceAnalyzers;

namespace NUnit.UiException.StackTraceAnalysers
{
    /// <summary>
    /// LineNumberParser tries to match a line number information
    /// given in a stack trace line. It relies on the following
    /// assumptions:
    ///     - the line number is located after the last ':' character,
    ///     - the line number is not part of a word,
    ///     - there cannot be more than one line number after the last ':'
    ///     - the line number must be > 0
    /// </summary>
    public class LineNumberParser :
        IErrorParser
    {
        #region IErrorParser Membres

        /// <summary>
        /// Reads args.Input and try to locate a line number information.
        /// If a match occurs the method fills args.Line with the identified
        /// integer.
        /// </summary>
        /// <param name="parser">The StackTraceParser instance. The
        /// parameter cannot be null.</param>
        /// <param name="args">The RawError instance from where read and
        /// write Input and Line properties. The parameter cannot be null.</param>
        /// <returns>True if a match occurs, false otherwise.</returns>
        public bool TryParse(StackTraceParser parser, RawError args)
        {
            int posTrailingColon;
            int line;

            UiExceptionHelper.CheckNotNull(parser, "parser");
            UiExceptionHelper.CheckNotNull(args, "args");

            if ((posTrailingColon = args.Input.LastIndexOf(":")) == -1)
                return (false);

            if ((line = lookForLastInteger(args.Input, posTrailingColon)) <= 0)
                return (false);

            args.Line = line;

            return (true);
        }

        #endregion

        private int lookForLastInteger(string error, int startIndex)
        {
            int res;
            int workPos;

            for (; startIndex < error.Length; ++startIndex)
                if (error[startIndex] >= '0' && error[startIndex] <= '9')
                {
                    // the integer we are looking for
                    // is following either a ' ' or ':' character.
                    // ex: "line 42" or ":42"
                    // but rejects values like: "line42"

                    if (startIndex > 0 &&
                        error[startIndex - 1] != ' ' &&
                        error[startIndex - 1] != ':')
                        return (-1);

                    break;
                }

            if (startIndex >= error.Length)
                return (-1);

            // it is assumes that only one line number information will be
            // present on a line (if any). If two integers are found (should
            // never happen) we cannot distinguish which one refers to the
            // line number. In this case we prefer returning -1.
            // The next loop checks this point by moving the cursor to a
            // place after the current number and expects the inner call
            // to return -1

            for (workPos = startIndex; workPos < error.Length; ++workPos)
                if (error[workPos] < '0' || error[workPos] > '9')
                    break;
            if (lookForLastInteger(error, workPos) != -1)
                return (-1);

            Int32.TryParse(error.Substring(startIndex, workPos - startIndex), out res);

            return (res);
        }
    }
}
