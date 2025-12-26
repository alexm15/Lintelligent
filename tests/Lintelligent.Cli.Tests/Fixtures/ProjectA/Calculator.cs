namespace ProjectA;

public class Calculator
{
    // LNT001 violation: This method is intentionally long for testing purposes
    public int CalculateTotal(int a, int b, int c, int d, int e)
    {
        var result = 0;
        var multiplier = 1;
        var bonus = 0;

        if (a > 0)
        {
            result += a;
            multiplier += 1;
        }

        if (b > 0)
        {
            result += b;
            multiplier += 1;
        }

        if (c > 0)
        {
            result += c;
            multiplier += 1;
        }

        if (d > 0)
        {
            result += d;
            multiplier += 1;
        }

        if (e > 0)
        {
            result += e;
            multiplier += 1;
        }

        if (result > 50)
        {
            bonus = 10;
            result += bonus;
        }

        if (result > 100)
        {
            bonus = 20;
            result = 100;
            result += bonus;
        }

        if (result < 0)
        {
            result = 0;
            bonus = 0;
        }

        result *= multiplier;
        result += bonus;

        return result;
    }
}