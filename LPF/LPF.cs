using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace LPF {
    /*
     * Low Pass Filter. Reads samples from stdin, as doubles.
     *  -d  <n>     Decimate. Take 1 sample, skip n. Applied after filtering. Optional. Default 1 sample per filterkernel.
     *  -t <n>      Taps. Length of filter-kernel. Optional, default = 100
     *  -c <float>  Cutoff-frequency; 0 to 0.5. (cutoff in hertz)/f_sample. Optional, default 0.5.
     */

    class Opts {
        public int taps = 100;
        public int d = 0;
        public double fc = 0.01;
    }

    class LPF {
        static void Usage() {

        }

        static Opts ParseOptions(string[] args) {
            Opts opts = new Opts();

            //if (args.Length < 2) 
            //    return null;


            return opts;

        }

        static double[] getKernel(int length, double fc) {
            var kernel = new double[length];

            for (int i = 0; i < length; i++) {
                if (i - length / 2 == 0)
                    kernel[i] = 2 * Math.PI * fc;
                else
                    kernel[i] = Math.Sin(2 * Math.PI * fc * (i - length / 2)) / (i - length / 2);
            }

            // Normalize filter
            double kernelSum = kernel.Sum();
            for (int i = 0; i < length; i++)
                kernel[i] /= kernelSum;

            return kernel;
        }

        static void Main(string[] args) {
            Opts opts = ParseOptions(args);
            if (opts == null) {
                Usage();
                return;
            }

            double[] filter = getKernel(opts.taps, opts.fc);
            double[] sampleBuffer = new double[opts.taps];

            string s;
            int count = 0;

            while (true) {
                s = Console.ReadLine();
                if (s == null)
                    return;

                if (!double.TryParse(s, NumberStyles.AllowExponent | NumberStyles.Float, CultureInfo.InvariantCulture, out sampleBuffer[0]))
                    Console.Error.WriteLine("Could not parse '{0}'", s);

                count++;

                if (count >= opts.taps) {
                    double sum = 0;

                    for (int i = 0; i < sampleBuffer.Length; i++) 
                        sum += sampleBuffer[i] * filter[i];

                    Console.WriteLine(sum.ToString("E15", CultureInfo.InvariantCulture));
                }

                Array.Copy(sampleBuffer, 0, sampleBuffer, 1, sampleBuffer.Length - 1);
            }
        }
    }
}
