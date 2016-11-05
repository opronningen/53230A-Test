using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using _53230A;

namespace BinRead {
    class BinRead {
        /*
        * Send a "Read?" command to counter, which triggers a new measurement. The counter will take the number of samples 
        * specified with ":Sample:count", and writes the returned values to Console, separated by ";", one line per "Read?"
        *
        * If auxQuery != null, runs this query once per trigger, and logs the result to q.txt
        *
        * If an integer argument is given, repeats the measurement this number of times. Else runs untill terminated with ctrl-c, or timeout
        */

        static void Main(string[] args) {

            Ag53230A instr = new Ag53230A();
            instr.debug = false;

            instr.LearnConfig();

            string fname = "res.bin";
            int repeats = -1;

            if (args.Length > 0)
                if (!Int32.TryParse(args[0], out repeats))
                    Console.Error.WriteLine("Error! Argument {0} not a parseable integer.", args[0]);

            if (args.Length > 1)
                fname = args[1];

            BinaryWriter bw = new BinaryWriter(File.Open(fname, FileMode.Create));

            // repeats-- will never be evaluated if repeats == -1. 
            while (repeats == -1 || repeats-- > 0) {
                instr.WriteString("READ?");

                foreach (double d in instr.GetReadings())
                    bw.Write(d);

                Console.Error.WriteLine(repeats);
            }

            bw.Dispose();
        }
    }
}
