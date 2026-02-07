using System;

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

// должен задаваться сид обез
// в паблик методе проверку на нулл
// не потокобезопасно
public static class RandomUtils
{
    private static System.Random _random;

    public static void SetSeed(int seed)
    {
        if (_random != null)
            throw new InvalidOperationException("Trying to reset current seed");

        _random = new Random(seed);
    }

    private static double Uniform()
    {
        return _random.NextDouble();
    }

    private static double UniformPositive()
    {
        double g;
        g=Uniform();
        while (g==0.0)
            g=Uniform();

        return g;
    }

    // границы [l;r)
    private static double UniformMinMax(double min, double max)
    {
        if (min > max)
            throw new ArgumentException("Min is greater than max");

        return (min + (max-min)*Uniform());
    }

    // границы включаются обе
    private static int UniformInt(int min, int max)
    {
        if (min > max)
            throw new ArgumentException("Min is greater than max");
        
        return _random.Next(min, max+1);
    }

    /*--------------  EXPONENTIAL RANDOM VARIATE GENERATOR  --------------*/
    /* The exponential distribution has the form

    p(x) dx = exp(-x/landa) dx/landa

    for x = 0 ... +infty 
    */

    private static double Exponential(double landa)
    {
        double u = UniformPositive();
        double mean = 1.0 / landa;

        return -mean * Math.Log(u);
    } 

    private static double Erlang(double x, double s)
    {
        int i, k; double z;
        if (s > x)
            throw new ArgumentException("erlang Argument Error: s > x");
        z=x/s; k=(int)(z*z);
        z=1.0; for (i=0; i<k; i++) z*=UniformPositive();
        return(-(x/k)*Math.Log(z));
    }

    private static double Hyperx(double x, double s)
    {
        double cv,z,p;
        if (s<=x)
            throw new ArgumentException("hyperx Argument Error: s not > x");
        cv=s/x; z=cv*cv; p=0.5*(1.0-Math.Sqrt((z-1.0)/(z+1.0)));
        z=(UniformPositive()>p)? (x/(1.0-p)):(x/p);
        return(-0.5*z*Math.Log(UniformPositive()));
    }

    private static double z2 = 0.0;
    private static double Normal(double x, double s)
    {
        double v1,v2,w,z1; 
        if (z2!=0.0)
            {z1=z2; z2=0.0;}  /* use value from previous call */
            else
            {
                do
                {v1=2.0*UniformPositive()-1.0; v2=2.0*UniformPositive()-1.0; w=v1*v1+v2*v2;}
                while (w>=1.0);
                w=Math.Sqrt((-2.0*Math.Log(w))/w); z1=v1*w; z2=v2*w;
            }
        return(x+z1*s);
    }

    private static double Lognormal(double p, double u)
    {
        return Math.Exp(Normal(p,u));
    }

    /* The Weibull distribution has the form,

    p(x) dx = (k/a) (x/a)^(k-1) exp(-(x/a)^k) dx

    k = shape
    a = landa = scale
    */

    private static double Weibull(double k, double a)
    {
        double x = UniformPositive();
        double z = Math.Pow(-Math.Log(x), 1.0/k);
        return(a * z);
    }

    /* The Gamma distribution 

    k = shape
    b = teta = scale

    p(x) dx = {1 / \Gamma(k) b^a } x^{k-1} e^{-x/b} dx

    for x>0.  If X and Y are independent gamma-distributed random
    variables of order a1 and a2 with the same scale parameter b, then
    X+Y has gamma distribution of order a1+a2.

    The algorithms below are from Knuth, vol 2, 2nd ed, p. 129. */

    private static double Gamma(double k, double b)
    {
        if (k<=0)
            throw new ArgumentException("gamma Argument Error: k<=0");
        
        uint na = (uint)Math.Floor(k);

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
            return b * (GammaInt(na) + GammaFrac(k-na));
        }

    }

    static double GammaInt(uint a)
    {
        if (a < 12)
        {
            uint i;
            double prod = 1;

            for (i = 0;i<a;i++)
            {
                prod *= UniformPositive();
            }

            return -Math.Log(prod);
        }
        else
        {
            return GammaLarge((double)a);
        }
    }

    static double GammaLarge(double a)
    {
        double sqa,x,y,v;
        sqa = Math.Sqrt(2*a-1);
        do
        {
            do
            {
                y = Math.Tan(Math.PI*Uniform());
                x = sqa*y+a-1;
            } 
            while (x<=0);
            v=Uniform();
        }
        while (v > (1 + y * y) * Math.Exp ((a - 1) * Math.Log (x / (a - 1)) - sqa * y));
    
        return x;
    }

    static double GammaFrac(double a)
    {
        double p, q, x, u, v;
        p = Math.E / (a + Math.E);
        do
            {
            u = Uniform();
            v = UniformPositive();

            if (u < p)
                {
                x = Math.Exp ((1 / a) * Math.Log (v));
                q = Math.Exp (-x);
                }
            else
                {
                x = 1 - Math.Log (v);
                q = Math.Exp ((a - 1) * Math.Log (x));
                }
            }
        while (Uniform() >= q);

        return x;
    }

    public static double GetDistribution(Distribution type, double a, double b)
    {
        if (_random == null)
            throw new InvalidOperationException("Random seed is not set");
        
        switch(type)
        {
            case Distribution.Weibull:
                return Weibull(a,b);
            case Distribution.Gamma:
                return Gamma(a,b);
            case Distribution.Lognormal:
                return Lognormal(a,b);
            case Distribution.Normal:
                return Normal(a,b);
            case Distribution.Hyperx:
                return Hyperx(a,b);
            case Distribution.Exponential:
                return Exponential(a);
            case Distribution.One:
                return 1;
            case Distribution.Zero:
                return 0;
            case Distribution.Uniform:
                return UniformMinMax(a,b);
            default:
                throw new ArgumentException("Unknown distribution type");
        }
    }

}
