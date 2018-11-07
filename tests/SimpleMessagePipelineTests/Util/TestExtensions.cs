using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExt;

namespace SimpleMessagePipelineTests.Util
{
    public static class TestExtensions
    {
        public static Option<Tuple<T, IEnumerable<T>>> HeadAndTail<T>(this IEnumerable<T> l)
        {
            if (!l.Any())
            {
                return Option<Tuple<T, IEnumerable<T>>>.None;
            }
            return new Tuple<T, IEnumerable<T>>(
                l.First(),
                l.Skip(1).ToList());
        }
    }
}