using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Globalization;
using System.IO;
using _53230A;

namespace HRphase {
    class HRphase {
        /*
         
            HighResPhase measurements:
                1. Use ancilliary clock generator on external trigger, 100-10Khz square wave
                    Stability is not extremely important, only impacts tau
                2. Measure phase between input 1 (ref) and input 2 (dut)
                3. Do magic
                    * Linear fit (omega frequency estimator)
                    * Average (pi frequenct estimator)
                    * ...?
                4. Output phase or frequency estimate, tau = rate of trigger generator, or decimate

            To-do
                * Phase slips, f_ref != f_dut. 
                * Use ":GATE:STOP:HOLDoff:EVENts" - 20ps trigger jitter divides by 1/sqrt(n) (???)
                    TI = measured ti / holdoff-events
                * Option to measure f_ref and f_dut
                *

            Options:
                -r n        Rate of measurements = frequency of trigger generator
                -t n        Tau. The space between samples returned. Default 1
                                1 = 1 sample per second, 0.1 = 10 SPS. The proportion between tau and rate 
                                defines number of input measurements averaged to give output measurement:
                                rate = 100Hz, tau = 0.1 -> 10 input samples are used to calculate each output sample
                -d n        Decimate by n (default 1)
                -f          Output frequency estimate
                -p          Output phase (default)
                -f f1 f2    f1 = Reference frequency, f2 = DUT frequency
                -fa         Auto, measure f1 and f2
                -h n        Holdoff n edges
                -e n        Estimator
                            1 PI - Average of samples (default)
                            2 Omega - Linear fit (least squares)
        
        */
        public void usage() {
            Console.WriteLine(@"
Usage:
HRPhase -r <rate> [-t <tau>] [-f <f_ref> <f_dut> | -f auto] [-d n] [-op | -of] [-h n] [-e n]
    -r n        Rate of measurements = frequency of trigger generator (1/Hz)
    -t n        Tau. Time in seconds between output samples. Default 1 per second.
                    1 = 1 sample per second, 0.1 = 10 SPS. The proportion between tau and rate 
                    defines number of input measurements averaged to give output measurement:
                    rate = 100Hz, tau = 0.1 -> 10 input samples are used to calculate each output sample
    -d n        Decimate by n (default 1)
    -o (f|p)    Output frequency estimate or phase measurement
    -f <f>      f = Reference frequency. Default Auto.
    -f auto     Measure reference frequency
    -h n        Holdoff n edges, default 0
    -e n        Frequency estimator
                    0 Raw - Output all samples, unprocessed
                    1 PI - Average of samples (default)
                    2 Omega - Linear fit (least squares).
    -u          Unwrap phaseslips
");
        }

        public class Options {
            public double f_ref = 5e6;
            public double sampleRate = 5000;     // Frequency of the external trigger
            public int estimator = 0;           // 0 = pi (straight average), 1 = lambda (linear curve fit). Only relevant for frequency estimates
            public bool output_phase = true;   // Either phase or frequency is returned
            public double tau = 1;              // Time between output samples. tau * samplerate gives number of averaged readings
            public bool unwrap = false;
        };

        public Options opts = new Options();
        public Ag53230A instr = new Ag53230A();
        
        public bool ParseOpts(string[] args) {
            return true;

            int i = 0;

            // Parse parameters
            if (args.Length < 2) {
                usage();
                return false;
            }

            bool err = false;
            while (i < args.Length) {
                switch (args[i++]) {

                    case "-t":
                        if (!double.TryParse(args[i++], out opts.tau))
                            err = true;

                        break;

                    case "-r":
                        break;
                }

                if (err) {
                    usage();
                    return (false);
                }
            }

            return (true);
        }

        //public double unwrap(double current, double last, ref int accumulated_cycles) {
        //    double period = 1 / opts.f_ref;
        //    double phase = current + (period * accumulated_cycles);
        //    int cycles = 0;

        //    double diff = phase - last;

        //    if (diff < period / 3.0)
        //        cycles++;
        //    else if (diff > period / 3.0)
        //        cycles--;

        //    accumulated_cycles += cycles;

        //    double res = phase;
        //    res += cycles * period;

        //    return res;
        //}

        // Handle phase wraps. Optionally provide previous phase
        public double[] unwrap(double[] readings, double prev = -1) {

            if (readings == null || readings.Length == 0)
                return readings;

            double diff = 0;

            if(prev == -1)
                prev = readings[0];
                
            long cycles = 0;
            double period = 1 / opts.f_ref;

            double[] res = new double[readings.Length];
            
            for (int i = 0; i < res.Length; i++) {
                res[i] = readings[i];

                // The 53230A may return negative numbers in TI mode. Add a cycle
                if (res[i] < 0)
                    res[i] += period;

                diff = res[i] - prev;
                if (diff <= -period / 3.0) { cycles += 1; }
                if (diff >= period / 3.0) { cycles -= 1; }
                prev = res[i];
                res[i] += cycles * period;
            }

            return res;
        }

        public void Run() {
            StreamWriter Err = new StreamWriter(Console.OpenStandardError());
            Err.AutoFlush = true;

            // Place instument in "waiting-for-trigger" mode
            instr.WriteString("ABORT;*WAI;INIT;*TRG");

            // Query to used to retrieve readings. Fetch 1 second worth of readings
            string query = String.Format("*TRG;:DATA:REMOVE? {0},WAIT", opts.tau * opts.sampleRate);

            double last_phase = -1;

            while (true) {
            
                instr.WriteString(query);

                double[] values = instr.GetReadings();

                // Add/remove cycles to account for frequency offset/phase slips
                if (opts.unwrap)
                    values = unwrap(values);

                double phase;

                // If phase is requested, simply average each batch of readings (phase unwrapped)
                if (opts.output_phase) {
                    if(opts.estimator == 0)
                        foreach(double sample in values)
                            Console.WriteLine(sample.ToString("E15", CultureInfo.InvariantCulture));
                    else
                        Console.WriteLine(values.Average().ToString("E15", CultureInfo.InvariantCulture));
                } else {
                    // Output frequency estimate

                    // PI estimator - average 100pt phase (or whatever tau * samplerate works out to) 
                    // with average of next 100pt -> calc f
                    if (opts.estimator == 0) {
                        phase = values.Average();

                        if (last_phase == -1) {
                            last_phase = phase;
                        } else {
                            // Phase to frequency based on current and last phase.
                            phase = unwrap(new double[] { phase }, last_phase)[0];                                  // Unwrap wrt last phase
                            double f = (phase - last_phase) * (opts.f_ref / (1 / opts.sampleRate)) + opts.f_ref;    // Error, must take into account averaging
                            Console.WriteLine(f.ToString("E15", CultureInfo.InvariantCulture));
                        }

                    } else if (opts.estimator == 1) {
                        // Linear fit of data in this "batch", output frequency estimate based on slope


                    } else if (opts.estimator == 2) {
                        // Unknown estimator - estimate frequency from each phase-measurement in this batch and average the frequency-etimates

                    }
                }
            }
        }

        static void Main(string[] args) {
            HRphase h = new HRphase();

            h.instr.LearnConfig();

            if(h.ParseOpts(args))
                h.Run();
        }
    }
}
