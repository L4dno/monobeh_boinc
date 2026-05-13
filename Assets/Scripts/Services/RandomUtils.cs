using UnityEngine;

public enum Distribution
{
    Weibull,
    Gamma,
    Lognormal,
    Normal,
    Hyperx,
    Exponential,
    One,
    Zero,
    Uniform
}

public static class RandomUtils
{

    public static float GetDistributionMean(Distribution distribution, float a, float b)
    {
        switch (distribution)
        {
            case Distribution.Weibull:
                if (a <= 0)
                {
                    return 0;
                }
                return b * GammaFunction(1f + 1f / a);
            case Distribution.Gamma:
                return a * b;
            case Distribution.Lognormal:
                return Mathf.Exp(a + b * b / 2f);
            case Distribution.Normal:
                return a;
            case Distribution.Hyperx:
                return a;
            case Distribution.Exponential:
                if (a <= 0)
                {
                    return 0;
                }
                return 1f / a;
            case Distribution.One:
                return 1f;
            case Distribution.Zero:
                return 0;
            case Distribution.Uniform:
                return (a + b) / 2f;
            default:
                return 0;
        }
    }


    public static float GetClampedDistributionMean(Distribution distribution, float a, float b, float minSpeed, float maxSpeed)
    {
        float mean = GetDistributionMean(distribution, a, b);

        if (maxSpeed < minSpeed)
        {
            float aux = minSpeed;
            minSpeed = maxSpeed;
            maxSpeed = aux;
        }

        if (distribution == Distribution.Exponential && a > 0)
        {
            float lower = Mathf.Max(minSpeed, 0);
            float upper = Mathf.Max(maxSpeed, lower);
            float pLow = 1f - Mathf.Exp(-a * lower);
            float pHigh = Mathf.Exp(-a * upper);
            float middle = (lower + 1f / a) * Mathf.Exp(-a * lower) - (upper + 1f / a) * Mathf.Exp(-a * upper);
            return lower * pLow + middle + upper * pHigh;
        }

        if (mean < minSpeed)
        {
            return minSpeed;
        }
        if (mean > maxSpeed)
        {
            return maxSpeed;
        }
        return mean;
    }

    private static float GammaFunction(float z)
    {
        if (z <= 0)
        {
            return 0;
        }

        float z2 = z * z;
        float z3 = z2 * z;
        float correction = 1f + 1f / (12f * z) + 1f / (288f * z2) - 139f / (51840f * z3);
        return Mathf.Sqrt(2f * Mathf.PI / z) * Mathf.Pow(z / Mathf.Exp(1f), z) * correction;
    }

    private static bool _isInitialized = false;

    public static void SetSeed(int seed)
    {
        if (_isInitialized)
            throw new System.InvalidOperationException("Trying to reset current seed");

        ResetSeed(seed);
    }

    public static void ResetSeed(int seed)
    {
        UnityEngine.Random.InitState(seed);
        z2 = 0.0f;
        _isInitialized = true;
    }

    public static int GetRandomIndex(int length)
    {
        if (length <= 0)
            throw new System.ArgumentOutOfRangeException(nameof(length));

        return UnityEngine.Random.Range(0, length);
    }

    private static float Uniform()
    {
        return UnityEngine.Random.value;
    }

    private static float UniformPositive()
    {
        float g;
        do
        {
            g = Uniform();
        }
        while (g == 0.0f);

        return g;
    }

    private static float UniformMinMax(float min, float max)
    {
        if (min > max)
            throw new System.ArgumentException("Min is greater than max");

        return min + (max - min) * Uniform();
    }

    private static int UniformInt(int min, int max)
    {
        if (min > max)
            throw new System.ArgumentException("Min is greater than max");
        
        return UnityEngine.Random.Range(min, max + 1);
    }

    private static float Exponential(float landa)
    {
        float u = UniformPositive();
        float mean = 1.0f / landa;

        return -mean * Mathf.Log(u);
    } 

    private static float Erlang(float x, float s)
    {
        int i, k;
        float z;
        if (s > x)
            throw new System.ArgumentException("erlang Argument Error: s > x");
        
        z = x / s;
        k = (int)z * (int)z;
        z = 1.0f;
        
        for (i = 0; i < k; i++)
            z *= UniformPositive();
        
        return -(x / k) * Mathf.Log(z);
    }

    private static float Hyperx(float x, float s)
    {
        float cv, z, p;
        if (s <= x)
            throw new System.ArgumentException("hyperx Argument Error: s not > x");
        
        cv = s / x;
        z = cv * cv;
        p = 0.5f * (1.0f - Mathf.Sqrt((z - 1.0f) / (z + 1.0f)));
        z = (UniformPositive() > p) ? (x / (1.0f - p)) : (x / p);
        
        return -0.5f * z * Mathf.Log(UniformPositive());
    }

    private static float z2 = 0.0f;
    
    private static float Normal(float x, float s)
    {
        float v1, v2, w, z1;
        
        if (z2 != 0.0f)
        {
            z1 = z2;
            z2 = 0.0f;
        }
        else
        {
            do
            {
                v1 = 2.0f * UniformPositive() - 1.0f;
                v2 = 2.0f * UniformPositive() - 1.0f;
                w = v1 * v1 + v2 * v2;
            }
            while (w >= 1.0f);
            
            w = Mathf.Sqrt((-2.0f * Mathf.Log(w)) / w);
            z1 = v1 * w;
            z2 = v2 * w;
        }
        
        return x + z1 * s;
    }

    private static float Lognormal(float p, float u)
    {
        return Mathf.Exp(Normal(p, u));
    }

    private static float Weibull(float k, float a)
    {
        float x = UniformPositive();
        float z = Mathf.Pow(-Mathf.Log(x), 1.0f / k);
        return a * z;
    }

    private static float Gamma(float k, float b)
    {
        if (k <= 0)
            throw new System.ArgumentException("gamma Argument Error: k<=0");
        
        uint na = (uint)Mathf.Floor(k);

        if (k == na)
        {
            return b * GammaInt(na);
        }
        else if (na == 0)
        {
            return b * GammaFrac(k);
        }
        else
        {
            return b * (GammaInt(na) + GammaFrac(k - na));
        }
    }

    private static float GammaInt(uint a)
    {
        if (a < 12)
        {
            uint i;
            float prod = 1.0f;

            for (i = 0; i < a; i++)
            {
                prod *= UniformPositive();
            }

            return -Mathf.Log(prod);
        }
        else
        {
            return GammaLarge((float)a);
        }
    }

    private static float GammaLarge(float a)
    {
        float sqa, x, y, v;
        sqa = Mathf.Sqrt(2.0f * a - 1.0f);
        
        do
        {
            do
            {
                y = Mathf.Tan(Mathf.PI * Uniform());
                x = sqa * y + a - 1.0f;
            } 
            while (x <= 0);
            
            v = Uniform();
        }
        while (v > (1.0f + y * y) * Mathf.Exp((a - 1.0f) * Mathf.Log(x / (a - 1.0f)) - sqa * y));
    
        return x;
    }

    private static float GammaFrac(float a)
    {
        float p, q, x, u, v;
        float e = Mathf.Exp(1.0f);
        p = e / (a + e);
        
        do
        {
            u = Uniform();
            v = UniformPositive();

            if (u < p)
            {
                x = Mathf.Exp((1.0f / a) * Mathf.Log(v));
                q = Mathf.Exp(-x);
            }
            else
            {
                x = 1.0f - Mathf.Log(v);
                q = Mathf.Exp((a - 1.0f) * Mathf.Log(x));
            }
        }
        while (Uniform() >= q);

        return x;
    }

    public static float GetDistribution(Distribution type, float a, float b)
    {
        if (!_isInitialized)
            throw new System.InvalidOperationException("Random seed is not set");
        
        switch(type)
        {
            case Distribution.Weibull:
                return Weibull(a, b);
            case Distribution.Gamma:
                return Gamma(a, b);
            case Distribution.Lognormal:
                return Lognormal(a, b);
            case Distribution.Normal:
                return Normal(a, b);
            case Distribution.Hyperx:
                return Hyperx(a, b);
            case Distribution.Exponential:
                return Exponential(a);
            case Distribution.One:
                return 1.0f;
            case Distribution.Zero:
                return 0.0f;
            case Distribution.Uniform:
                return UniformMinMax(a, b);
            default:
                throw new System.ArgumentException("Unknown distribution type");
        }
    }
}
