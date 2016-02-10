using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.IO;

namespace Phase2Freq {
    class Phase2Freq {
        /* Reads raw TI measurements from stdin, return frequency-estimate based on
        * several estimators. Arguments:
        *
        *   Phase2Freq <windowsize> <decimation> <estimator>
        *       <windowsize>    Integer. How many samples is included in the frequency estimate
        *       <decimation>    Integer. How "often" to output a sample. If decimation = windowsize
        *                       there is no overlap. If decimation < windowsize, some datapoints is
        *                       used in two consecutive estimates.
        *       <estimator>     Integer. 0 = Pi, 1 = Lambda, 2 = Omega
        *
        */

        static void Usage() {
            Console.WriteLine(
@"Phase2Freq <f_ref> <windowsize> <decimation> <estimator>
<f_ref>         Integer. Reference frequency.
<tau>           Float. Samples per second of input data
<windowsize>    Integer. How many samples is included in the frequency estimate
<decimation>    Integer. How often to output a sample. If decimation = windowsize
                there is no overlap. If decimation < windowsize, some datapoints are
                used in two consecutive estimates.
<estimator>     Integer. 0 = Pi, 1 = Lambda, 2 = Omega


Takes samples from stdin, writes frequency-estimates to stdout.");

        }

        static double[] unwrap(double[] rawdata, double f_ref) {
            if (rawdata == null || rawdata.Length == 0)
                return rawdata;

            double diff = 0;

            int cycles = 0;
            double period = 1 / f_ref;

            double[] res = new double[rawdata.Length];

            res[0] = rawdata[0];

            for (int i = 1; i < res.Length; i++) {

                diff = rawdata[i] - rawdata[i - 1];
                if (diff <= -period / 3.0) { cycles += 1; }
                if (diff >= period / 3.0) { cycles -= 1; }
                res[i] = rawdata[i] + (cycles * period);
            }

            return res;
        }

        static double applyWindow(double[] data, double[] window, int offset) {
            double res = 0;
            for (int i = 0; i < window.Length; i++) {
                res += data[offset + i] * window[i];
            }

            return res;
        }

        static double[] piWindow(int points) {
            double[] window = new double[points];

            for (int i = 0; i < window.Length; i++) {
                window[i] = 1.0 / (double)points;
            }

            return window;
        }

        static double[] lambdaWindow(int points) {
            double[] window = new double[points];

            for (int i = 0; i < window.Length; i++)
                window[i] = -((i - ((points) / 2)) / ((points - 1) / 2));

            return window;
        }

        static void Main(string[] args) {
            List<double> data = new List<double>();
            double val;

            //StreamWriter Err = new StreamWriter(Console.OpenStandardError());
            //Err.AutoFlush = true;

            int f_ref = 0;
            double tau = 0;
            int windowSize = 0;
            int decimation = 0;
            int estimator = 0;

            try {
                f_ref = Int32.Parse(args[0], NumberStyles.AllowExponent);
                tau = Double.Parse(args[1], NumberStyles.AllowExponent);
                windowSize = Int32.Parse(args[2]);
                decimation = Int32.Parse(args[3]);
                estimator = Int32.Parse(args[4]);
            } catch (Exception) {
                Usage();
                return;
            }

            string s;
            while ((s = Console.ReadLine()) != null && s != "") {

                if (!double.TryParse(s, NumberStyles.AllowExponent | NumberStyles.Float, CultureInfo.InvariantCulture, out val)) {
                    Console.WriteLine("Could not parse '{0}'", s);
                }

                data.Add(val);
                val = new double();
            }

            double[] unwrapped_data = unwrap(data.ToArray(), f_ref);

            double[] pi_window = piWindow(windowSize);
            double[] lambda_window = lambdaWindow(windowSize);

            double phase;
            double last_phase = -1;
            double diff;
            double f = 0;
            for (int i = 0; i + (windowSize - decimation) < unwrapped_data.Length; i += decimation) {

                switch (estimator) {
                    case 0:
                        phase = applyWindow(unwrapped_data, pi_window, i);
                        if (last_phase == -1) {
                            last_phase = phase;
                            continue;
                        }

                        //Console.WriteLine("Phase; {0}", phase);

                        diff = phase - last_phase;
                        f = ((f_ref * diff) * (tau / decimation)) + f_ref;
                        last_phase = phase;

                        break;
                    case 1:
                        phase = applyWindow(unwrapped_data, lambda_window, i);
                        if (last_phase == -1) {
                            last_phase = phase;
                            continue;
                        }

                        diff = phase - last_phase;
                        f = ((f_ref * diff) * (tau / decimation)) + f_ref;
                        last_phase = phase;
                        break;
                    case 2:
                        int ix;
                        double dx, dy, Sx, Sy, Sdx2, Sdxdy, xmean, ymean;

                        Sx = Sy = 0.0;
                        for (ix = 0; ix < windowSize; ix += 1) {
                            Sy += unwrapped_data[i + ix];
                            Sx += ix;
                        }

                        xmean = Sx / windowSize;
                        ymean = Sy / windowSize;

                        Sdx2 = Sdxdy = 0.0;
                        for (ix = 0; ix < windowSize; ix += 1) {
                            dy = unwrapped_data[i + ix] - xmean;
                            dx = ix - ymean;
                            Sdx2 += dx * dx;
                            Sdxdy += dx * dy;
                        }

                        double slope = Sdxdy / Sdx2;         // slope
                                                             // c[0] = ymean - c[1] * xmean; // offset
                        f = ((f_ref * slope) * (tau / decimation)) + f_ref;
                        break;

                }

                Console.WriteLine(f.ToString("E15", CultureInfo.InvariantCulture));
            }
        }
    }
}
