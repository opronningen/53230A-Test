﻿using System;
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
                1. Use ancilliary clock generator on external trigger, 100-50Khz square wave
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
    -a n        Average n points (default 1)
    -f <f>      f = Reference frequency. Default Auto.
    -f auto     Measure reference frequency
    -u          Unwrap phaseslips (implicit if averaging)
");
        }

        public class Options {
            public double f_ref = 5e6;          // Frequency of reference signal
            public double sampleRate = 5e4;     // Frequency of the external trigger
            public int average = 0;             // How many samples to average
            public bool unwrap = true;
            public int tau = 1;                // tau in seconds (only relevant for frequency estimates
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

        // Handle phase wraps. 
        static double prev = -1;
        static long cycles = 0;
        public double[] unwrap(double[] readings) {

            if (readings == null || readings.Length == 0)
                return readings;

            double diff = 0;
                
            double period = 1 / opts.f_ref;

            double[] res = new double[readings.Length];

            if (prev == -1)
                prev = readings[0];

            for (int i = 0; i < res.Length; i++) {
                res[i] = readings[i];

                // The 53230A may return negative numbers in TI mode. Add a cycle
                //if (res[i] < 0)
                //    res[i] += period;

                diff = res[i] - prev;
                if (diff <= -period / 3.0) {
                    cycles += 1;
                }else if (diff >= period / 3.0) {
                    cycles -= 1;
                }

                prev = res[i];
                res[i] += cycles * period;
            }

            return res;
        }

        public void Run() {

            double[] values = null;

            // Place instument in "waiting-for-trigger" mode
            instr.WriteString("ABORT;*WAI;INIT;*TRG");

            // Query to used to retrieve readings. Fetch 1 trigger worth of readings
            string query = String.Format("*TRG;:DATA:REMOVE? {0},WAIT", opts.sampleRate);

            int triggersToFetch = (int)instr.Conf.GetNumericByID(SettingID.trig_coun).value;
            Console.Error.Write("\nFetching {0} triggers", triggersToFetch);

            int triggerCount = 0;
            while (triggerCount < triggersToFetch) {

                // There is already one buffered trigger, to not issue the last trigger (or the instrument will balk)
                if(triggerCount < triggersToFetch -1)
                    instr.WriteString(query);
                else
                    instr.WriteString(String.Format(":DATA:REMOVE? {0},WAIT", opts.sampleRate));

                values = instr.GetReadings();

                // Add/remove cycles to account for frequency offset/phase slips - implicit if averaging
                if (opts.unwrap || opts.average > 2)
                    values = unwrap(values);

                // Return phase samples, optionally averaged

                if (opts.average < 2) {
                    foreach (double sample in values)
                        Console.WriteLine(sample.ToString("E15", CultureInfo.InvariantCulture));
                } else {
                    Console.WriteLine(values.Average().ToString("E15", CultureInfo.InvariantCulture));

                    // To-do: average may not match readings per trigger. Also, may not divide readings per
                    // trigger exactly, so need to save "leftover" readings.
                }
                
                // output status message every 100 triggers
                if (++triggerCount % 100 == 0)
                    Console.Error.Write("\nFetched {0} triggers", triggerCount);
                else
                    Console.Error.Write(".");
            }
            Console.Error.Write("\nFetched {0} triggers", triggerCount);
        }

        static void Main(string[] args) {
            HRphase h = new HRphase();

            h.instr.LearnConfig();
            h.opts.sampleRate = (h.instr.Conf.GetByID(SettingID.samp_coun) as NumericSetting).value;
            if(h.ParseOpts(args))
                h.Run();
        }
    }
}
