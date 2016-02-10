using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace MA {
    class MA {
        /*
        * Read numeric values from stdin, apply n pt moving average filter, write 
        * values to stdout.
        */
        static void Main(string[] args) {
            int points = 100;       // Default to 100pt MA filter
            if(args.Length != 0) {
                if(!Int32.TryParse(args[0], out points)) {
                    Console.WriteLine("Could not parse '{0}'", args[0]);
                    return;
                }
            }

            double[] window = new double[points];

            string s;
            int i = 0;
            double val = 0;
            double sum = 0;
            bool windowFilled = false;

            while(true) {
                s = Console.ReadLine();

                if (s == null)
                    return;

                if (!double.TryParse(s, NumberStyles.AllowExponent | NumberStyles.Float, CultureInfo.InvariantCulture, out val)) {
                    Console.WriteLine("Could not parse '{0}'", s);
                }

                sum += val;          // Add current value to sum
                sum -= window[i];    // Subtract oldest value from sum
                window[i] = val;     // Insert value into window

                // Calculate where in the window the next value goes
                if (++i == points) {
                    i = 0;
                    windowFilled = true;
                }

                // Only emit a value if the window is full.
                if (windowFilled) {
                    double res = sum / points;

                    Console.WriteLine(res.ToString("E15", CultureInfo.InvariantCulture));
                }
            }
        }
    }
}
