namespace DingDongBiomeHomes;

public static partial class DeterministicConstraintRepairSolver
{
	private static int PositiveModulo(int value, int modulus)
	{
		long positive = value;
		if (positive < 0)
			positive = -positive;
		return (int)(positive % modulus);
	}
}
