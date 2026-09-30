using System;
using System.Collections.Generic;

namespace F1.Data
{
    /// <summary>A single static data value or definition broke a rule.</summary>
    public sealed class DataException : Exception
    {
        public DataException(string message)
            : base(message)
        {
        }
    }

    /// <summary>Static data as a whole is invalid. Carries every problem that was found.</summary>
    public sealed class DataValidationException : Exception
    {
        public DataValidationException(IReadOnlyList<string> problems)
            : base("Static data is invalid:\n" + string.Join("\n", problems))
        {
            Problems = problems;
        }

        public IReadOnlyList<string> Problems { get; }
    }
}
