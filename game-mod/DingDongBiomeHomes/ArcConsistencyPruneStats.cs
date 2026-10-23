using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;
public sealed record ArcConsistencyPruneStats<TVariable>(int QueuePops, int Revisions, int PairEvaluations, int ValuesRemoved, bool BudgetExhausted, bool Wipeout, TVariable BlockedVariable, TVariable SupportVariable, IReadOnlyDictionary<TVariable, int> ValuesRemovedByVariable);
