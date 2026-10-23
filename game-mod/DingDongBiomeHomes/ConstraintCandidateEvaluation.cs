using System;
using System.Collections.Generic;

namespace DingDongBiomeHomes;
public readonly record struct ConstraintCandidateEvaluation(bool Feasible, int Score)
{
	public static ConstraintCandidateEvaluation Reject => new(false, int.MinValue);

	public static ConstraintCandidateEvaluation Accept(int score) => new(true, score);
}
