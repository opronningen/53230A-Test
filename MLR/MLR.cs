using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace MLR {
    class MLR {
        static void Main(string[] args) {
            int n = 100;       // Default to 100pt filter
            if (args.Length != 0) {
                if (!Int32.TryParse(args[0], out n)) {
                    Console.WriteLine("Could not parse '{0}'", args[0]);
                    return;
                }
            }

            double[] window = new double[n];

            string s;
            int windowIndex = 0;
            double val = 0;
            bool windowFilled = false;

            while (true) {
                s = Console.ReadLine();

                if (s == null)
                    return;

                if (!double.TryParse(s, NumberStyles.AllowExponent | NumberStyles.Float, CultureInfo.InvariantCulture, out val)) {
                    Console.WriteLine("Could not parse '{0}'", s);
                }

                window[windowIndex] = val;     // Insert value into window
                
                // Calculate where in the window the next value goes
                if (++windowIndex == n) {
                    windowIndex = 0;
                    windowFilled = true;
                }

                // Only emit a value if the window is full.
                if (windowFilled) {

                    // Fit line through data, least squares
                    int i;
                    double dx, dy, Sx, Sy, Sdx2, Sdxdy, xmean, ymean;

                    Sx = Sy = 0.0;
                    for (i = 0; i < n; i += 1) {
                        Sx += i;
                        Sy += window[(i + windowIndex) % n];
                    }

                    xmean = Sx / n;
                    ymean = Sy / n;

                    Sdx2 = Sdxdy = 0.0;
                    for (i = 0; i < n; i += 1) {
                        dx = i - xmean;
                        dy = window[(i + windowIndex) % n] - ymean;
                        Sdx2 += dx * dx;
                        Sdxdy += dx * dy;
                    }

                    double slope = Sdxdy / Sdx2;         // slope
                    double offset = ymean - slope * xmean; // offset

                    double res = offset + (slope);
                    Console.WriteLine(res.ToString("E15", CultureInfo.InvariantCulture));
                }
            }
        }
    }
}
