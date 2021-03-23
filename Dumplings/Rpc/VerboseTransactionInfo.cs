using System;
using System.Collections.Generic;
using System.Linq;
using Dumplings.Helpers;
using NBitcoin;

namespace Dumplings.Rpc
{
    public class VerboseTransactionInfo
    {
        public VerboseTransactionInfo(TransactionBlockInfo blockInfo, uint256 id, IEnumerable<VerboseInputInfo> inputs, IEnumerable<VerboseOutputInfo> outputs)
        {
            Id = id;
            BlockInfo = blockInfo;
            Inputs = inputs;
            Outputs = outputs;
        }

        public uint256 Id { get; }
        public TransactionBlockInfo BlockInfo { get; }
        public IEnumerable<VerboseInputInfo> Inputs { get; }

        public IEnumerable<VerboseOutputInfo> Outputs { get; }

        public IEnumerable<(Money value, int count)> GetIndistinguishableOutputs(bool includeSingle)
        {
            return Outputs.GroupBy(x => x.Value)
                .ToDictionary(x => x.Key, y => y.Count())
                .Select(x => (x.Key, x.Value))
                .Where(x => includeSingle || x.Value > 1);
        }
        public Money NetworkFee => Inputs.Sum(x => x.PrevOutput.Value) - Outputs.Sum(x => x.Value);

        public ulong CalculateCoinJoinEquality() => CalculateEquality(Inputs.Select(x => x.PrevOutput.Value)) + CalculateEquality(Outputs.Select(x => x.Value));

        private ulong CalculateEquality(IEnumerable<Money> values)
        {
            var buckets = new Dictionary<Money, int>();
            foreach (var val in values.OrderByDescending(x => x))
            {
                var found = buckets.FirstOrDefault(x => x.Key.Almost(val, Money.Satoshis(10000))).Key;
                if (found is { })
                {
                    buckets[found]++;
                }
                else
                {
                    buckets.Add(val, 1);
                }
            }

            // Only mixed coins count.
            var nonSingleBuckets = buckets.Where(x => x.Value > 1).ToArray();
            ulong equality = 0;
            foreach (var bucket in nonSingleBuckets)
            {
                // x people mixed with x - 1 people x bitcoins
                decimal bitcoinValue = bucket.Key.ToDecimal(MoneyUnit.BTC);
                equality += (ulong)(bucket.Value * bucket.Value * bitcoinValue);
            }

            return equality;
        }
    }
}
