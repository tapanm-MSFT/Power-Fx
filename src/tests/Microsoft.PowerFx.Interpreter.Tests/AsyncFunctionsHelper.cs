// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.PowerFx.Core;
using Microsoft.PowerFx.Core.Functions;
using Microsoft.PowerFx.Core.Localization;
using Microsoft.PowerFx.Core.Public;
using Microsoft.PowerFx.Core.Public.Types;
using Microsoft.PowerFx.Core.Public.Values;
using Microsoft.PowerFx.Core.Tests;
using Microsoft.PowerFx.Core.Texl;
using Microsoft.PowerFx.Core.Types;
using Microsoft.PowerFx.Core.Utils;
using Xunit;
using static Microsoft.PowerFx.Core.Localization.TexlStrings;

namespace Microsoft.PowerFx.Tests
{
    // Async(N) returns N. Does not complete until Async(N-1) completes. 
    // Async(0) is completed.
    internal class AsyncFunctionsHelper
    {
        private readonly List<TaskCompletionSource<int>> _list = new List<TaskCompletionSource<int>>();

        private TaskCompletionSource<int> Get(int i)
        {
            lock (_list)
            {
                while (_list.Count <= i)
                {
                    _list.Append(new TaskCompletionSource<int>());
                }
            }

            return _list[i];
        }

        private async Task WaitFor(int i)
        {
            var tsc = Get(i);
            await tsc.Task;
        }

        private void Complete(int i)
        {
            var tsc = Get(i);
            tsc.SetResult(i);
        }

        public async Task<FormulaValue> Worker(FormulaValue[] args, CancellationToken cancel)
        {
            var i = (int)((NumberValue)args[0]).Value;

            // all previous instances must have finished. 
            for (var x = 0; x < i; x++)
            {
                await WaitFor(x);
            }

            Complete(i);

            return args[0];
        }

        public TexlFunction GetFunction()
        {
            return new CustomAsyncTexlFunction("Async", DType.Number, DType.Number)
            {
                _impl = Worker
            };
        }
    }
}
