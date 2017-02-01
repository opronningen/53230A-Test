using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace PiEstimator {
    class FrqEstimator {
        static double OverlappedPiEstimator(double[] frqEstimates) {
            return (frqEstimates.Average());
        }

        static double PiEstimator(double[] phaseSamples, double f_samp, double f_ref) {
            double[] frqEstimates = new double[phaseSamples.Length - 1];

            for (int i = 1; i < phaseSamples.Length; i++)
                frqEstimates[i - 1] = (((phaseSamples[i - 1] - phaseSamples[i]) / (1/f_samp)) * f_ref) + f_ref;

            return (frqEstimates.Average());
        }

        static double LambdaEstimator(double[] frqEstimates) {
            return (frqEstimates.Average());
        }

        static double[] PhaseToFrequency(double[] newestSamples, double[] oldestSamples, double tau, double f_ref) {
            if (oldestSamples.Length != newestSamples.Length)
                return null;

            double[] res = new double[newestSamples.Length];

            for (int i = 0; i < newestSamples.Length; i++)
                res[i] = (((newestSamples[i] - oldestSamples[i]) / tau)*f_ref)+f_ref;

            return res;
        }
        /*
         * Read high resolution phase data from stdin, calculates frequency estimates
         * based on Pi-type frequency estimator
         * 
         */
        static void Main(string[] args) {

            // Number of samples per second
            int sampleRate = 5000;

            // Desired tau, in seconds
            double tau = .1;

            // Reference frequency
            double f_ref = 5e6;

            // Selected "personality"
            // 0 = Pi estimator
            // 1 = Lambda estimator
            // 2 = Omega estimator
            int estimator = 0;

            double[] oldestBuffer = null;
            double[] newestBuffer = new double[(int)(sampleRate * tau)];

            int readCounter = 0;
            int writeCounter = 0;

            int i = 0;
            string s;
            while (true) {
                s = Console.ReadLine();
                if (s == null)
                    goto end;

                readCounter++;

                if (!double.TryParse(s, NumberStyles.AllowExponent | NumberStyles.Float, CultureInfo.InvariantCulture, out newestBuffer[i++])) {
                    Console.Error.WriteLine("Could not parse '{0}'", s);
                }

                if(i == newestBuffer.Length) {
                    if(oldestBuffer != null) {
                        //double[] frqEstimates = PhaseToFrequency(newestBuffer, oldestBuffer, tau, f_ref);

                        double res = 0;
                        switch (estimator) {
                            case 0:
                                //res = PiEstimator(frqEstimates);
                                res = PiEstimator(newestBuffer, sampleRate, f_ref);
                                break;
                            case 1:
                                break;
                            case 2:
                                break;
                        }

                        Console.WriteLine(res.ToString("E15", CultureInfo.InvariantCulture));
                        writeCounter++;
                    }


                    i = 0;
                    oldestBuffer = newestBuffer;
                    newestBuffer = new double[oldestBuffer.Length];
                }

            }

        end:
            Console.Error.WriteLine("Read {0} records, wrote {1} records.", readCounter, writeCounter);
        }
    }
}
